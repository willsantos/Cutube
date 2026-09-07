# Cutube Architecture

## Current State

The project has **evolved organically** without a planned architecture, resulting in:
- Multiple interfaces (CLI, Web, API)
- Unclear separation of concerns
- Mixed architectural patterns

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      User Interfaces                        │
├─────────────────┬───────────────────┬─────────────────────┤
│   CLI (cutube/) │   Web (Next.js)   │   API (Swagger)    │
│   Interactive   │   cutube-web/     │   src/Cutube.Api/  │
└────────┬────────┴─────────┬─────────┴─────────┬───────────┘
         │                  │                     │
         │                  │                     │
         ▼                  ▼                     ▼
┌─────────────────────────────────────────────────────────────┐
│                   Message Bus (RabbitMQ)                    │
│                   MassTransit abstraction                  │
└───────────────────────────┬───────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   Worker (Background Processing)             │
│                   src/Cutube.Worker/                      │
└───────────────────────────┬───────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   Domain Layer                              │
│                   src/Cutube.Domain/                       │
│                   - Models                                 │
│                   - Services (YtDlpHelper, FfmpegHelper)   │
│                   - Interfaces                             │
└───────────────────────────┬───────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   External Services                          │
│                   - yt-dlp (video download)                │
│                   - FFmpeg (video processing)              │
└─────────────────────────────────────────────────────────────┘
```

## Architectural Patterns

### Patterns Currently Used

1. **Worker Service Pattern**
   - `Cutube.Worker` processes messages from RabbitMQ
   - Long-running background operations
   - Decouples API from heavy processing

2. **Domain-Driven Design (DDD) Elements**
   - `Cutube.Domain` contains domain models
   - Services for business logic (YtDlpHelper, FfmpegHelper)
   - NOT full DDD - missing aggregates, repositories, domain events

3. **Result Pattern**
   - `FluentResults` for operation results
   - `ValidationResult`, `DownloadResult`, `ProcessingResult` models

4. **Message-Based Communication**
   - RabbitMQ via MassTransit
   - `Cutube.Contracts` defines message types
   - Async processing between API and Worker

### Patterns Missing or Incomplete

1. **Repository Pattern**
   - No data persistence layer (yet)
   - File system operations scattered

2. **CQRS**
   - Not implemented
   - Could be useful for download history tracking

3. **Domain Events**
   - Not used
   - Could improve decoupling

4. **Dependency Injection**
   - Used in API/Worker
   - NOT used in CLI (direct instantiation)

## Component Relationships

### Dependency Graph

```
cutube-web/ (Next.js)
    └─ SignalR ──► Cutube.Api

Cutube.Api
    ├─ ProjectReference ──► Cutube.Domain
    ├─ ProjectReference ──► Cutube.Contracts
    ├─ ProjectReference ──► cutube (CLI project!)
    └─ MassTransit/RabbitMQ ──► Queue

Cutube.Worker
    ├─ ProjectReference ──► Cutube.Contracts
    └─ MassTransit/RabbitMQ ──◄─ Queue

cutube (CLI)
    ├─ Direct file includes ──► Domain classes
    └─ Process execution ──► yt-dlp, FFmpeg

Cutube.Domain
    └─ NO external dependencies (pure .NET)
```

### Critical Issue: CLI Reference in API

```xml
<!-- In Cutube.Api.csproj -->
<ProjectReference Include="..\..\cutube\cutube.csproj" />
```

**Problem**: API directly references CLI project, creating:
- Tight coupling between API and CLI
- Shared business logic duplication
- Violation of separation of concerns

**Solution**: Move shared logic to `Cutube.Domain` or create `Cutube.Core`

## Data Flow

### Download Request Flow

```
User (Web/CLI)
    │
    ▼
API Endpoint / CLI Command
    │
    ├── Validate input
    │
    ▼
Publish DownloadMessage to RabbitMQ
    │
    ▼
Worker receives message
    │
    ├── Call YtDlpHelper.Download()
    │
    ├── Call FfmpegHelper.Cut()
    │
    ▼
Publish ProcessingResult
    │
    ▼
SignalR broadcast → Web clients
```

### Real-time Updates

```
Worker completes download
    │
    ▼
SignalR Hub broadcasts
    │
    ├─► Web clients (SignalR connection)
    └─► Update UI (React Query invalidation)
```

## Architectural Concerns

### Current Problems

1. **No Clear Layers**
   - Business logic scattered across CLI, Domain, Worker
   - API directly references CLI project

2. **Mixed Abstractions**
   - Some services use interfaces (IYtDlpService)
   - Others are static helpers (YtDlpHelper)
   - Inconsistent dependency injection

3. **No Persistence Strategy**
   - Downloads stored in file system only
   - No database for history/search
   - Recovery mode uses file scanning

4. **Testing Strategy**
   - Tests exist but coverage unclear
   - No clear test architecture

5. **Configuration Management**
   - Multiple .env files
   - User secrets in Worker
   - No centralized configuration

### Recommended Improvements

1. **Create Clear Layers**
   ```
   src/Cutube.Core/       (business logic, interfaces)
   src/Cutube.Domain/     (models, value objects)
   src/Cutube.Application/ (use cases, application services)
   src/Cutube.Infrastructure/ (yt-dlp, FFmpeg, file system)
   src/Cutube.Api/        (controllers, SignalR)
   src/Cutube.Worker/     (background processing)
   ```

2. **Remove CLI Reference**
   - Move shared logic from `cutube/` to `Cutube.Core`
   - CLI becomes thin presentation layer

3. **Integrate Persistence**
   - Add database (SQLite, PostgreSQL?)
   - Repository pattern for download history
   - Entity Framework Core

4. **Standardize DI**
   - All services use interfaces
   - Consistent registration pattern
   - Remove static helpers

5. **Improve Testing**
   - Unit tests for domain logic
   - Integration tests for API
   - E2E tests for Web (already has Playwright)

## Cross-Cutting Concerns

### Logging
- **Serilog** with structured logging
- File and console sinks
- **Gaps**: No correlation IDs, no distributed tracing

### Error Handling
- Custom exceptions in `Cutube.Api/Queuing/Exceptions/`
- **Gaps**: No global error handler, no retry policies (except Polly)

### Validation
- `ValidationResult` in Domain
- FluentResults for operation results
- **Gaps**: No validation attributes, no FluentValidation

### Configuration
- appsettings.json for .NET projects
- .env files for local development
- **Gaps**: No configuration validation, no secrets management strategy
