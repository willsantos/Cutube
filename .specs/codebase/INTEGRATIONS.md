# Cutube Integrations

## External Services & APIs

### Video Services

#### YouTube (yt-dlp)

**Purpose**: Download videos from YouTube and other platforms.

**Integration**:
- **Library**: `YoutubeDLSharp` 1.2.0 (.NET wrapper)
- **Binary**: `yt-dlp` (bundled, auto-updating)
- **Location**: `~/.local/share/Cutube/yt-dlp`

**Implementation**:
- **CLI Project**: `YtDlpHelper` (static helper)
- **Domain Project**: `IYtDlpService` interface

**Features**:
- ✅ Video download
- ✅ Metadata extraction
- ✅ Format selection
- ✅ Quality options
- ✅ Subtitle download
- ❌ PO token support (mentioned in README)

**Configuration**:
```csharp
// In YtDlpHelper
private const string YtDlpFileName = "yt-dlp";
private static readonly string XdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
private static readonly string AppDataDir = Path.Combine(XdgDataHome, "Cutube");
private static readonly string YtDlpPath = Path.Combine(AppDataDir, YtDlpFileName);
```

**Error Handling**:
- Custom exception: `YtDlpException`
- Process exit code checking
- Stderr parsing for errors

**Issues**:
- ❌ Static helper in CLI (should be injected service)
- ❌ No retry logic for network failures
- ❌ No timeout configuration

**Alternatives**:
- **NewPipe**: For YouTube (Android extractor)
- **youtube-dl**: Legacy tool (yt-dlp is fork)
- **Invidious**: Frontend API (rate limits)

### Video Processing

#### FFmpeg

**Purpose**: Cut and process videos.

**Integration**:
- **Library**: `FFmpeg.AutoGen` 8.0.0 (bindings)
- **Binary**: System `ffmpeg` or bundled
- **Check**: Validates `ffmpeg --version` on startup

**Implementation**:
- **CLI Project**: `FfmpegHelper` (static helper)
- **Features**:
  - ✅ Video cutting by time range
  - ✅ Audio extraction
  - ✅ Format conversion
  - ❌ Quality adjustment
  - ❌ Codec options

**Configuration**:
```csharp
// In FfmpegHelper
private static readonly string FfmpegFileName = GetPlatformFileName();
private static readonly string FfprobeFileName = GetPlatformProbeFileName();
```

**Error Handling**:
- Custom exception: `FfmpegException`
- Process exit code checking
- Stderr parsing for errors

**Issues**:
- ❌ Static helper in CLI (should be injected service)
- ❌ No progress reporting during processing
- ❌ No hardware acceleration support

## Messaging Infrastructure

### RabbitMQ

**Purpose**: Message broker for async processing.

**Integration**:
- **Library**: `MassTransit` 8.3.x + `MassTransit.RabbitMQ`
- **Transport**: AMQP over TCP
- **Topology**: Direct exchange, queue per consumer

**Configuration**:
```json
// appsettings.json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "cutube",
    "Password": "cutube123"
  }
}
```

**Messages**:
- `DownloadMessage` (API → Worker)
- `ProcessingResult` (Worker → API → SignalR)

**Topology**:
```
┌─────────────┐
│    API      │
└──────┬──────┘
       │ Publish DownloadMessage
       ▼
┌─────────────────────┐
│ RabbitMQ (Exchange)  │
└──────┬──────────────┘
       │
       ▼
┌─────────────────────┐
│ cutube-queue       │
└──────┬────────────┘
       │ Consume
       ▼
┌─────────────┐
│   Worker    │
└────────────┘
```

**Local Development**:
- **docker-compose.yml**: RabbitMQ container
- **Management UI**: http://localhost:15672
- **Credentials**: cutube / cutube123

**Error Handling**:
- ❌ No dead letter queue
- ❌ No retry policy configuration visible
- ✅ Polly for HTTP resilience (not RabbitMQ)

**Alternatives**:
- **Azure Service Bus**: Cloud-based
- **AWS SQS**: Cloud-based
- **Redis Streams**: Lightweight
- **gRPC**: Direct streaming

## Real-time Communication

### SignalR

**Purpose**: Real-time updates to web clients.

**Integration**:
- **Server**: ASP.NET Core SignalR 10.0.0
- **Client**: `@microsoft/signalr` 10.0.0 (React)

**Features**:
- ✅ Progress updates
- ✅ Status notifications
- ✅ Error broadcasting
- ❌ Authentication/authorization

**Hub** (inferred):
```csharp
// In Cutube.Api/Hubs/
public class DownloadHub : Hub
{
    public async Task JoinDownloadGroup(string downloadId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"download-{downloadId}");
    }
}
```

**Client**:
```typescript
// In cutube-web
import { HubConnectionBuilder } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
  .withUrl('http://localhost:5000/hubs/download')
  .build();

connection.on('DownloadProgress', (progress) => {
  // Update UI
});
```

**Issues**:
- ❌ No authentication
- ❌ No reconnection strategy visible
- ❌ No connection lifecycle management

## File System

### Downloads Directory

**Purpose**: Store downloaded videos.

**Location**:
- **Default**: `./downloads/` (project root)
- **Configurable**: Via command-line flag

**Implementation**:
- **CLI**: Direct file operations
- **Worker**: File system operations in services

**Issues**:
- ❌ No cleanup of old downloads
- ❌ No storage quota management
- ❌ No duplicate handling
- ❌ No file organization (by date, user, etc.)

**Recommendations**:
- Add download history tracking
- Implement cleanup policies
- Add storage quota limits
- Organize by user/date

### Logs Directory

**Purpose**: Store application logs.

**Location**:
- **.NET**: `./logs/` (Serilog file sink)
- **Worker**: Structured log files

**Configuration**:
```csharp
// Serilog configuration
.WriteTo.File(
    Path.Combine("logs", "cutube-.log"),
    rollingInterval: RollingInterval.Day,
    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
)
```

**Issues**:
- ❌ No log rotation policy
- ❌ No log size limits
- ❌ No centralized logging

## Infrastructure as Code

### Docker

**Purpose**: Containerize services for development and deployment.

**Configuration Files**:
- **docker-compose.yml**: RabbitMQ, services
- **docker/**: Dockerfiles for services

**Services**:
```yaml
# docker-compose.yml (inferred)
services:
  rabbitmq:
    image: rabbitmq:management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: cutube
      RABBITMQ_DEFAULT_PASS: cutube123
```

**Issues**:
- ❌ No Dockerfile for API
- ❌ No Dockerfile for Worker
- ❌ No Dockerfile for Web (Next.js)

**Recommendations**:
- Add Dockerfiles for all services
- Use multi-stage builds
- Add health checks
- Use docker-compose for local dev

## Development Tools

### Version Management

#### asdf (.NET)

**Purpose**: Manage .NET runtime versions.

**Configuration**:
```
# .tool-versions
dotnet-core 10.0.0
```

**Usage**:
- Automatically activates when entering project directory
- Ensures consistent .NET version across team

#### pnpm (Frontend)

**Purpose**: Fast, disk-efficient package manager.

**Configuration**:
```json
{
  "packageManager": "pnpm@9.0.0"
}
```

**Benefits**:
- Faster installs than npm
- Strict dependency handling
- Monorepo support (pnpm-workspace.yaml)

### Git Hooks

**Purpose**: Automate checks before commits/pushes.

**Installed Hooks**:
- `.githooks/post-checkout`: Auto-import bd tasks
- `.githooks/pre-commit`: (check if exists)
- `.githooks/pre-push`: (check if exists)

**Installation**:
```bash
./install-hooks.sh
```

**Issues**:
- ❌ No pre-commit hooks for tests/linting
- ❌ No pre-push hooks for quality gates
- ❌ No commit-msg hook for conventional commits

## CI/CD

### GitHub Actions

**Purpose**: Automate builds, tests, and deployments.

**Workflows**: (check `.github/workflows/`)
- Likely:
  - `dotnet.yml` (build, test .NET)
  - `playwright.yml` (E2E tests)

**Issues**:
- ❌ No workflow files visible in root
- ❌ No deployment automation

**Recommendations**:
- Add CI workflow (build, test, lint)
- Add CD workflow (deploy to production)
- Add dependency scanning
- Add container scanning

## Missing Integrations

### Should Add

1. **Database**
   - **SQLite**: Embedded, easy
   - **PostgreSQL**: Production-ready
   - **Purpose**: Download history, user data

2. **Object Storage**
   - **S3**: Scalable storage
   - **Azure Blob**: Cloud-native
   - **Purpose**: Video storage instead of file system

3. **CDN**
   - **CloudFront**: AWS
   - **Azure CDN**: Microsoft
   - **Purpose**: Video delivery

4. **Monitoring**
   - **Application Insights**: Azure
   - **DataDog**: Cross-platform
   - **Prometheus**: Open-source

5. **Authentication**
   - **Auth0**: Easy integration
   - **IdentityServer**: Self-hosted
   - **Purpose**: User accounts

### Optional

1. **Analytics**
   - **Plausible**: Privacy-friendly
   - **Google Analytics**: Standard

2. **Error Tracking**
   - **Sentry**: Open-source
   - **Rollbar**: Easy setup

3. **Notifications**
   - **SendGrid**: Email
   - **Twilio**: SMS

## Integration Testing

### Current State
- ❌ No integration test project
- ❌ No testcontainers setup
- ❌ No integration tests for external services

### Recommendations

#### Testcontainers
```csharp
// Example: RabbitMQ container
var container = new RabbitMqBuilder()
  .WithUsername("cutube")
  .WithPassword("cutube123")
  .Build();

await container.StartAsync();
// Run tests
await container.DisposeAsync();
```

#### WireMock
```csharp
// Example: Mock YouTube API
var server = WireMockServer.Start();
server
  .Given(Request.Create()
    .WithPath("/watch*v=*"))
  .RespondWith(Response.Create()
    .WithBody(videoData));
```

## Security Considerations

### Current Issues
- ❌ No API authentication
- ❌ No CORS configuration visible
- ❌ No rate limiting
- ❌ No input sanitization
- ❌ Secrets in appsettings.json

### Recommendations

1. **Authentication**
   - Add API keys/tokens
   - Add user authentication (JWT)

2. **Authorization**
   - Role-based access control
   - Resource-based permissions

3. **Rate Limiting**
   - ASP.NET Core rate limiting middleware
   - Per-user limits

4. **Input Validation**
   - URL validation
   - File path sanitization
   - SQL injection prevention (when adding DB)

5. **Secrets Management**
   - User Secrets (dev)
   - Key Vault (prod)
   - Environment variables

## Integration Health Checks

### Current State
- ✅ `Cutube.Api/HealthChecks/` exists
- ❌ No health checks for RabbitMQ
- ❌ No health checks for yt-dlp
- ❌ No health checks for FFmpeg

### Recommendations

#### ASP.NET Core Health Checks
```csharp
// In Program.cs
builder.Services
    .AddHealthChecks()
    .AddRabbitMQ(rabbitConnectionString)
    .AddProcess("yt-dlp", "--version")
    .AddProcess("ffmpeg", "-version");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

## Documentation

### Current State
- ✅ README.md (basic)
- ✅ AGENTS.md (developer workflow)
- ✅ GITHOOKS.md (git hooks)
- ❌ No API documentation (Swagger exists, not documented)
- ❌ No architecture diagrams
- ❌ No integration documentation

### Recommendations

1. **API Documentation**
   - Document all endpoints
   - Add request/response examples
   - Document error codes

2. **Architecture Diagrams**
   - C4 model diagrams
   - Sequence diagrams for flows
   - Deployment diagrams

3. **Integration Guides**
   - How to add new video platforms
   - How to configure RabbitMQ
   - How to deploy to production

4. **Troubleshooting**
   - Common issues
   - Debug steps
   - Log analysis
