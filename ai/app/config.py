from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    supabase_url: str
    supabase_key: str
    dotnet_api_base_url: str = "http://localhost:5000"
    whisper_model_size: str = "base"  # base | small | medium | large-v3
    whisper_device: str = "cpu"       # cpu | cuda
    whisper_compute_type: str = "int8"

    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"


settings = Settings()
