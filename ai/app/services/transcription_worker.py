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
)
from app.services import caption_service, storage_service, transcription_service
from app.core.logging import log_operation, log_context

logger = logging.getLogger(__name__)


async def _post_status(client: httpx.AsyncClient, url: str, payload: dict) -> None:
    """Posts a status update to the .NET API, silently logging failures."""
    try:
        response = await client.post(url, json=payload, timeout=15)
        response.raise_for_status()
    except Exception as exc:
        logger.warning("Status callback failed: %s — %s", url, exc)


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
