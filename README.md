# VideoHub AI

VideoHub AI is a modular-monolith ASP.NET Core backend for automated transcription, captioning, translation, and background job processing.

The current solution focuses on the Phase 1 infrastructure foundation:
- ASP.NET Core Web API
- PostgreSQL with EF Core
- Redis cache abstraction
- Hangfire background processing
- Serilog structured logging
- OpenTelemetry tracing and metrics
- FluentValidation
- Problem Details error responses
- Local `.env`-based configuration loading

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

## Existing API Endpoints

### Health
- `GET /api/health`
  - Returns application health status

### Authentication & Identity
- `POST /api/v1/auth/register`
  - Register a new user account with secure password complexity checks.
- `POST /api/v1/auth/login`
  - Authenticate credentials. Returns a short-lived JSON Web Token (JWT) in the response body and writes a secure, `HttpOnly`, `SameSite=Lax` refresh token cookie.
- `POST /api/v1/auth/refresh`
  - Rotates refresh session cookies and issues a new memory-bound JWT access token.
- `POST /api/v1/auth/logout`
  - Revokes refresh tokens in the database and cleans up browser cookies.
- `GET /api/v1/auth/me`
  - Retrieves the active user's profile details. Requires Bearer JWT authentication header.

### Projects
- `GET /api/v1/projects`
  - Retrieve list of projects belonging *only* to the authenticated user.
- `GET /api/v1/projects/{id}`
  - Retrieve a user-owned project by its unique ID (403 Forbidden on non-owned projects).
- `POST /api/v1/projects`
  - Create a new project assigned directly to the authenticated user.
- `PUT /api/v1/projects/{id}`
  - Update details of a user-owned project (403 Forbidden on non-owned projects).
- `DELETE /api/v1/projects/{id}`
  - Delete a user-owned project (403 Forbidden on non-owned projects).

### Media Upload
- `POST /api/v1/projects/{projectId}/media`
  - Accepts `multipart/form-data` with files. Requires project ownership.
  - Uploads video/audio files to the configured blob storage provider (Local/Supabase).
  - Stores metadata in PostgreSQL.
  - Creates a processing job record.
  - Enqueues a Hangfire media-processing placeholder job.
  - Returns `202 Accepted`.

### Background Jobs
- `POST /api/jobs/hello-world`
  - Queues a fire-and-forget Hangfire job, returns `202 Accepted`
- `POST /api/jobs/hello-world/delayed/{delaySeconds}`
  - Queues a delayed Hangfire job, returns `202 Accepted`
- `POST /api/jobs/hello-world/recurring`
  - Registers a recurring Hangfire job, returns `202 Accepted`
- `POST /api/jobs/hello-world/continuation`
  - Queues a continuation job chain, returns `202 Accepted`

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
