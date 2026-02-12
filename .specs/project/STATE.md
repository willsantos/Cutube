# Cutube - Project State

**Last Updated**: 2026-02-12
**Session**: Initial brownfield analysis

## Current Status

### Version
- **Current Version**: v0.1.0
- **Next Version**: v0.2.0 (Planned)

### Development Phase
- **Phase**: Brownfield → Refactoring
- **Status**: Analysis complete, refactoring pending
- **Branch**: `feature/restructure` (to be created)

### Health Metrics
- **Build Status**: ✅ Passing
- **Test Status**: ✅ Passing (coverage unknown)
- **Coverage**: ❓ Not measured
- **Documentation**: ⚠️ Partial

## Completed Work

### v0.1.0 Features

#### Backend (src/)
- ✅ Modular .NET architecture
- ✅ API with SignalR for real-time updates
- ✅ Worker for background processing
- ✅ Domain layer with models and services
- ✅ Contracts for message passing
- ✅ RabbitMQ integration via MassTransit
- ✅ Serilog for structured logging

#### Frontend (cutube-web/)
- ✅ Next.js 16 with App Router
- ✅ shadcn/ui component library
- ✅ Tailwind CSS v4
- ✅ SignalR client integration
- ✅ React Query for server state
- ✅ Playwright E2E tests
- ✅ TypeScript strict mode

#### CLI (cutube/)
- ✅ Interactive menu
- ✅ Command-line parsing
- ✅ yt-dlp integration
- ✅ FFmpeg integration
- ✅ Time parsing (flexible input)
- ✅ Input validation
- ✅ Progress bars
- ✅ Error handling

## In Progress

### Current Session Tasks

#### Completed
- ✅ Brownfield analysis (STACK.md, ARCHITECTURE.md, CONVENTIONS.md, STRUCTURE.md, TESTING.md, INTEGRATIONS.md)
- ✅ Project initialization (PROJECT.md, ROADMAP.md)

#### Pending
- ⏳ Create refactoring plan
- ⏳ Prioritize technical debt
- ⏳ Define quality gates

## Blockers

### Technical
- **API references CLI**: Cutube.Api → cutube project reference
  - **Impact**: Tight coupling, unclear architecture
  - **Solution**: Extract shared logic to Cutube.Core
  - **Effort**: Medium
  - **Priority**: High

### Process
- **No code coverage**: Coverage not configured
  - **Impact**: Unknown test quality
  - **Solution**: Add Coverlet
  - **Effort**: Low
  - **Priority**: Medium

## Decisions Made

### Architectural
1. **Messaging**: Use RabbitMQ for async processing
   - **Date**: Initial design
   - **Rationale**: Decouples API from worker, scales independently
   - **Status**: Implemented ✅

2. **Dual Interface**: Maintain both CLI and Web
   - **Date**: Initial design
   - **Rationale**: Different user needs
   - **Status**: Implemented ✅

3. **DDD Elements**: Use domain-driven design patterns
   - **Date**: Initial design
   - **Rationale**: Clear business logic
   - **Status**: Partially implemented ⚠️

### Technology
1. **.NET 10.0**: Use latest LTS
   - **Date**: Feb 2026
   - **Rationale**: Long-term support until Nov 2028
   - **Status**: Implemented ✅

2. **Next.js 16**: Use latest with App Router
   - **Date**: Feb 2026
   - **Rationale**: Modern React patterns
   - **Status**: Implemented ✅

3. **Serilog**: Use for logging
   - **Date**: Initial design
   - **Rationale**: Structured logging
   - **Status**: Implemented ✅

## Technical Debt

### High Priority
1. **Remove CLI reference from API**
   - **Location**: `src/Cutube.Api/Cutube.Api.csproj`
   - **Impact**: Architecture clarity
   - **Effort**: Medium

2. **Add test coverage**
   - **Tool**: Coverlet
   - **Target**: 80%+ unit, 70%+ integration
   - **Effort**: Low

3. **Add integration tests**
   - **Scope**: API endpoints, Worker processing
   - **Tools**: WebApplicationFactory, Testcontainers
   - **Effort**: Medium

### Medium Priority
1. **Standardize DI patterns**
   - **Issue**: Mixed static/injected services
   - **Solution**: All services via DI
   - **Effort**: Medium

2. **Add health checks**
   - **Scope**: RabbitMQ, yt-dlp, FFmpeg
   - **Tool**: ASP.NET Core Health Checks
   - **Effort**: Low

3. **Improve error handling**
   - **Issue**: No global error handler
   - **Solution**: Exception handling middleware
   - **Effort**: Low

### Low Priority
1. **Add distributed tracing**
   - **Tool**: OpenTelemetry
   - **Effort**: High
   - **Value**: Debugging production issues

2. **Add performance tests**
   - **Tool**: BenchmarkDotNet, K6
   - **Effort**: Medium
   - **Value**: Performance regression detection

## Known Issues

### Bugs
- **None reported**: Software appears stable

### Feature Gaps
- **No database**: Download history not tracked
- **No authentication**: No user accounts
- **Limited platforms**: YouTube only (technically supports more via yt-dlp)

### Usability
- **Error messages**: Could be more user-friendly
- **Documentation**: Needs improvement
- **Onboarding**: No developer getting started guide

## Next Steps

### Immediate (This Session)
1. **Create refactoring plan**
   - Define `Cutube.Core` structure
   - Plan migration from CLI
   - Estimate effort

2. **Prioritize technical debt**
   - Rank by impact/effort
   - Create tasks in bd
   - Schedule sprints

### Short-Term (Next Session)
1. **Implement Cutube.Core**
   - Extract shared logic
   - Add interfaces
   - Update references

2. **Add test coverage**
   - Configure Coverlet
   - Set minimum thresholds
   - Add to CI

3. **Add integration tests**
   - API endpoint tests
   - Worker tests with Testcontainers
   - E2E flow tests

### Medium-Term (Next Month)
1. **Complete v0.2.0 features**
   - Multi-platform support
   - Error handling polish
   - Web UI improvements

2. **Database planning**
   - Choose: SQLite vs PostgreSQL
   - Design schema
   - Plan migrations

## Preferences

### Development
- **Model Preference**: Use faster/cheaper models for lightweight tasks (validation, state updates, session handoff)
- **Code Review**: All changes via PRs
- **Testing**: Test-first for new features

### Communication
- **Style**: Conversational, not robotic
- **Tone**: Professional but approachable
- **Format**: Markdown documentation

### Tools
- **Linting**: Strict, zero warnings
- **Formatting**: Prettier (auto-format)
- **Git**: Conventional commits required

## Environment

### Development
- **OS**: Linux (primary), macOS (secondary)
- **Runtime**: .NET 10.0, Node.js (LTS)
- **IDE**: VS Code, Rider

### Local Services
- **RabbitMQ**: docker-compose
- **yt-dlp**: Bundled with app
- **FFmpeg**: System binary

### CI/CD
- **Platform**: GitHub Actions
- **Status**: Not configured yet
- **Plan**: Add in v0.2.0

## Resources

### Documentation
- **AGENTS.md**: Development workflow
- **README.md**: Project overview
- **docs/**: Additional documentation

### Tools
- **bd (Beads)**: Task tracking
- **GitHub**: Issue tracking, releases
- **Linear**: User-reported issues (Oroborus workspace)

### External
- [.NET 10.0 Docs](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10)
- [Next.js 16 Docs](https://nextjs.org/docs)
- [yt-dlp Docs](https://github.com/yt-dlp/yt-dlp)

## Session History

### 2026-02-12
- **Activity**: Initial brownfield analysis
- **Outputs**:
  - STACK.md
  - ARCHITECTURE.md
  - CONVENTIONS.md
  - STRUCTURE.md
  - TESTING.md
  - INTEGRATIONS.md
  - PROJECT.md
  - ROADMAP.md
- **Next**: Refactoring plan

## Notes

### Architecture Observations
1. Code evolved organically without clear planning
2. Multiple interfaces (CLI, Web, API) exist
3. Business logic scattered across projects
4. API directly references CLI (tight coupling)

### Refactoring Strategy
1. **Phase 1**: Extract `Cutube.Core` with shared logic
2. **Phase 2**: Move CLI to thin presentation layer
3. **Phase 3**: Standardize DI patterns
4. **Phase 4**: Add integration tests
5. **Phase 5**: Add database (v0.3.0)

### Quality Improvements
1. Add test coverage measurement
2. Add integration tests
3. Add health checks
4. Improve error handling
5. Add distributed tracing

### Technical Debt
1. Remove CLI reference from API
2. Standardize DI patterns
3. Add integration tests
4. Add health checks
5. Improve documentation

## Actions

### Completed This Session
- [x] Analyze codebase structure
- [x] Document technology stack
- [x] Document architecture patterns
- [x] Document code conventions
- [x] Document project structure
- [x] Document testing strategy
- [x] Document integrations
- [x] Create project overview
- [x] Create roadmap

### Next Session
- [ ] Create refactoring plan
- [ ] Create tasks for technical debt
- [ ] Prioritize by impact/effort
- [ ] Implement Cutube.Core
- [ ] Add test coverage
- [ ] Add integration tests

### Blockers to Resolve
- [ ] Remove API → CLI reference
- [ ] Add test coverage tooling
- [ ] Add CI/CD pipeline

## Decisions Requiring Documentation

### ADRs Needed
1. **Messaging Architecture**: Why RabbitMQ?
2. **Dual Interface**: Why CLI and Web?
3. **Technology Stack**: Why .NET 10.0, Next.js 16?
4. **Database Choice**: SQLite vs PostgreSQL? (when chosen)

### ADRs to Create
- [ ] ADR-001: RabbitMQ for Async Processing
- [ ] ADR-002: Dual Interface Strategy
- [ ] ADR-003: Technology Stack Selection
- [ ] ADR-004: Database Architecture (pending)

## Glossary

### Terms
- **Brownfield**: Existing codebase with technical debt
- **Greenfield**: New codebase without constraints
- **DDD**: Domain-Driven Design
- **CQRS**: Command Query Responsibility Segregation
- **SOLID**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion

### Acronyms
- **ADR**: Architecture Decision Record
- **API**: Application Programming Interface
- **CI/CD**: Continuous Integration/Continuous Deployment
- **DI**: Dependency Injection
- **E2E**: End-to-End
- **LTS**: Long-Term Support
- **SOLID**: See above

## References

### Internal
- [AGENTS.md](../AGENTS.md) - Development workflow
- [README.md](../../README.md) - Project overview
- [.specs/codebase/](./codebase/) - Brownfield analysis

### External
- [Semantic Versioning](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [.NET Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Next.js Best Practices](https://nextjs.org/docs)

## Questions

### For Future Sessions
1. Should v0.3.0 use SQLite or PostgreSQL?
2. Should we support video hosting or only processing?
3. Should we add user authentication before or after database?
4. Should we use OpenTelemetry for distributed tracing?

### For Users
1. What platforms are most important after YouTube?
2. Is batch download a high-priority feature?
3. Is cloud storage important or is local storage sufficient?

## Contact

### Maintainer
- **Name**: [User]
- **Role**: Lead Developer
- **Location**: [Timezone]

### Communication
- **GitHub**: [@will](https://github.com/will)
- **Email**: [Private]

---

**Document Version**: 1.0
**Last Modified**: 2026-02-12
**Next Review**: 2026-03-12
