import logging
import asyncio
from contextlib import asynccontextmanager

from fastapi import FastAPI, BackgroundTasks
from fastapi.responses import JSONResponse

from app.models import ProcessRequest
from app.services import transcription_worker

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s — %(message)s")
logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info("VideoHub AI Service starting up")
    yield
    logger.info("VideoHub AI Service shutting down")


app = FastAPI(
    title="VideoHub AI Service",
    description="Stateless AI worker for transcription and caption generation.",
    version="1.0.0",
    lifespan=lifespan,
)


@app.get("/health")
async def health():
    return {"status": "ok"}


@app.post("/process", status_code=202)
async def process(request: ProcessRequest, background_tasks: BackgroundTasks):
    """
    Accepts a caption generation job from the .NET backend.
    Returns 202 Accepted immediately and processes in the background.
    """
    logger.info("Process request received: job_id=%s languages=%s",
                request.job_id, [l.language_code for l in request.languages])

    background_tasks.add_task(transcription_worker.run, request)

    return JSONResponse(
        status_code=202,
        content={"message": "Processing started", "jobId": request.job_id},
    )
