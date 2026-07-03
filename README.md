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
  - `HealthController`

### Application
- `Application/Commands`
  - `SubmitUploadCommand`
- `Application/Queries`
  - `GetJobStatusQuery`
- `Application/DTOs`
  - `UploadRequestDto`
- `Application/Validators`
  - `SubmitUploadCommandValidator`
  - `UploadRequestDtoValidator`
- `Application/BackgroundJobs`
  - `IBackgroundJobService`
  - `BackgroundJobService`
  - `BackgroundJobsController`

### Domain
- `User`
- `Project`
- `MediaFile`
- `Transcript`
- `TranscriptSegment`
- `Word`
- `Speaker`
- `Translation`
- `CaptionFile`
- `Job`
- `AuditLog`

### Infrastructure
- `Infrastructure/Persistence`
  - `AppDbContext`
  - `AppDbContextFactory`
  - `UnitOfWork`
  - `Repositories/EfRepository`
- `Infrastructure/Storage`
  - `IBlobStorage`
  - `LocalBlobStorage`
  - `SupabaseBlobStorage`
- `Infrastructure/Caching`
  - `ICacheService`
  - `RedisCacheService`
- `Infrastructure/Middleware`
  - `CorrelationIdMiddleware`
  - `ExceptionHandlingMiddleware`
- `Infrastructure/BackgroundJobs`
  - `HangfireJobExecutionLoggingFilter`
- `Infrastructure/DependencyInjection`
  - `InfrastructureExtensions`
- `Infrastructure/Extensions`
  - `ApplicationBuilderExtensions`
- `Infrastructure/Configuration`
  - `EnvFileLoader`
- `Infrastructure/Options`
  - `HangfireSettings`

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

### Background Jobs
- `POST /api/jobs/hello-world`
  - queues a fire-and-forget Hangfire job
- `POST /api/jobs/hello-world/delayed/{delaySeconds}`
  - queues a delayed Hangfire job
- `POST /api/jobs/hello-world/recurring`
  - registers a recurring Hangfire job
- `POST /api/jobs/hello-world/continuation`
  - queues a continuation job chain

### Media Upload
- `POST /api/v1/projects/{projectId}/media`
  - accepts `multipart/form-data`
  - uploads video/audio files to the configured blob storage provider
  - stores metadata in PostgreSQL
  - creates a processing job record
  - enqueues a Hangfire media-processing placeholder job
  - returns `202 Accepted`

Each endpoint returns `202 Accepted`.

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
- Authentication and project-ownership enforcement for the media upload endpoint are intentionally deferred for the next pass, per the current implementation request.
