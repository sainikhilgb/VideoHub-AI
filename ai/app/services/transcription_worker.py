import asyncio
import logging
import tempfile
import os

import httpx

from app.core.config import settings
from app.models.schemas import (
    CaptionStatusCallback,
    LanguageResult,
    LanguageTarget,
    ProcessCallbackPayload,
    ProcessRequest,
    SegmentResult,
    CombineRequest,
)
from app.services import caption_service, storage_service, transcription_service
from app.core.logging import log_operation, log_context

logger = logging.getLogger(__name__)


async def _post_status(client: httpx.AsyncClient, url: str, payload: dict, headers: Optional[dict] = None) -> None:
    """Posts a status update to the .NET API, silently logging failures."""
    try:
        response = await client.post(url, json=payload, headers=headers, timeout=15)
        response.raise_for_status()
    except Exception as exc:
        logger.warning("Status callback failed: %s — %s", url, exc)


async def _post_status_with_backoff(
    client: httpx.AsyncClient,
    url: str,
    payload: dict,
    headers: Optional[dict] = None,
    max_retries: int = 4
) -> bool:
    """Posts a status update with bounded exponential backoff. Returns True if successful."""
    delay = 2.0
    for attempt in range(max_retries):
        try:
            response = await client.post(url, json=payload, headers=headers, timeout=15)
            response.raise_for_status()
            logger.info("Status callback succeeded on attempt %s.", attempt + 1)
            return True
        except Exception as exc:
            logger.warning(
                "Status callback attempt %s failed: %s. Retrying in %s seconds...",
                attempt + 1, exc, delay
            )
            if attempt == max_retries - 1:
                break
            await asyncio.sleep(delay)
            delay = min(delay * 2.0, 30.0)
    return False


async def _persist_recovery_state(request: CombineRequest, payload: dict) -> None:
    """Saves the callback payload locally and uploads to Supabase storage to prevent orphaned jobs."""
    try:
        import json
        # 1. Local persistence
        recovery_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "recovery"))
        os.makedirs(recovery_dir, exist_ok=True)
        recovery_path = os.path.join(recovery_dir, f"{request.combined_media_id}.json")
        with open(recovery_path, "w") as f:
            json.dump(payload, f, indent=2)
        logger.info("Local recovery payload written to: %s", recovery_path)

        # 2. Supabase Cloud persistence
        supabase_path = f"{request.output_folder.rstrip('/')}/recovery/{request.combined_media_id}.json"
        logger.info("Uploading recovery payload to storage bucket: %s", supabase_path)
        await storage_service.upload_to_supabase(
            request.bucket,
            recovery_path,
            supabase_path,
            "application/json"
        )
    except Exception as recovery_exc:
        logger.critical("Failed to persist callback recovery state: %s", recovery_exc, exc_info=True)


async def _process_language(
    client: httpx.AsyncClient,
    bucket: str,
    lang: LanguageTarget,
    segments: list[SegmentResult],
    dotnet_base: str,
) -> LanguageResult:
    """
    Generates SRT and VTT captions for a single language,
    uploads them to Supabase, and notifies .NET per-file.
    """
    try:
        formats = {
            "srt": caption_service.generate_srt(segments),
            "vtt": caption_service.generate_vtt(segments),
        }
        content_types = {"srt": "text/plain", "vtt": "text/vtt"}

        for fmt, content in formats.items():
            caption_file_id = lang.caption_file_ids.get(fmt)
            if not caption_file_id:
                continue

            storage_path = f"{lang.folder_path.rstrip('/')}/transcript.{fmt}"

            with tempfile.NamedTemporaryFile(mode="w", suffix=f".{fmt}", delete=False, encoding="utf-8") as tmp:
                tmp.write(content)
                tmp_path = tmp.name

            try:
                blob_url = await storage_service.upload_to_supabase(
                    bucket, tmp_path, storage_path, content_types[fmt]
                )
            finally:
                os.unlink(tmp_path)

            # Notify .NET that this specific caption file is done
            status_url = f"{dotnet_base.rstrip('/')}/api/v1/caption-files/{caption_file_id}/status"
            await _post_status(client, status_url, CaptionStatusCallback(
                status="Completed",
                blob_url=blob_url,
            ).model_dump(by_alias=True))

        return LanguageResult(language_code=lang.language_code, status="Completed")

    except Exception as exc:
        logger.error("Language processing failed: lang=%s error=%s", lang.language_code, exc, exc_info=True)

        # Notify .NET of the failure for each caption file in this language
        for fmt, caption_file_id in lang.caption_file_ids.items():
            status_url = f"{dotnet_base.rstrip('/')}/api/v1/caption-files/{caption_file_id}/status"
            await _post_status(client, status_url, CaptionStatusCallback(
                status="Failed",
                error=str(exc),
            ).model_dump(by_alias=True))

        return LanguageResult(language_code=lang.language_code, status="Failed", error=str(exc))


async def run(request: ProcessRequest) -> None:
    """
    Main background worker:
    1. Downloads media from Supabase.
    2. Extracts audio if video.
    3. Transcribes with Whisper (single inference).
    4. Processes all languages in parallel.
    5. Sends the final callback to .NET.
    """
    dotnet_base = settings.dotnet_api_base_url
    completed_languages = set()
    language_results = []
    transcript_blob_url = ""
    
    # Initialize background task log context using incoming request identifiers
    token = log_context.set({
        "CorrelationId": request.correlation_id,
        "RequestId": request.request_id,
        "JobId": request.job_id,
        "ProjectId": request.project_id,
        "MediaId": request.media_id,
        "ServiceName": "VideoHub.Ai"
    })

    try:
        with log_operation("transcription_worker_job"):
            with tempfile.TemporaryDirectory() as tmpdir:
                source_ext = os.path.splitext(request.storage_path)[-1] or ".mp4"
                source_path = os.path.join(tmpdir, f"source{source_ext}")
                audio_path = os.path.join(tmpdir, "audio.wav")

                # Step 1: Download
                with log_operation("download_media", {"StoragePath": request.storage_path}):
                    await storage_service.download_from_supabase(request.bucket, request.storage_path, source_path)

                # Step 2: Extract audio if video
                if request.media_type.lower() == "video":
                    with log_operation("extract_audio"):
                        await asyncio.to_thread(transcription_service.extract_audio, source_path, audio_path)
                else:
                    audio_path = source_path

                # Step 3: Transcribe (single Whisper inference)
                with log_operation("whisper_transcription"):
                    lang_hint = request.original_language if request.original_language else None
                    detected_language, segments = await asyncio.to_thread(
                        transcription_service.transcribe, audio_path, lang_hint
                    )

                async with httpx.AsyncClient() as client:
                    # Step 4: Process all languages in parallel
                    with log_operation("process_languages", {"TargetLanguages": [language.language_code for language in request.languages]}):
                        tasks = [
                            _process_language(client, request.bucket, lang, segments, dotnet_base)
                            for lang in request.languages
                        ]
                        language_results = await asyncio.gather(*tasks)
                        for res in language_results:
                            if res.status == "Completed":
                                completed_languages.add(res.language_code)

                    # Step 5: Compile segments to transcript.json and upload
                    import json
                    transcript_data = {
                        "detectedLanguage": detected_language,
                        "segments": [seg.model_dump(by_alias=True) for seg in segments]
                    }

                    tmp_path = None
                    try:
                        with tempfile.NamedTemporaryFile(mode="w", suffix=".json", delete=False, encoding="utf-8") as tmp:
                            tmp_path = tmp.name
                            json.dump(transcript_data, tmp, ensure_ascii=False, indent=2)

                        path_parts = request.storage_path.strip("/").split("/")
                        if len(path_parts) < 2:
                            raise ValueError(f"Invalid media storage path format: {request.storage_path}")

                        project_prefix = f"{path_parts[0]}/{path_parts[1]}"
                        transcript_storage_path = f"{project_prefix}/transcripts/transcript.json"

                        transcript_blob_url = await storage_service.upload_to_supabase(
                            request.bucket, tmp_path, transcript_storage_path, "application/json"
                        )
                    finally:
                        if tmp_path and os.path.exists(tmp_path):
                            os.unlink(tmp_path)

                    # Step 6: Final callback to .NET
                    with log_operation("final_callback_dotnet"):
                        callback_url = f"{dotnet_base.rstrip('/')}{request.callback_url}"
                        payload = ProcessCallbackPayload(
                            detected_language=detected_language,
                            transcript_blob_url=transcript_blob_url,
                            language_results=language_results,
                        )

                        response = await client.post(
                            callback_url,
                            json=payload.model_dump(by_alias=True),
                            timeout=30,
                        )
                        response.raise_for_status()

    except Exception as exc:
        logger.error("Job execution failed globally: job_id=%s error=%s", request.job_id, exc, exc_info=True)
        
        # Notify .NET of the failures only for caption files that were not completed successfully
        async with httpx.AsyncClient() as client:
            final_language_results = []
            
            # Map existing results for easy lookup
            existing_results = {r.language_code: r for r in language_results}
            
            for lang in request.languages:
                if lang.language_code in completed_languages:
                    if lang.language_code in existing_results:
                        final_language_results.append(existing_results[lang.language_code])
                    continue
                
                for fmt, caption_file_id in lang.caption_file_ids.items():
                    status_url = f"{dotnet_base.rstrip('/')}/api/v1/caption-files/{caption_file_id}/status"
                    await _post_status(client, status_url, CaptionStatusCallback(
                        status="Failed",
                        error=f"Job failed globally: {str(exc)}",
                    ).model_dump(by_alias=True))
                
                final_language_results.append(LanguageResult(
                    language_code=lang.language_code,
                    status="Failed",
                    error=str(exc)
                ))
            
            # Send the final callback with the successfully generated transcript URL (if any)
            callback_url = f"{dotnet_base.rstrip('/')}{request.callback_url}"
            payload = ProcessCallbackPayload(
                detected_language=request.original_language or "unknown",
                transcript_blob_url=transcript_blob_url,
                language_results=final_language_results,
            )
            try:
                await client.post(callback_url, json=payload.model_dump(by_alias=True), timeout=30)
            except Exception as cb_exc:
                logger.error("Failed to send fallback final callback: %s", cb_exc)
    finally:
        log_context.reset(token)


async def run_combine(request: CombineRequest) -> None:
    """
    Background worker for combining subtitle tracks with video using ffmpeg:
    1. Downloads both video and subtitle files via HTTP GET.
    2. Runs FFmpeg:
       - Soft-Mux: embeds the subtitle file as a soft track stream.
    3. Uploads the processed video back to the Supabase storage bucket.
    4. Posts a status callback to the .NET backend.
    """
    import subprocess
    import uuid
    dotnet_base = settings.dotnet_api_base_url
    callback_url = f"{dotnet_base.rstrip('/')}{request.callback_url}"
    headers = {"Authorization": f"Bearer {request.callback_secret}"}

    # Initialize log context
    token = log_context.set({
        "CorrelationId": str(uuid.uuid4()),
        "CombinedMediaId": request.combined_media_id,
        "ServiceName": "VideoHub.Ai"
    })

    try:
        with log_operation("combine_worker_job"):
            with tempfile.TemporaryDirectory() as tmpdir:
                video_ext = os.path.splitext(request.output_name)[-1] or ".mp4"
                video_input_path = os.path.join(tmpdir, f"video_input{video_ext}")
                subtitle_input_path = os.path.join(tmpdir, "subtitle_input.srt")
                video_output_path = os.path.join(tmpdir, f"video_output{video_ext}")

                # Download video file
                async with httpx.AsyncClient(timeout=300) as client:
                    async with client.stream("GET", request.video_url) as r:
                        r.raise_for_status()
                        with open(video_input_path, "wb") as f:
                            async for chunk in r.aiter_bytes(chunk_size=8192):
                                f.write(chunk)

                # Download subtitle file (SRT)
                async with httpx.AsyncClient(timeout=60) as client:
                    async with client.stream("GET", request.subtitle_url) as r:
                        r.raise_for_status()
                        with open(subtitle_input_path, "wb") as f:
                            async for chunk in r.aiter_bytes(chunk_size=8192):
                                f.write(chunk)

                # Run FFmpeg soft-muxing (combines video stream with subtitle text stream)
                logger.info("Executing soft-mux subtitles using FFmpeg")
                lang_code = request.language.lower()
                lang_map = {"en": "eng", "es": "spa", "fr": "fre", "de": "ger", "it": "ita"}
                ffmpeg_lang = lang_map.get(lang_code, lang_code)

                cmd = [
                    "ffmpeg", "-y", "-i", video_input_path,
                    "-i", subtitle_input_path,
                    "-map", "0:v",
                    "-map", "0:a?",
                    "-map", "1:s",
                    "-c:v", "libx264",
                    "-pix_fmt", "yuv420p",
                    "-c:a", "aac",
                    "-c:s", "mov_text",
                    "-metadata:s:s:0", f"language={ffmpeg_lang}",
                    "-metadata:s:s:0", f"title={request.language.upper()} Subtitles",
                    video_output_path
                ]

                # Execute FFmpeg synchronously inside a thread pool to avoid blocking the asyncio loop
                def run_ffmpeg():
                    try:
                        res = subprocess.run(cmd, capture_output=True, text=True, check=True, timeout=settings.ffmpeg_combine_timeout)
                        logger.info("FFmpeg combine completed successfully: stdout=%s", res.stdout)
                    except subprocess.TimeoutExpired as err:
                        logger.error("FFmpeg combine timed out after %s seconds: stdout=%s stderr=%s", err.timeout, err.stdout, err.stderr)
                        raise Exception(f"FFmpeg combine timed out after {err.timeout} seconds.") from err
                    except subprocess.CalledProcessError as err:
                        logger.error("FFmpeg combine failed with exit code %s: stdout=%s stderr=%s", err.returncode, err.stdout, err.stderr)
                        err_msg = err.stderr or ""
                        raise Exception(f"FFmpeg failed with exit code {err.returncode}: {err_msg}") from err

                await asyncio.to_thread(run_ffmpeg)

                # Upload to Supabase
                storage_path = f"{request.output_folder.rstrip('/')}/{request.output_name}"
                logger.info("Uploading combined video output to storage: %s", storage_path)
                blob_url = await storage_service.upload_to_supabase(
                    request.bucket, video_output_path, storage_path, "video/mp4"
                )

                # Send success status callback to .NET with backoff retry and fallback cloud persistence
                success_payload = {
                    "status": "Completed",
                    "blobUrl": blob_url
                }
                async with httpx.AsyncClient() as client:
                    success_ok = await _post_status_with_backoff(client, callback_url, success_payload, headers=headers)
                    if not success_ok:
                        logger.error("Failed to deliver success callback to .NET API after retries. Saving recovery file.")
                        await _persist_recovery_state(request, success_payload)

    except Exception as exc:
        logger.error("Combined media processing failed: CombinedMediaId=%s error=%s", request.combined_media_id, exc, exc_info=True)
        failure_payload = {
            "status": "Failed",
            "error": str(exc)
        }
        async with httpx.AsyncClient() as client:
            failure_ok = await _post_status_with_backoff(client, callback_url, failure_payload, headers=headers)
            if not failure_ok:
                logger.error("Failed to deliver failure callback to .NET API after retries. Saving recovery file.")
                await _persist_recovery_state(request, failure_payload)
    finally:
        log_context.reset(token)
