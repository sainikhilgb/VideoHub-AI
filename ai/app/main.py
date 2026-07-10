import uuid
import time
import logging
from contextlib import asynccontextmanager
from fastapi import FastAPI, Request
from starlette.middleware.base import BaseHTTPMiddleware

from app.api.v1.processing import router as processing_router
from app.core.logging import setup_logging, log_context

logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info("VideoHub AI Service starting up")
    yield
    logger.info("VideoHub AI Service shutting down")


class StructuredLoggingMiddleware(BaseHTTPMiddleware):
    async def dispatch(self, request: Request, call_next):
        # Extract or generate Correlation ID
        correlation_id = request.headers.get("X-Correlation-ID") or request.headers.get("x-correlation-id")
        if not correlation_id:
            correlation_id = str(uuid.uuid4())

        # Generate unique Request ID
        request_id = str(uuid.uuid4())

        # Build initial log context
        ctx = {
            "CorrelationId": correlation_id,
            "RequestId": request_id,
            "ServiceName": "VideoHub.Ai"
        }
        
        token = log_context.set(ctx)
        start_time = time.perf_counter()
        
        logger.info("HTTP request started: %s %s", request.method, request.url.path)
        
        try:
            response = await call_next(request)
            
            elapsed = (time.perf_counter() - start_time) * 1000.0
            ctx_ended = log_context.get()
            ctx_ended = ctx_ended.copy() if ctx_ended is not None else {}
            ctx_ended["Duration"] = round(elapsed, 3)
            ctx_ended["Operation"] = f"HTTP {request.method} {request.url.path}"
            log_context.set(ctx_ended)
            
            response.headers["X-Correlation-ID"] = correlation_id
            
            logger.info("HTTP request completed: %s %s -> %s", request.method, request.url.path, response.status_code)
            return response
        except Exception as exc:
            elapsed = (time.perf_counter() - start_time) * 1000.0
            ctx_ended = log_context.get()
            ctx_ended = ctx_ended.copy() if ctx_ended is not None else {}
            ctx_ended["Duration"] = round(elapsed, 3)
            ctx_ended["Operation"] = f"HTTP {request.method} {request.url.path}"
            log_context.set(ctx_ended)
            logger.error("HTTP request failed: %s %s — %s", request.method, request.url.path, exc, exc_info=True)
            raise
        finally:
            log_context.reset(token)


def create_app() -> FastAPI:
    setup_logging()
    
    app = FastAPI(
        title="VideoHub AI Service",
        description="Stateless AI worker for transcription and caption generation.",
        version="1.0.0",
        lifespan=lifespan,
    )
    
    app.add_middleware(StructuredLoggingMiddleware)
    app.include_router(processing_router)
    
    return app


app = create_app()
