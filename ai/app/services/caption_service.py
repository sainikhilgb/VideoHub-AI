from app.models.schemas import SegmentResult


def _format_srt_time(seconds: float) -> str:
    ms = int((seconds % 1) * 1000)
    s = int(seconds) % 60
    m = int(seconds) // 60 % 60
    h = int(seconds) // 3600
    return f"{h:02}:{m:02}:{s:02},{ms:03}"


def _format_vtt_time(seconds: float) -> str:
    ms = int((seconds % 1) * 1000)
    s = int(seconds) % 60
    m = int(seconds) // 60 % 60
    h = int(seconds) // 3600
    return f"{h:02}:{m:02}:{s:02}.{ms:03}"


def generate_srt(segments: list[SegmentResult]) -> str:
    """Generates an SRT format string from transcript segments."""
    lines = []
    for i, seg in enumerate(segments, start=1):
        lines.append(str(i))
        lines.append(f"{_format_srt_time(seg.start)} --> {_format_srt_time(seg.end)}")
        lines.append(seg.text)
        lines.append("")
    return "\n".join(lines)


def generate_vtt(segments: list[SegmentResult]) -> str:
    """Generates a WebVTT format string from transcript segments."""
    lines = ["WEBVTT", ""]
    for i, seg in enumerate(segments, start=1):
        lines.append(str(i))
        lines.append(f"{_format_vtt_time(seg.start)} --> {_format_vtt_time(seg.end)}")
        lines.append(seg.text)
        lines.append("")
    return "\n".join(lines)
