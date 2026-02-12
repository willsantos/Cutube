# Cutube Project Structure

## Root Directory

```
Cutube/
├── .specs/                    # ✨ Spec-driven development docs
│   ├── project/               # Project-level docs
│   ├── codebase/              # Brownfield analysis
│   └── features/              # Feature specifications
│
├── .agents/                   # Agent configurations (legacy)
├── .beads/                    # Beads task tracking
├── .github/                   # GitHub Actions workflows
├── .githooks/                 # Git hooks (installed via install-hooks.sh)
│
├── AGENTS.md                  # Agent instructions (legacy)
├── CHANGELOG.md              # Version history
├── GITHOOKS.md              # Git hooks documentation
├── README.md                 # Project overview
├── .gitignore               # Git ignore rules
├── .tool-versions           # asdf version config
├── cutube.sln              # .NET solution file
│
├── Assets/                   # Screenshots, images for docs
├── docs/                    # User/developer documentation
├── scripts/                 # Utility scripts
│   ├── docker-compose-up.sh
│   ├── install.sh
│   └── install.ps1
│
├── plans/                   # Architecture plans (legacy?)
├── logs/                    # Application logs
├── downloads/               # Default download location
│
├── docker/                  # Docker configurations
├── docker-compose.yml       # RabbitMQ, services
│
├── cutube/                  # ❌ OLD CLI app (59 .cs files)
├── cutube-web/              # ✅ NEW Next.js web app
│
├── src/                     # ✅ NEW .NET modular backend
│   ├── Cutube.Api/          # ASP.NET Core Web API
│   ├── Cutube.Worker/       # Background worker (RabbitMQ consumer)
│   ├── Cutube.Domain/       # Domain models & services
│   └── Cutube.Contracts/    # Message contracts, DTOs
│
├── tests/                   # ✅ Test projects
│   ├── Cutube.Tests/        # CLI tests
│   ├── Cutube.Domain.Tests/ # Domain tests
│   ├── Cutube.Api.Tests/    # API tests
│   └── Cutube.Worker.Tests/ # Worker tests
│
└── TestResults/             # Test output
```

## Legacy CLI (cutube/)

### Purpose
Original console application for downloading and cutting videos.

### Structure
```
cutube/
├── Program.cs              # Entry point, command parsing
├── ProgramWorkflow.cs      # Interactive mode flow
├── DomainWorkflow.cs       # Download domain operations
│
├── Configuration/           # Settings, app configuration
├── ErrorHandling/          # Custom exceptions
├── Infrastructure/         # External services (yt-dlp, FFmpeg)
├── Logging/                # Logging setup
├── Recovery/               # Resume mode logic
├── Services/               # Business services
├── Validation/             # Input validation
│
├── *.cs helpers            # Utility classes
│   ├── TimeHelper.cs
│   ├── YtDlpHelper.cs
│   ├── FfmpegHelper.cs
│   └── ...
│
└── cutube.csproj           # Project file
```

### Problems
1. ❌ **Contains business logic** that should be in `Cutube.Domain`
2. ❌ **Directly referenced by `Cutube.Api`** (tight coupling)
3. ❌ **No clear separation** of concerns
4. ❌ **59 files** - large, monolithic

### Recommendation
- Extract shared logic to `Cutube.Core`
- Keep CLI as thin presentation layer
- Remove API reference to CLI project

## Next.js Web App (cutube-web/)

### Purpose
Modern web interface for Cutube.

### Structure
```
cutube-web/
├── app/                    # Next.js App Router
│   ├── page.tsx            # Home page
│   ├── layout.tsx          # Root layout
│   ├── downloads/          # Downloads pages
│   │   ├── page.tsx        # Downloads list
│   │   └── [id]/page.tsx   # Download detail
│   └── monitor/            # Real-time monitoring
│
├── components/             # React components
│   ├── ui/                # shadcn/ui components
│   ├── downloads/          # Download-related components
│   └── ...
│
├── lib/                   # Utilities, helpers
├── hooks/                 # Custom React hooks
├── types/                 # TypeScript type definitions
│
├── e2e/                   # Playwright E2E tests
├── public/                # Static assets
│
├── next.config.ts         # Next.js configuration
├── tailwind.config.ts     # Tailwind CSS v4 config
├── tsconfig.json          # TypeScript configuration
├── playwright.config.ts    # Playwright configuration
├── package.json           # Dependencies
└── pnpm-lock.yaml         # Lockfile
```

### Key Features
- ✅ **Modern UI** with shadcn/ui
- ✅ **Real-time updates** via SignalR
- ✅ **Server state** via React Query
- ✅ **E2E tests** with Playwright
- ✅ **Type-safe** with TypeScript

### Tech Stack
- **Framework**: Next.js 16.1.6 (App Router)
- **UI**: React 19, Tailwind CSS 4, shadcn/ui
- **State**: React Query, React Hook Form
- **Real-time**: SignalR client
- **Testing**: Playwright

## .NET Backend (src/)

### Overview
Modular backend architecture with message-based processing.

### Cutube.Api

**Purpose**: HTTP API for web and CLI clients.

```
Cutube.Api/
├── Controllers/            # ❓ Not present? (check if using minimal APIs)
├── Hubs/                  # SignalR hubs for real-time updates
├── HealthChecks/          # Health check endpoints
├── Models/                # API models, DTOs
├── DTOs/                  # Data transfer objects
├── Queuing/               # Message publishing logic
│   └── Exceptions/        # Queue-related exceptions
│
├── Program.cs             # App configuration, middleware
├── appsettings.json       # Configuration
├── appsettings.Development.json
└── Cutube.Api.csproj      # References: Domain, Contracts, CLI (❌)
```

**Dependencies**:
- ✅ `Cutube.Domain`
- ✅ `Cutube.Contracts`
- ❌ `cutube` (legacy CLI project)

**Issues**:
- Direct reference to CLI creates tight coupling
- Should interface through Domain layer

### Cutube.Worker

**Purpose**: Background processing of download messages.

```
Cutube.Worker/
├── Services/              # Message consumers
├── Configuration/          # Worker settings
│
├── Program.cs             # Worker host configuration
├── Worker.cs              # BackgroundService implementation
├── appsettings.json       # Configuration
├── appsettings.Development.json
└── Cutube.Worker.csproj   # References: Contracts
```

**Dependencies**:
- ✅ `Cutube.Contracts`
- ✅ MassTransit, RabbitMQ
- ✅ Serilog (logging)

**Architecture**:
- ✅ Clean separation via messages
- ✅ No direct API coupling
- ✅ Resilience with Polly

### Cutube.Domain

**Purpose**: Core domain models, interfaces, services.

```
Cutube.Domain/
├── Models/                # Domain entities, value objects
│   ├── DownloadRequest.cs
│   ├── VideoMetadata.cs
│   ├── DownloadProgress.cs
│   └── ...
│
├── Services/              # Domain services
│   ├── IYtDlpService.cs
│   └── ...
│
├── Interfaces/            # Service interfaces
│   ├── IYtDlpService.cs
│   ├── IFfmpegHelper.cs
│   └── ...
│
├── Helpers/               # ❓ Utility classes? (check contents)
│
└── Cutube.Domain.csproj   # No external dependencies (pure .NET)
```

**Dependencies**:
- ✅ None (pure .NET, good!)

**Issues**:
- Some helpers might be in CLI instead of here
- Missing domain events for decoupling

### Cutube.Contracts

**Purpose**: Shared message contracts, DTOs.

```
Cutube.Contracts/
├── Messages/              # MassTransit message types
│   └── DownloadMessage.cs
│
├── Configuration/          # Options classes
│   └── RabbitMqOptions.cs
│
└── Cutube.Contracts.csproj # No external dependencies
```

**Dependencies**:
- ✅ None (good!)

**Design**:
- ✅ Clean contract-first approach
- ✅ Used by API and Worker

## Tests (tests/)

### Structure
```
tests/
├── Cutube.Tests/          # CLI tests
├── Cutube.Domain.Tests/   # Domain tests
├── Cutube.Api.Tests/      # API tests
└── Cutube.Worker.Tests/   # Worker tests
```

### Test Framework
- **xUnit** (inferred)
- Coverage unknown
- ❌ No integration test project
- ✅ E2E tests in `cutube-web/e2e/`

## Configuration Files

### .NET
```
cutube.sln                # Solution file
.tool-versions            # asdf version config
docker-compose.yml        # RabbitMQ, services
.env.example              # Environment variables template
```

### Next.js
```
cutube-web/package.json    # Dependencies
cutube-web/next.config.ts # Next.js config
cutube-web/tsconfig.json   # TypeScript config
cutube-web/tailwind.config.ts # Tailwind v4 config
cutube-web/playwright.config.ts # E2E test config
```

### Git
```
.gitignore               # Ignore patterns
.github/workflows/       # CI/CD pipelines
.githooks/              # Git hook scripts
install-hooks.sh        # Hook installer
```

## Recommended Restructuring

### Phase 1: Extract Core Logic

```
src/
├── Cutube.Core/          # ✨ NEW: Shared business logic
│   ├── Services/         # DownloadService, ProcessingService
│   ├── Interfaces/       # IVideoService, IProcessingService
│   └── Exceptions/       # Domain exceptions
│
├── Cutube.Domain/        # Keep: Models, value objects
├── Cutube.Application/   # ✨ NEW: Use cases, application services
├── Cutube.Infrastructure/# ✨ NEW: yt-dlp, FFmpeg, file system
├── Cutube.Contracts/     # Keep: Messages, DTOs
├── Cutube.Api/          # Update: Remove CLI reference
└── Cutube.Worker/       # Keep: Background processing
```

### Phase 2: Clean CLI

```
cutube/
├── Program.cs            # Thin: command parsing, DI setup
├── Presentation/         # Console UI
├── DependencyInjection/  # Service registration
└── cutube.csproj        # References: Core, Application
```

### Phase 3: Add Tests

```
tests/
├── Cutube.Core.Tests/
├── Cutube.Application.Tests/
├── Cutube.Infrastructure.Tests/
├── Cutube.Domain.Tests/   # Existing
├── Cutube.Api.Tests/      # Existing
├── Cutube.Worker.Tests/   # Existing
└── Cutube.Integration.Tests/ # ✨ NEW
```

## File Organization Patterns

### .NET Projects

```
[ProjectName]/
├── [Feature]/            # Feature-based (preferred)
│   ├── Commands/
│   ├── Queries/
│   ├── Handlers/
│   └── Models/
│
├── [Layer]/             # Layer-based (current)
│   ├── Controllers/
│   ├── Services/
│   └── Repositories/
│
└── [Category]/          # Category-based (CLI)
    ├── Configuration/
    ├── Infrastructure/
    └── Services/
```

### Next.js

```
cutube-web/
├── app/                 # File-based routing
├── components/          # Reusable UI
│   ├── ui/             # Base components
│   ├── [feature]/      # Feature components
│   └── layout/        # Layout components
├── lib/                # Utilities
├── hooks/              # Custom hooks
├── types/              # TypeScript definitions
└── e2e/                # E2E tests
```

## Dependency Management

### .NET Dependencies

**Current Flow**:
```
Cutube.Api → cutube (CLI) → Domain classes
Cutube.Api → Cutube.Domain
Cutube.Api → Cutube.Contracts

Cutube.Worker → Cutube.Contracts
```

**Target Flow**:
```
Cutube.Api → Cutube.Application → Cutube.Core → Cutube.Domain
Cutube.Api → Cutube.Contracts

Cutube.Worker → Cutube.Application → Cutube.Core → Cutube.Domain
Cutube.Worker → Cutube.Infrastructure → Cutube.Domain
Cutube.Worker → Cutube.Contracts

cutube (CLI) → Cutube.Application → Cutube.Core → Cutube.Domain
```

### Frontend Dependencies

**Flow**:
```
cutube-web (Next.js)
    ↓ SignalR
Cutube.Api
    ↓ RabbitMQ
Cutube.Worker
```

## Build Artifacts

### Ignored in Git
- `bin/`, `obj/` (.NET build output)
- `node_modules/` (frontend dependencies)
- `.next/` (Next.js build output)
- `TestResults/` (test results)
- `logs/` (application logs)
- `downloads/` (user data)

### Deployment
- **.NET**: Publish to `bin/Release/net10.0/publish/`
- **Next.js**: Build to `.next/` and `standalone/`
- **Docker**: Multi-stage builds in `Dockerfile`
