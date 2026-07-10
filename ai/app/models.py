from __future__ import annotations
from typing import Optional
from pydantic import BaseModel, ConfigDict
from pydantic.alias_generators import to_camel


class CamelModel(BaseModel):
    model_config = ConfigDict(
        alias_generator=to_camel,
        populate_by_name=True,
        from_attributes=True
    )


class WordResult(CamelModel):
    text: str
    start: float
    end: float
    confidence: Optional[float] = None


class SegmentResult(CamelModel):
    start: float
    end: float
    text: str
    confidence: Optional[float] = None
    words: list[WordResult] = []


class CaptionResult(CamelModel):
    format: str   # "srt" | "vtt"
    content: str


class LanguageTarget(CamelModel):
    language_code: str
    caption_file_ids: dict[str, str]   # format → CaptionFile UUID
    folder_path: str


class ProcessRequest(CamelModel):
    job_id: str
    project_id: str
    media_id: str
    media_type: str                    # "video" | "audio"
    bucket: str
    storage_path: str
    original_language: str
    callback_url: str
    languages: list[LanguageTarget]


class CaptionStatusCallback(CamelModel):
    status: str                        # "Completed" | "Failed"
    blob_url: Optional[str] = None
    error: Optional[str] = None


class LanguageResult(CamelModel):
    language_code: str
    status: str
    error: Optional[str] = None


class ProcessCallbackPayload(CamelModel):
    detected_language: str
    segments: list[SegmentResult]
    language_results: list[LanguageResult]
