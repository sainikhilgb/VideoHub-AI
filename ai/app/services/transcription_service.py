import subprocess
import tempfile
import os
from typing import Optional
from faster_whisper import WhisperModel
from app.core.config import settings
from app.models.schemas import SegmentResult, WordResult


_model: Optional[WhisperModel] = None


def _get_model() -> WhisperModel:
    """Lazily loads and caches the Whisper model (loaded once per process)."""
    global _model
    if _model is None:
        _model = WhisperModel(
            settings.whisper_model_size,
            device=settings.whisper_device,
            compute_type=settings.whisper_compute_type,
        )
    return _model


def extract_audio(video_path: str, audio_path: str) -> None:
    """Extracts and normalizes audio from a video file using ffmpeg."""
    subprocess.run(
        [
            "ffmpeg", "-y", "-i", video_path,
            "-vn",                     # No video
            "-acodec", "pcm_s16le",    # 16-bit PCM
            "-ar", "16000",            # 16kHz sample rate (Whisper optimal)
            "-ac", "1",                # Mono
            audio_path,
        ],
        check=True,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )


def transcribe(audio_path: str, language: Optional[str] = None) -> tuple[str, list[SegmentResult]]:
    """
    Runs faster-whisper transcription on the given audio file.
    Returns (detected_language, list of SegmentResult).
    """
    valid_languages = {
        "af", "am", "ar", "as", "az", "ba", "be", "bg", "bn", "bo", "br", "bs", "ca", "cs", "cy", "da",
        "de", "el", "en", "es", "et", "eu", "fa", "fi", "fo", "fr", "gl", "gu", "ha", "haw", "he", "hi",
        "hr", "ht", "hu", "hy", "id", "is", "it", "ja", "jw", "ka", "kk", "km", "kn", "ko", "la", "lb",
        "ln", "lo", "lt", "lv", "mg", "mi", "mk", "ml", "mn", "mr", "ms", "mt", "my", "ne", "nl", "nn",
        "no", "oc", "pa", "pl", "ps", "pt", "ro", "ru", "sa", "sd", "si", "sk", "sl", "sn", "so", "sq",
        "sr", "su", "sv", "sw", "ta", "te", "tg", "th", "tk", "tl", "tr", "tt", "uk", "ur", "uz", "vi",
        "yi", "yo", "zh", "yue"
    }

    lang_code = language.lower().strip() if language else None
    if lang_code not in valid_languages:
        lang_code = None  # Fallback to auto-detection if invalid

    model = _get_model()
    segments_raw, info = model.transcribe(
        audio_path,
        language=lang_code,
        word_timestamps=True,
        beam_size=5,
    )

    detected_language = info.language
    segments: list[SegmentResult] = []

    for seg in segments_raw:
        words = []
        if seg.words:
            words = [
                WordResult(
                    text=w.word,
                    start=w.start,
                    end=w.end,
                    confidence=w.probability,
                )
                for w in seg.words
            ]
        segments.append(
            SegmentResult(
                start=seg.start,
                end=seg.end,
                text=seg.text.strip(),
                confidence=seg.avg_logprob,
                words=words,
            )
        )

    return detected_language, segments
