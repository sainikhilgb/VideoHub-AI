import logging
from fastapi import APIRouter, BackgroundTasks
from fastapi.responses import JSONResponse

from app.models.schemas import ProcessRequest
from app.services import transcription_worker
from app.core.logging import log_context

router = APIRouter()
logger = logging.getLogger(__name__)


@router.get("/health")
async def health():
    return {"status": "ok"}


@router.post("/process", status_code=202)
async def process(request: ProcessRequest, background_tasks: BackgroundTasks):
    """
    Accepts a caption generation job from the .NET backend.
    Returns 202 Accepted immediately and processes in the background.
    """
    # Enrich the local log context for request lifecycle logging
    ctx = log_context.get().copy()
    ctx.update({
        "JobId": request.job_id,
        "ProjectId": request.project_id,
        "MediaId": request.media_id,
    })
    log_context.set(ctx)

    logger.info("Process request received: job_id=%s languages=%s",
                request.job_id, [l.language_code for l in request.languages])

    # Pass the worker background task execution
    background_tasks.add_task(transcription_worker.run, request)

    return JSONResponse(
        status_code=202,
        content={"message": "Processing started", "jobId": request.job_id},
    )
