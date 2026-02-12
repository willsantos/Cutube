# Cutube - Monorepo Management with Turborepo

## Current Pain Points

### Development Workflow Issues

1. **No unified commands**:
   - CLI: `cd cutube && dotnet run`
   - API: `cd src/Cutube.Api && dotnet run`
   - Worker: `cd src/Cutube.Worker && dotnet run`
   - Web: `cd cutube-web && pnpm dev`

2. **No dependency awareness**:
   - API depends on Domain (but no enforcement)
   - Web depends on API (but manual startup)
   - Worker depends on Contracts (but no order)

3. **No caching**:
   - `dotnet build` every time (even if nothing changed)
   - `next build` every time
   - Tests run from scratch

4. **Docker-compose incomplete**:
   - Only RabbitMQ in compose
   - API, Worker, Web run separately

## Recommended Solution

### ✅ **Turborepo + pnpm workspaces** (RECOMMENDED)

**This is the RECOMMENDED approach for monorepo management.**

#### Why Turborepo?

1. **Intelligent Caching**
   - Skip unchanged packages
   - Reuse build artifacts
   - Faster builds (30s → 1s)

2. **Dependency Awareness**
   - Automatic build ordering
   - Wait for dependencies
   - Fail fast on errors

3. **Powerful Filtering**
   - `pnpm dev --filter=Cutube.Api`
   - `pnpm build --filter=...Cutube.Web`
   - `pnpm test --filter=[HEAD^1]`

4. **Parallel Execution**
   - Run tasks in parallel where possible
   - Faster CI/CD
   - Better developer experience

#### Why pnpm workspaces?

1. **Already using** - `pnpm-workspace.yaml` exists
2. **Efficient** - Hard links, fast installs
3. **Strict** - Prevents phantom dependencies
4. **Standard** - Community best practice

### Alternatives Considered (Not Recommended)

#### Concurrently
- ✅ Simpler setup
- ❌ No caching
- ❌ Manual dependency ordering
- ❌ No filtering

#### Custom Scripts
- ✅ Full control
- ❌ High maintenance
- ❌ Reinventing the wheel

## Architecture

### Workspace Structure

```
Cutube/                          # Root workspace
├── package.json                  # Root package.json (NEW)
├── pnpm-workspace.yaml           # Workspace config (MOVED from cutube-web/)
├── turbo.json                    # Turborepo config (NEW)
│
├── src/                          # .NET packages
│   ├── Cutube.Api/
│   │   └── package.json          # pnpm package (NEW)
│   ├── Cutube.Worker/
│   │   └── package.json          # pnpm package (NEW)
│   ├── Cutube.Domain/
│   │   └── package.json          # pnpm package (NEW)
│   ├── Cutube.Contracts/
│   │   └── package.json          # pnpm package (NEW)
│   ├── Cutube.Cli/
│   │   └── package.json          # pnpm package (NEW)
│   └── Cutube.Web/
│       └── package.json          # Already exists
│
├── tests/                        # Test packages
│   ├── Cutube.Api.Tests/
│   │   └── package.json          # pnpm package (NEW)
│   ├── Cutube.Worker.Tests/
│   │   └── package.json          # pnpm package (NEW)
│   ├── Cutube.Domain.Tests/
│   │   └── package.json          # pnpm package (NEW)
│   ├── Cutube.Cli.Tests/
│   │   └── package.json          # pnpm package (NEW)
│   └── Cutube.Web.E2E/
│       └── package.json          # Already exists
│
├── docker/                       # Docker configs
│   ├── docker-compose.yml         # All services (UPDATED)
│   └── .env                    # Shared env vars
│
└── scripts/                     # Utility scripts
    ├── dev.sh                    # Start all services
    ├── build.sh                  # Build all
    └── test.sh                   # Test all
```

## Recommended Configuration

### Root package.json
**Note**: This is the RECOMMENDED structure, not yet implemented.

### Root package.json

```json
{
  "name": "cutube-monorepo",
  "version": "0.1.0",
  "private": true,
  "description": "Cutube - Video download & processing tool",
  "scripts": {
    "dev": "turbo run dev",
    "dev:api": "turbo run dev --filter=Cutube.Api",
    "dev:worker": "turbo run dev --filter=Cutube.Worker",
    "dev:web": "turbo run dev --filter=Cutube.Web",
    "dev:cli": "turbo run dev --filter=Cutube.Cli",

    "build": "turbo run build",
    "build:api": "turbo run build --filter=Cutube.Api",
    "build:worker": "turbo run build --filter=Cutube.Worker",
    "build:web": "turbo run build --filter=Cutube.Web",

    "test": "turbo run test",
    "test:api": "turbo run test --filter=Cutube.Api.Tests",
    "test:worker": "turbo run test --filter=Cutube.Worker.Tests",
    "test:web": "turbo run test --filter=Cutube.Web.E2E",

    "lint": "turbo run lint",
    "format": "turbo run format",
    "typecheck": "turbo run typecheck",

    "clean": "turbo run clean",
    "docker:up": "docker-compose -f docker/docker-compose.yml up -d",
    "docker:down": "docker-compose -f docker/docker-compose.yml down"
  },
  "devDependencies": {
    "turbo": "^2.3.0",
    "concurrently": "^9.1.0",
    "cross-env": "^7.0.3"
  },
  "engines": {
    "node": ">=18",
    "pnpm": ">=9"
  },
  "packageManager": "pnpm@9.0.0"
}
```

### pnpm-workspace.yaml

```yaml
packages:
  - 'src/*'
  - 'tests/*'
  - 'scripts'

ignoreBuiltDependencies:
  - sharp
  - unrs-resolver
```

### turbo.json

```json
{
  "$schema": "https://turbo.build/schema.json",
  "globalDependencies": [
    "**/.env.*local",
    "**/.env.*.local",
    "**/docker-compose.yml"
  ],
  "pipeline": {
    "dev": {
      "dependsOn": ["^build"],
      "cache": false,
      "persistent": true
    },
    "build": {
      "dependsOn": ["^build"],
      "outputs": ["bin/**", "obj/**", ".next/**", "dist/**"]
    },
    "test": {
      "dependsOn": ["build"],
      "outputs": ["TestResults/**", "coverage/**"]
    },
    "lint": {
      "outputs": []
    },
    "format": {
      "outputs": []
    },
    "typecheck": {
      "dependsOn": ["^build"],
      "outputs": []
    },
    "clean": {
      "cache": false
    }
  }
}
```

## Package Configuration

### .NET Projects (src/Cutube.Api/package.json)

```json
{
  "name": "Cutube.Api",
  "version": "0.1.0",
  "private": true,
  "scripts": {
    "dev": "dotnet watch --project Cutube.Api.csproj",
    "build": "dotnet build Cutube.Api.csproj",
    "test": "dotnet test ../tests/Cutube.Api.Tests/Cutube.Api.Tests.csproj",
    "clean": "dotnet clean"
  }
}
```

### Web (src/Cutube.Web/package.json)

```json
{
  "name": "Cutube.Web",
  "version": "0.1.0",
  "private": true,
  "scripts": {
    "dev": "next dev --port 4000",
    "build": "next build",
    "start": "next start --port 4000",
    "test": "playwright test",
    "lint": "eslint . --max-warnings=0",
    "format": "prettier --write .",
    "typecheck": "tsc --noEmit"
  },
  "dependencies": { /* existing */ },
  "devDependencies": { /* existing */ }
}
```

## Docker Compose (Complete)

### docker/docker-compose.yml

```yaml
version: '3.8'

services:
  # Infrastructure
  rabbitmq:
    image: rabbitmq:3-management
    container_name: cutube-rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: cutube
      RABBITMQ_DEFAULT_PASS: cutube123
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  # Backend Services
  api:
    build:
      context: ..
      dockerfile: docker/Cutube.Api.Dockerfile
    container_name: cutube-api
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - RabbitMQ__Host=rabbitmq
      - RabbitMQ__Port=5672
      - RabbitMQ__Username=cutube
      - RabbitMQ__Password=cutube123
    depends_on:
      rabbitmq:
        condition: service_healthy
    volumes:
      - ../downloads:/downloads

  worker:
    build:
      context: ..
      dockerfile: docker/Cutube.Worker.Dockerfile
    container_name: cutube-worker
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - RabbitMQ__Host=rabbitmq
      - RabbitMQ__Port=5672
      - RabbitMQ__Username=cutube
      - RabbitMQ__Password=cutube123
    depends_on:
      rabbitmq:
        condition: service_healthy
    volumes:
      - ../downloads:/downloads

  # Frontend
  web:
    build:
      context: ../src/Cutube.Web
      dockerfile: ../../docker/Dockerfile.web
    container_name: cutube-web
    ports:
      - "3000:3000"
    environment:
      - NEXT_PUBLIC_API_URL=http://api:5000
      - NEXT_PUBLIC_SIGNALR_URL=http://api:5000/hubs
    depends_on:
      - api

volumes:
  rabbitmq-data:

networks:
  default:
    name: cutube-network
```

## Development Scripts

### scripts/dev.sh

```bash
#!/bin/bash
set -e

echo "🚀 Starting Cutube development environment..."

# Start infrastructure
echo "📡 Starting RabbitMQ..."
pnpm docker:up

# Wait for RabbitMQ
echo "⏳ Waiting for RabbitMQ..."
sleep 5

# Start all services in parallel
echo "🎯 Starting all services..."
pnpm dev
```

### scripts/build.sh

```bash
#!/bin/bash
set -e

echo "🔨 Building Cutube..."

# Build in dependency order
pnpm build

echo "✅ Build complete!"
```

### scripts/test.sh

```bash
#!/bin/bash
set -e

echo "🧪 Testing Cutube..."

# Run all tests
pnpm test

echo "✅ Tests passed!"
```

## Usage Examples

### Start Everything

```bash
# One command
pnpm dev

# Or with Docker
pnpm docker:up  # Start RabbitMQ
pnpm dev       # Start services
```

### Start Individual Services

```bash
# API only
pnpm dev:api

# Web only
pnpm dev:web

# Worker only
pnpm dev:worker
```

### Build Everything

```bash
# Build all (with cache!)
pnpm build

# Build specific
pnpm build:web
```

### Test Everything

```bash
# Run all tests
pnpm test

# Test specific
pnpm test:api
```

## Benefits

### 1. Single Command Development

**Before**:
```bash
# Terminal 1
docker-compose -f docker/docker-compose.yml up -d

# Terminal 2
cd src/Cutube.Api && dotnet run

# Terminal 3
cd src/Cutube.Worker && dotnet run

# Terminal 4
cd cutube-web && pnpm dev
```

**After**:
```bash
# One terminal!
pnpm dev
```

### 2. Cached Builds

**Before**:
```bash
# Every time: 30s+
dotnet build
```

**After**:
```bash
# First time: 30s
pnpm build

# Next time: 1s (from cache!)
pnpm build
```

### 3. Dependency Awareness

**Before**:
- API starts before Domain builds (fails!)
- Web starts before API (fails!)

**After**:
- Turborepo detects dependencies
- Starts in correct order automatically
- Waits for dependencies

### 4. Docker Compose Complete

**Before**:
- Only RabbitMQ in compose
- API, Worker, Web manual

**After**:
- Everything in compose
- One command: `docker-compose up`

### 5. Parallel Execution

**Before**:
```bash
# Sequential (slow)
dotnet test
dotnet test ../Cutube.Api.Tests
dotnet test ../Cutube.Worker.Tests
```

**After**:
```bash
# Parallel (fast!)
pnpm test
```

## Turborepo Features

### 1. Caching

```
┌─────────────────────────────────────┐
│  Turborepo Cache                 │
│  - .turbo/cache/                │
│  - Input hashing                 │
│  - Output reuse                 │
└─────────────────────────────────────┘
```

**Benefits**:
- Skip unchanged packages
- Reuse build artifacts
- Faster CI/CD

### 2. Orquestração

```
Dependencies: Cutube.Domain → Cutube.Api

build order:
1. Cutube.Domain    (no deps)
2. Cutube.Api       (waits for Domain)
3. Cutube.Web        (waits for API)
```

**Benefits**:
- Correct build order
- Parallel where possible
- Fail fast on errors

### 3. Filtering

```bash
# Build API and dependencies
pnpm build --filter=Cutube.Api

# Test API and Web only
pnpm test --filter=Cutube.Api... --filter=Cutube.Web...

# Lint changed packages
pnpm lint --filter=[HEAD^1]
```

### 4. Remote Caching (Optional)

```bash
# Setup remote cache
pnpm add -D turbo @vercel/ncc

# Use Vercel remote cache
turbo link
```

**Benefits**:
- Share cache across team
- Faster CI (no rebuilds)
- Share with CI/CD

## Alternative Approaches (Not Recommended)

### Concurrently
If Turborepo setup is too complex, Concurrently is a simpler alternative:

#### package.json (concurrently version)

### package.json (concurrently version)

```json
{
  "scripts": {
    "dev": "concurrently \"pnpm dev:*\"",
    "dev:api": "cd src/Cutube.Api && dotnet watch",
    "dev:worker": "cd src/Cutube.Worker && dotnet watch",
    "dev:web": "cd src/Cutube.Web && pnpm dev",

    "build": "pnpm build:api && pnpm build:worker && pnpm build:web",
    "build:api": "cd src/Cutube.Api && dotnet build",
    "build:worker": "cd src/Cutube.Worker && dotnet build",
    "build:web": "cd src/Cutube.Web && pnpm build"
  }
}
```

**Trade-offs**:
- ✅ Simpler
- ❌ No cache
- ❌ Manual dependency order
- ❌ No filtering

## CI/CD Integration

### GitHub Actions

```yaml
name: Build & Test

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup pnpm
        uses: pnpm/action-setup@v2
        with:
          version: 9

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          cache: 'pnpm'

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install dependencies
        run: pnpm install

      - name: Build & Test
        run: pnpm build && pnpm test

      - name: Build Docker images
        run: docker-compose -f docker/docker-compose.yml build
```

## Migration Steps

### Phase 1: Root Setup

1. **Create root package.json**
   - Move to root
   - Add turbo, concurrently
   - Add unified scripts

2. **Move pnpm-workspace.yaml**
   - From cutube-web/ to root
   - Update paths: `'src/*', 'tests/*'`

3. **Create turbo.json**
   - Configure pipelines
   - Add cache settings

### Phase 2: Package Scripts

1. **Add package.json** to each .NET project
   - src/Cutube.Api/package.json
   - src/Cutube.Worker/package.json
   - src/Cutube.Domain/package.json
   - etc.

2. **Update cutube-web/package.json**
   - Already exists
   - Just verify scripts

### Phase 3: Docker Compose

1. **Move docker-compose**
   - From root to docker/docker-compose.yml
   - Add API, Worker, Web services

2. **Test**
   - `pnpm docker:up`
   - `docker ps`
   - Verify all services running

### Phase 4: Development Scripts

1. **Create scripts/**
   - dev.sh
   - build.sh
   - test.sh

2. **Make executable**
   - `chmod +x scripts/*.sh`

### Phase 5: Test & Verify

1. **Test dev mode**
   - `pnpm dev`
   - Verify all services start
   - Verify hot reload

2. **Test build**
   - `pnpm build`
   - Verify cache works
   - Verify build order

3. **Test tests**
   - `pnpm test`
   - Verify parallel execution
   - Verify all pass

## Next Steps

### Immediate

1. **Decision**: Use Turborepo or Concurrently?
2. **Setup**: Create root package.json
3. **Configure**: Move pnpm-workspace.yaml

### Short-term

1. **Add package.json** to all projects
2. **Update docker-compose** (all services)
3. **Create dev scripts**
4. **Test thoroughly**

### Long-term

1. **Remote cache**: Setup Vercel/CI cache
2. **CI/CD**: Update workflows
3. **Monitoring**: Add health checks

## References

### Turborepo
- [Turborepo Docs](https://turbo.build/repo/docs)
- [Why Turborepo](https://turbo.build/repo/docs/core-concepts/monorepos/why-monorepos)

### pnpm
- [pnpm Workspaces](https://pnpm.io/workspaces)
- [pnpm Filters](https://pnpm.io/filtering)

### Docker Compose
- [Docker Compose Docs](https://docs.docker.com/compose/)
- [Multi-stage Builds](https://docs.docker.com/develop/develop-images/multistage-build/)
