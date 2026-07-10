import asyncio
import logging
import tempfile
import os

import httpx

from app.config import settings
from app.models import (
    CaptionStatusCallback,
    LanguageResult,
    LanguageTarget,
    ProcessCallbackPayload,
    ProcessRequest,
    SegmentResult,
)
from app.services import caption_service, storage_service, transcription_service

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
    logger.info("Worker started: job_id=%s", request.job_id)

    with tempfile.TemporaryDirectory() as tmpdir:
        source_ext = os.path.splitext(request.storage_path)[-1] or ".mp4"
        source_path = os.path.join(tmpdir, f"source{source_ext}")
        audio_path = os.path.join(tmpdir, "audio.wav")

        # Step 1: Download
        logger.info("Downloading media: %s", request.storage_path)
        await storage_service.download_from_supabase(request.bucket, request.storage_path, source_path)

        # Step 2: Extract audio if video
        if request.media_type.lower() == "video":
            logger.info("Extracting audio from video")
            transcription_service.extract_audio(source_path, audio_path)
        else:
            audio_path = source_path

        # Step 3: Transcribe (single Whisper inference)
        logger.info("Transcribing audio")
        lang_hint = request.original_language if request.original_language else None
        detected_language, segments = transcription_service.transcribe(audio_path, lang_hint)
        logger.info("Transcription complete: detected_language=%s segments=%d", detected_language, len(segments))

        # Step 4: Process all languages in parallel
        async with httpx.AsyncClient() as client:
            tasks = [
                _process_language(client, request.bucket, lang, segments, dotnet_base)
                for lang in request.languages
            ]
            language_results: list[LanguageResult] = await asyncio.gather(*tasks)

        # Step 5: Final callback to .NET
        callback_url = f"{dotnet_base.rstrip('/')}{request.callback_url}"
        payload = ProcessCallbackPayload(
            detected_language=detected_language,
            segments=segments,
            language_results=language_results,
        )

        async with httpx.AsyncClient() as client:
            try:
                response = await client.post(
                    callback_url,
                    json=payload.model_dump(by_alias=True),
                    timeout=30,
                )
                response.raise_for_status()
                logger.info("Final callback sent: job_id=%s status=%s", request.job_id, response.status_code)
            except Exception as exc:
                logger.error("Final callback failed: job_id=%s error=%s", request.job_id, exc, exc_info=True)
