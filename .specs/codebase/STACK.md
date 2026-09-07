# Cutube Technology Stack

## Backend (.NET)

### Core Framework
- **.NET 10.0** (LTS until Nov 2028)
- **C# 13** with nullable reference types enabled
- **ImplicitUsings** enabled across all projects

### Architecture Pattern
- **Modular monolith** with messaging
- **Worker service** pattern for background processing
- **Domain-Driven Design** (DDD) elements

### Key Libraries

#### Video Processing
- **yt-dlp** (bundled, auto-updating)
- **YoutubeDLSharp** 1.2.0 (.NET wrapper)
- **FFmpeg.AutoGen** 8.0.0

#### Messaging
- **MassTransit** 8.3.x (message bus abstraction)
- **MassTransit.RabbitMQ** 8.3.x (RabbitMQ transport)
- **RabbitMQ** (message broker)

#### API
- **ASP.NET Core** 10.0.2 (Web API)
- **SignalR** 10.0.0 (real-time communication)
- **Swashbuckle** 10.1.2 (OpenAPI/Swagger)

#### Logging & Monitoring
- **Serilog** 9.0.0 (structured logging)
- **Serilog.Extensions.Hosting** 9.0.0
- **Serilog.Settings.Configuration** 9.0.0
- **Serilog.Formatting.Compact** 3.0.0
- **Serilog.Sinks.Console** 6.0.0
- **Serilog.Sinks.File** 7.0.0

#### Resilience
- **Microsoft.Extensions.Http.Polly** 10.0.3

#### Validation
- **FluentResults** 4.0.0 (result pattern)

### Hosting
- **Microsoft.Extensions.Hosting** 10.0.2 (generic host)

## Frontend (Next.js)

### Core Framework
- **Next.js 16.1.6** (App Router)
- **React 19.2.3**
- **TypeScript 5**

### UI & Styling
- **Tailwind CSS 4** (CSS-first)
- **shadcn/ui** 3.8.4 (component primitives)
- **Radix UI** 1.4.3 (headless components)
- **Lucide React** 0.563.0 (icons)
- **next-themes** 0.4.6 (dark mode)

### Forms & Validation
- **React Hook Form** 7.71.1
- **@hookform/resolvers** 5.2.2
- **Zod** 4.3.6 (schema validation)

### Data Fetching
- **@tanstack/react-query** 5.90.20 (server state)

### Real-time
- **@microsoft/signalr** 10.0.0 (SignalR client)

### Utilities
- **class-variance-authority** 0.7.1 (className management)
- **clsx** 2.1.1 (conditional classes)
- **tailwind-merge** 3.4.0
- **date-fns** 4.1.0 (date utilities)
- **sonner** 2.0.7 (toasts)

### Testing
- **Playwright** 1.58.2 (E2E tests)
- **@axe-core/playwright** 4.11.1 (accessibility)

### Quality
- **ESLint 9** (linting)
- **Prettier 3.8.1** (formatting)

## Infrastructure

### Containerization
- **Docker** (container runtime)
- **docker-compose** (local development)

### Package Managers
- **asdf** (.NET version management)
- **pnpm** (frontend dependencies)

## Development Tools

### Version Control
- **Git** with conventional commits
- **GitHub Actions** (CI/CD)

### Task Tracking
- **bd (Beads)** - Development tasks
- **Linear** - User-reported issues

### Code Quality
- **Dotnet test** (unit tests)
- **Playwright** (E2E tests)
- **ESLint** (linting)
- **TypeScript** (type checking)

## External Dependencies

### Video Services
- **YouTube** (primary platform)
- **yt-dlp** (downloader engine)

### Message Broker
- **RabbitMQ** (async processing)

## Technology Debt & Risks

### Current Issues
1. **Dual frontend apps**: Both `cutube/` (CLI) and `cutube-web/` (Next.js) exist
2. **Architecture drift**: Code evolved without clear architecture planning
3. **Mixed concerns**: Some business logic in CLI, some in Domain

### Version Considerations
- **.NET 10.0**: Very new (released Feb 2025), consider stability
- **Next.js 16**: Latest, may have breaking changes
- **React 19**: Latest, may have ecosystem compatibility issues

## Technology Decisions Needed

1. **CLI vs Web**: Should both be maintained long-term?
2. **API-first**: Should CLI use same API as web?
3. **Deployment**: Container strategy for production
4. **State management**: SignalR vs polling for real-time updates
