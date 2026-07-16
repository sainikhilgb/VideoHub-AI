import json
import logging
import time
from contextlib import contextmanager
from datetime import datetime, timezone
from contextvars import ContextVar
from typing import Any, Generator, Optional

# Context variables for structured logging
log_context: ContextVar[Optional[dict[str, Any]]] = ContextVar("log_context", default=None)

class StructuredFormatter(logging.Formatter):
    def format(self, record: logging.LogRecord) -> str:
        ctx = log_context.get() or {}
        
        # Override fields if present in log record or fallback to context
        duration = getattr(record, "duration", None)
        if duration is None:
            duration = ctx.get("Duration")
            
        operation = getattr(record, "operation", None)
        if operation is None:
            operation = ctx.get("Operation")

        message = record.getMessage()
        
        exception_str = None
        if record.exc_info:
            exception_str = self.formatException(record.exc_info)
        elif record.exc_text:
            exception_str = record.exc_text

        # Map python log levels to Serilog equivalents
        log_level_map = {
            "DEBUG": "Debug",
            "INFO": "Information",
            "WARNING": "Warning",
            "ERROR": "Error",
            "CRITICAL": "Fatal"
        }
        level_name = log_level_map.get(record.levelname, record.levelname)

        log_entry = {
            "Timestamp": datetime.fromtimestamp(record.created, tz=timezone.utc).isoformat(timespec='milliseconds').replace("+00:00", "Z"),
            "LogLevel": level_name,
            "ServiceName": "VideoHub.Ai",
            "CorrelationId": ctx.get("CorrelationId") or getattr(record, "correlation_id", None),
            "RequestId": ctx.get("RequestId") or getattr(record, "request_id", None),
            "JobId": ctx.get("JobId") or getattr(record, "job_id", None),
            "ProjectId": ctx.get("ProjectId") or getattr(record, "project_id", None),
            "MediaId": ctx.get("MediaId") or getattr(record, "media_id", None),
            "Operation": operation,
            "Duration": duration,
            "Message": message,
            "Exception": exception_str,
        }
        
        # Clean up None values to keep logs clean
        cleaned_entry = {k: v for k, v in log_entry.items() if v is not None}
        return json.dumps(cleaned_entry)


def setup_logging(level: int = logging.INFO) -> None:
    root_logger = logging.getLogger()
    for handler in root_logger.handlers[:]:
        root_logger.removeHandler(handler)
        
    handler = logging.StreamHandler()
    handler.setFormatter(StructuredFormatter())
    root_logger.addHandler(handler)
    root_logger.setLevel(level)
    
    # Silence excessive logging from libraries
    logging.getLogger("uvicorn").setLevel(logging.WARNING)
    logging.getLogger("uvicorn.access").setLevel(logging.WARNING)
    logging.getLogger("fastapi").setLevel(logging.WARNING)


@contextmanager
def log_operation(operation: str, extra_context: dict[str, Any] = None) -> Generator[None, None, None]:
    start_time = time.perf_counter()
    ctx = log_context.get()
    ctx = ctx.copy() if ctx is not None else {}
    ctx["Operation"] = operation
    if extra_context:
        ctx.update(extra_context)
    
    token = log_context.set(ctx)
    logger = logging.getLogger("VideoHub.Ai.Operation")
    logger.info(f"Operation started: {operation}")
    
    try:
        yield
        elapsed = (time.perf_counter() - start_time) * 1000.0  # in ms
        ctx_ended = log_context.get()
        ctx_ended = ctx_ended.copy() if ctx_ended is not None else {}
        ctx_ended["Duration"] = round(elapsed, 3)
        log_context.set(ctx_ended)
        logger.info(f"Operation completed: {operation}")
    except Exception as exc:
        elapsed = (time.perf_counter() - start_time) * 1000.0
        ctx_ended = log_context.get()
        ctx_ended = ctx_ended.copy() if ctx_ended is not None else {}
        ctx_ended["Duration"] = round(elapsed, 3)
        log_context.set(ctx_ended)
        logger.error(f"Operation failed: {operation} — {exc}", exc_info=True)
        raise
    finally:
        log_context.reset(token)
