# VideoHub AI

[![C#](https://img.shields.io/badge/C%23-178600?style=for-the-badge&logo=c-sharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![React](https://img.shields.io/badge/React-20232A?style=for-the-badge&logo=react&logoColor=61DAFB)](https://react.dev/)
[![FastAPI](https://img.shields.io/badge/FastAPI-005571?style=for-the-badge&logo=fastapi&logoColor=white)](https://fastapi.tiangolo.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white)](https://redis.io/)
[![Docker](https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)

VideoHub AI is a complete full-stack web application and processing pipeline for automated transcription, captioning, translation, and background job handling. Built on a modular monorepo architecture, the application is divided into three key services:

1. **Backend API (`src/`)**: A modular monolith ASP.NET Core Web API handling authentication, project metadata management, and background job scheduling.
2. **AI Worker (`ai/`)**: A stateless FastAPI service utilizing `faster-whisper` for machine learning transcript generation.
3. **Web Interface (`web/`)**: A modern React SPA built with Vite for managing projects, uploads, and editing subtitles.

---

## Key Infrastructure Features (Phase 1)
- **ASP.NET Core Web API**: Structured as a clean architecture modular monolith.
- **Relational Persistence**: PostgreSQL database mapping via EF Core.
- **Distributed Caching**: Redis cache abstraction layer.
- **Background Jobs**: Hangfire queue management with persistent PostgreSQL state storage.
- **Structured Observability**: Serilog logging, RFC 7807 Problem Details responses, and OpenTelemetry instrumentation.
- **Validation**: Strict request validation using FluentValidation.
- **Secrets Management**: Configuration integration utilizing environment-specific local `.env` files.

## Solution Overview

- `VideoHub.sln` - solution entry point
- `src/VideoHub.Api` - main ASP.NET Core Web API project

## Architecture

The application uses a single deployed Web API project organized by feature and responsibility:

- `Api` - HTTP controllers
- `Application` - commands, queries, DTOs, validators, and background job services
- `Domain` - core entities and business data model
- `Infrastructure` - persistence, caching, storage, logging, middleware, and dependency injection

This structure keeps the codebase modular without splitting into multiple deployable services yet.

## Project Structure

### API
- `Api/Controllers`
  - `HealthController.cs` - API health check
  - `ProjectController.cs` - CRUD operations for Projects
  - `ProjectMediaController.cs` - Media upload endpoint

### Application
- `Application/Exceptions`
  - `NotFoundException.cs`
  - `BadRequestException.cs`
  - `UnauthorizedException.cs`
  - `ForbiddenException.cs`
  - `ConflictException.cs`
  - `ServiceUnavailableException.cs`
  - `GatewayTimeoutException.cs`
- `Application/BackgroundJobs`
  - `IBackgroundJobService.cs`
  - `BackgroundJobService.cs`
  - `BackgroundJobsController.cs`
- `Application/Commands`
  - `SubmitUploadCommand.cs`
- `Application/Queries`
  - `GetJobStatusQuery.cs`
- `Application/Project`
  - `IProjectService.cs`
  - `ProjectService.cs`
  - `DTOs/ProjectRequestDto.cs`
  - `DTOs/ProjectResponseDto.cs`
- `Application/Uploads`
  - `IMediaUploadService.cs`
  - `MediaUploadService.cs`
  - `DTOs/UploadRequestDto.cs`
  - `DTOs/UploadMediaRequestDto.cs`
  - `DTOs/UploadMediaResponseDto.cs`
- `Application/Validators`
  - `SubmitUploadCommandValidator.cs`
  - `UploadRequestDtoValidator.cs`
  - `UploadMediaRequestDtoValidator.cs`

### Domain
- `Domain/Entities`
  - `User.cs`
  - `Project.cs`
  - `MediaFile.cs`
  - `Transcript.cs`
  - `TranscriptSegment.cs`
  - `Word.cs`
  - `Speaker.cs`
  - `Translation.cs`
  - `CaptionFile.cs`
  - `Job.cs`
  - `AuditLog.cs`
- `Domain/Jobs`
  - `JobStatuses.cs`
  - `JobTypes.cs`
- `Domain/Media`
  - `MediaFileStatuses.cs`
  - `MediaFileTypes.cs`

### Infrastructure
- `Infrastructure/Abstractions`
  - `IBlobStorage.cs`
  - `ICacheService.cs`
  - `IMediaStoragePathBuilder.cs`
  - `IProjectRepository.cs`
  - `IRepository.cs`
  - `IUnitOfWork.cs`
- `Infrastructure/Persistence`
  - `AppDbContext.cs`
  - `AppDbContextFactory.cs`
  - `UnitOfWork.cs`
  - `Repositories/EfRepository.cs`
  - `Repositories/ProjectRepositoryService.cs`
- `Infrastructure/Storage`
  - `LocalBlobStorage.cs`
  - `MediaStoragePathBuilder.cs`
  - `SupabaseBlobStorage.cs`
- `Infrastructure/Caching`
  - `RedisCacheService.cs`
- `Infrastructure/Middleware`
  - `CorrelationIdMiddleware.cs`
  - `ExceptionHandlingMiddleware.cs`
- `Infrastructure/BackgroundJobs`
  - `HangfireJobExecutionLoggingFilter.cs`
- `Infrastructure/DependencyInjection`
  - `InfrastructureExtensions.cs`
- `Infrastructure/Extensions`
  - `ApplicationBuilderExtensions.cs`
- `Infrastructure/Options`
  - `BlobStorageOptions.cs`
  - `HangfireSettings.cs`
  - `MediaUploadOptions.cs`

## Core Domain Model

The current data model supports the transcription workflow:

- `User` owns projects
- `Project` represents one upload session
- `MediaFile` stores uploaded media metadata
- `Transcript` stores transcript versions by language
- `TranscriptSegment` stores speech chunks
- `Word` stores word-level timing
- `Speaker` stores diarized speakers
- `Translation` stores translated transcript variants
- `CaptionFile` stores generated caption artifacts
- `Job` tracks background work
- `AuditLog` is reserved for security/audit events

## API Reference & Documentation

Detailed endpoint documentation, request/response schemas, and interactive testing tools are available via Swagger UI.

* **Swagger UI URL (Local):** `http://localhost:5000/swagger` or `https://localhost:5001/swagger`
* **OpenAPI Specification (JSON):** `http://localhost:5000/swagger/v1/swagger.json`

### API Architecture Overview

The API is organized into the following core functional modules:

| Module | Base Path | Description | Key Features |
| :--- | :--- | :--- | :--- |
| **Health Check** | `/api/health` | Service status checks | Health monitoring |
| **Authentication** | `/api/v1/auth` | Identity & session management | JWT login, secure cookie refresh, registration, profiles |
| **Projects** | `/api/v1/projects` | User workspace organization | CRUD project resources isolated by authenticated user |
| **Media Upload** | `/api/v1/projects/{id}/media` | Media ingestion pipeline | Chunked/multipart uploads, background job dispatch |
| **Transcripts** | `/api/v1/projects/{id}/transcript` | Transcript & caption services | JSON editing, SRT/VTT generation |
| **Background Jobs** | `/api/jobs` | Background task orchestration | Hangfire trigger test endpoints |

### Quick Start API Test

To quickly verify API connectivity after starting the application:

1. **Authenticate** to receive a JWT access token:
   ```bash
   curl -X POST http://localhost:5000/api/v1/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email": "user@example.com", "password": "SecurePassword123!"}'
   ```
2. **Access a Protected Endpoint** using the returned token:
   ```bash
   curl -H "Authorization: Bearer <YOUR_ACCESS_TOKEN>" http://localhost:5000/api/v1/projects
   ```

## Hangfire

Hangfire is configured with PostgreSQL storage and a dashboard route driven by configuration.

Current behavior:
- Hangfire server starts with the API
- dashboard is mapped from `Hangfire:DashboardPath`
- job execution logs are captured through the existing Serilog/ILogger setup

The dashboard is currently public because authentication/authorization is not implemented yet.

## Logging and Observability

The app currently includes:
- Serilog structured logging
- console logging for warnings and errors
- file logging for informational and above events
- correlation ID middleware
- global exception middleware
- RFC 7807 Problem Details responses
- OpenTelemetry tracing and metrics

## Configuration

Local configuration is loaded from `.env` and standard ASP.NET Core configuration.

Important environment variables:

- `ConnectionStrings__DefaultConnection`
- `Redis__Configuration`
- `Redis__InstanceName`
- `BlobStorage__Provider`
- `BlobStorage__LocalPath`
- `BlobStorage__SupabaseUrl`
- `BlobStorage__SupabaseKey`
- `BlobStorage__BucketName`
- `Hangfire__DashboardPath`
- `OpenTelemetry__OtlpEndpoint`
- `Serilog__FilePath`

Example `.env` values:

```bash
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=VideoHub;Username=postgres;Password=postgres
Redis__Configuration=localhost:6379
Redis__InstanceName=VideoHub:
BlobStorage__Provider=Local
BlobStorage__LocalPath=blobs
BlobStorage__SupabaseUrl=
BlobStorage__SupabaseKey=
BlobStorage__BucketName=
Hangfire__DashboardPath=/hangfire
OpenTelemetry__OtlpEndpoint=
Serilog__FilePath=logs/videohub-.log
```

Supabase Storage is now supported through the existing `IBlobStorage` abstraction. Set `BlobStorage__Provider=Supabase` and provide the Supabase URL, service key, and bucket name to switch from local file storage to Supabase Storage.

Supported upload types:
- Video: `mp4`, `mov`, `avi`, `mkv`, `webm`
- Audio: `mp3`, `wav`, `m4a`, `aac`, `flac`

Current upload limits:
- Video: `2 GB`
- Audio: `500 MB`

## AI Service (Python / FastAPI)

Located in the `ai` directory, this stateless AI worker handles transcription and caption generation using Faster-Whisper.

### Setup and Running the AI Service
1. **Navigate to the AI folder**:
   ```bash
   cd ai
   ```
2. **Create a virtual environment**:
   ```bash
   python3 -m venv .venv
   ```
3. **Install dependencies**:
   ```bash
   ./.venv/bin/pip install -r requirements.txt
   ```
4. **Configure environment**:
   Create a `.env` file inside the `ai` directory containing:
   ```bash
   supabase_url=YOUR_SUPABASE_URL
   supabase_key=YOUR_SUPABASE_KEY
   dotnet_api_base_url=http://localhost:5000
   ```
5. **Run the development server**:
   ```bash
   ./.venv/bin/uvicorn main:app --reload --port 8000
   ```

## Frontend Web UI (React / Vite)

Located in the `web` directory, this is the modern responsive frontend application for managing speech-to-text, projects, and subtitles.

### Setup and Running the Frontend

1. **Navigate to the web folder**:
   ```bash
   cd web
   ```
2. **Install dependencies**:
   ```bash
   npm install
   ```
3. **Configure environment**:
   Manually create a `.env` file inside the `web` directory with the required `VITE_API_BASE_URL` value (not tracked by Git, per `web/.gitignore`):
   ```bash
   VITE_API_BASE_URL=http://localhost:5000/api
   ```
4. **Run the development server**:
   ```bash
   npm run dev
   ```
5. **Run checks**:
   ```bash
   npm run build  # Compiles TypeScript and builds production artifacts
   npm run lint   # Runs ESLint rules checking
   npm run format # Formats codebase using Prettier
   ```

## Running Locally

### Prerequisites
- .NET SDK 8
- PostgreSQL
- Redis if you want cache support enabled locally
- ASP.NET Core development certificate for HTTPS if you want to use the HTTPS profile

### Restore

```bash
dotnet restore VideoHub.sln
```

### Build

```bash
dotnet build VideoHub.sln -c Debug
```

### Database Migration

```bash
dotnet ef migrations add InitialCreate --project src/VideoHub.Api --startup-project src/VideoHub.Api --output-dir Migrations
dotnet ef database update --project src/VideoHub.Api --startup-project src/VideoHub.Api
```

### Run

```bash
dotnet run --project src/VideoHub.Api
```

The app is configured to listen on:
- `http://localhost:5000`
- `https://localhost:5001`

Swagger is available at:
- `http://localhost:5000/swagger`
- `https://localhost:5001/swagger`

If HTTPS does not work locally, trust the development certificate:

```bash
dotnet dev-certs https --trust
```

## Notes

- The project is currently a single deployable Web API with modular folders, not separate projects per layer.
- The implementation is intentionally infrastructure-first; business feature work should build on top of this foundation.
- `appsettings.json` is kept minimal and non-secret; secrets and environment-specific values should live in `.env` or production environment variables.
- Authentication and project-ownership enforcement are fully implemented across all media upload, project, and background job polling endpoints.

## Production Deployment (Vercel & Render)

This application is configured for a multi-platform production deployment using native continuous deployment (CD) on merge to your deployment branch:

### 1. Frontend Web UI (Vercel)
Connect your repository to Vercel and point the build configuration to the `web` subdirectory:
- **Framework Preset**: Vite
- **Root Directory**: `web`
- **Build Command**: `npm run build`
- **Output Directory**: `dist`
- **Environment Variables**:
  - `VITE_API_BASE_URL`: The URL of your deployed .NET API on Render (e.g., `https://videohub-api.onrender.com/api`).

### 2. API Backend (.NET on Render)
Create a new **Web Service** on Render connected to your repository:
- **Environment**: Docker
- **Docker Build Context**: `.` (Repository root)
- **Dockerfile Path**: `src/VideoHub.Api/Dockerfile`
- **Environment Variables**:
  - `ConnectionStrings__DefaultConnection`: Your Supabase PostgreSQL Connection String.
  - `BlobStorage__Provider`: `Supabase`
  - `BlobStorage__SupabaseUrl`: Your Supabase URL.
  - `BlobStorage__SupabaseKey`: Your Supabase service role secret key.
  - `BlobStorage__BucketName`: Your Supabase Storage bucket name.
  - `Jwt__Secret`: A secure random password string.
  - `Jwt__Issuer`: `VideoHubAI`
  - `Jwt__Audience`: `VideoHubAI.Clients`
  - `Jwt__ExpiryMinutes`: `60`
  - `Jwt__RefreshTokenExpiryDays`: `7`
  - `Jwt__ClockSkewSeconds`: `5`
  - `AiService__BaseUrl`: The URL of your deployed FastAPI worker on Render (e.g., `https://videohub-ai-worker.onrender.com`).

### 3. AI Worker (FastAPI on Render)
Create a second **Web Service** on Render connected to your repository:
- **Environment**: Docker
- **Docker Build Context**: `.` (Repository root)
- **Dockerfile Path**: `ai/Dockerfile`
- **Environment Variables**:
  - `supabase_url`: Your Supabase URL.
  - `supabase_key`: Your Supabase service role secret key.
  - `dotnet_api_base_url`: The URL of your deployed .NET API on Render (e.g., `https://videohub-api.onrender.com`).
- **Hardware Recommendations**: It is recommended to use at least a Starter instance (1GB+ RAM) to avoid Out Of Memory crashes when Whisper processes larger audio files.

