import httpx
from app.core.config import settings


async def download_from_supabase(bucket: str, storage_path: str, dest_path: str) -> None:
    """Downloads a file from Supabase Storage to a local temp path."""
    url = (
        f"{settings.supabase_url.rstrip('/')}/storage/v1/object/authenticated/"
        f"{bucket}/{storage_path}"
    )
    headers = {
        "Authorization": f"Bearer {settings.supabase_key.get_secret_value()}",
        "apikey": settings.supabase_key.get_secret_value(),
    }
    async with httpx.AsyncClient(timeout=300) as client:
        async with client.stream("GET", url, headers=headers) as response:
            response.raise_for_status()
            with open(dest_path, "wb") as f:
                async for chunk in response.aiter_bytes(chunk_size=8192):
                    f.write(chunk)


async def upload_to_supabase(bucket: str, local_path: str, storage_path: str, content_type: str) -> str:
    """Uploads a local file to Supabase Storage and returns the public URL."""
    url = (
        f"{settings.supabase_url.rstrip('/')}/storage/v1/object/"
        f"{bucket}/{storage_path}"
    )
    headers = {
        "Authorization": f"Bearer {settings.supabase_key.get_secret_value()}",
        "apikey": settings.supabase_key.get_secret_value(),
        "Content-Type": content_type,
        "x-upsert": "true",
    }
    async with httpx.AsyncClient(timeout=120) as client:
        with open(local_path, "rb") as f:
            response = await client.post(url, headers=headers, content=f.read())
            response.raise_for_status()

    public_url = (
        f"{settings.supabase_url.rstrip('/')}/storage/v1/object/public/"
        f"{bucket}/{storage_path}"
    )
    return public_url
