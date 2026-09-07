# Cutube - Project Overview

## Vision

**Cutube** is a flexible, platform-agnostic video downloading and processing tool designed to simplify video editing workflows, particularly for podcast creators.

### Core Value Proposition

- **Simplicity**: Download videos in different formats and qualities with a single command
- **Flexibility**: Cut videos at desired time points with audio-only options
- **Automation**: Background processing with real-time progress updates
- **Platform Agnostic**: Support multiple video platforms (starting with YouTube)

## Goals

### Primary Goals

1. **Streamline Podcast Production**
   - Quickly download guest videos from YouTube/other platforms
   - Extract audio for podcast distribution
   - Cut specific segments for clips/social media

2. **Provide Multiple Interfaces**
   - **CLI**: Power users, automation, scripts
   - **Web**: Casual users, visual progress, mobile access
   - **API**: Third-party integrations

3. **Ensure Quality & Reliability**
   - Handle network failures gracefully
   - Support resume/retry workflows
   - Clear error messages and progress indication

### Secondary Goals

1. **Extensibility**
   - Easy to add new video platforms
   - Plugin architecture for processing stages
   - Custom FFmpeg command templates

2. **Performance**
   - Fast downloads with parallel processing
   - Efficient video cutting with FFmpeg
   - Minimal resource usage

3. **User Experience**
   - Intuitive interface (CLI or Web)
   - Real-time progress updates
   - Accessible design principles

## Success Metrics

### Technical Metrics
- **Test Coverage**: 80%+ unit, 70%+ integration
- **Response Time**: API < 200ms (p95)
- **Uptime**: 99.9% availability
- **Error Rate**: < 1% for successful requests

### User Metrics
- **Completion Rate**: > 95% of downloads complete
- **NPS Score**: Measure user satisfaction
- **Adoption**: Track active users
- **Feature Usage**: Most used formats, quality levels

## Scope

### In Scope

#### Current Features (v0.1.0)
- ✅ YouTube video download (via yt-dlp)
- ✅ Video cutting by time range
- ✅ Audio-only downloads
- ✅ Multiple format support (MP4, MP3, etc.)
- ✅ CLI interface
- ✅ Web interface (Next.js)
- ✅ Real-time progress (SignalR)
- ✅ Background processing (RabbitMQ)
- ✅ Flexible time input (1h30m, 90s, etc.)
- ✅ Custom filename
- ✅ Input validation
- ✅ CTRL+C cancellation

#### Planned Features (See ROADMAP.md)
- ⏳ Multi-platform support (Instagram, TikTok, etc.)
- ⏳ Batch downloads
- ⏳ Download history & search
- ⏳ User accounts & authentication
- ⏳ Cloud storage integration (S3)
- ⏳ Advanced FFmpeg options (codec, bitrate)
- ⏳ Playlist/channel downloads
- ⏳ Automatic subtitle download & embed
- ⏳ Video quality preview
- ⏳ Download scheduling

### Out of Scope

#### Explicitly Excluded
- ❌ Video hosting/streaming
- ❌ Video editing (beyond cutting)
- ❌ Social media posting
- ❌ Content recommendation
- ❌ User-generated content platform
- ❌ Monetization features
- ❌ DRM circumvention
- ❌ Platform-specific DRM protection bypass

#### Future Consideration
- 🚫 Mobile apps (use web PWA)
- 🚫 Desktop apps (use web or CLI)
- 🚫 Browser extensions (use API)

## Stakeholders

### Primary
- **Podcast Creators**: Target users
- **Content Creators**: YouTubers, influencers
- **Developers**: API consumers, contributors

### Secondary
- **Researchers**: Educational content download
- **Archivists**: Personal media archives
- **Automation**: CI/CD pipelines, scripts

## Constraints

### Technical
- **.NET 10.0**: Must support LTS until Nov 2028
- **Next.js 16**: Use latest, but consider stability
- **yt-dlp**: Must keep bundled version updated
- **FFmpeg**: System requirement (or bundle)

### Legal
- **ToS Compliance**: Respect platform terms of service
- **Copyright**: Users responsible for content rights
- **GDPR/Privacy**: If adding user accounts

### Operational
- **Self-Funded**: No budget for paid services (use free tiers)
- **Single Developer**: Must be maintainable by one person
- **Open Source**: Consider licensing (MIT?)

## Architecture Principles

### Core Principles

1. **Modularity**: Clear separation of concerns
2. **Testability**: Comprehensive test coverage
3. **Scalability**: Horizontal scaling for worker
4. **Maintainability**: Clean code, documented decisions
5. **User Experience**: Fast, responsive, accessible

### Architectural Decisions

#### Messaging Over Direct Calls
**Decision**: Use RabbitMQ for API → Worker communication

**Rationale**:
- Decouples API from processing
- Allows scaling workers independently
- Enables retry/dead letter queues
- Supports multiple worker types

**Trade-offs**:
- Increased complexity
- Additional infrastructure
- Eventual consistency

#### CLI and Web First
**Decision**: Maintain both CLI and Web interfaces

**Rationale**:
- CLI for power users, automation
- Web for casual users, mobile
- API shared between both

**Trade-offs**:
- More maintenance surface
- Potential feature parity issues

#### Domain-Driven Design Elements
**Decision**: Use DDD patterns for business logic

**Rationale**:
- Clear domain model
- Testable business rules
- Language ubiquitous

**Trade-offs**:
- Overhead for simple features
- Learning curve for maintainers

## Technology Choices

### Backend
- **.NET 10.0**: Modern C#, performance, LTS
- **ASP.NET Core**: Web API, SignalR
- **MassTransit**: Message bus abstraction
- **RabbitMQ**: Message broker
- **Serilog**: Structured logging
- **yt-dlp**: Video downloader
- **FFmpeg**: Video processor

### Frontend
- **Next.js 16**: React framework, App Router
- **shadcn/ui**: Component primitives
- **Tailwind CSS**: Styling
- **SignalR Client**: Real-time updates
- **React Query**: Server state

### Infrastructure
- **Docker**: Containerization
- **docker-compose**: Local development
- **Git**: Version control
- **GitHub**: CI/CD, hosting

## Non-Functional Requirements

### Performance
- **API**: < 200ms response (p95)
- **Download**: Max available bandwidth
- **Processing**: Real-time or faster (FFmpeg)
- **Web**: < 3s First Contentful Paint

### Scalability
- **Users**: 1000+ concurrent
- **Downloads**: 100+ simultaneous
- **Storage**: 1TB+ (user-provided)

### Reliability
- **Uptime**: 99.9% (8.76h downtime/year)
- **Data Loss**: Zero (no database yet)
- **Recovery**: Automatic from file system

### Security
- **Auth**: Not yet implemented (planned)
- **Encryption**: TLS for all network traffic
- **Input**: Sanitize all user input
- **Secrets**: Never commit to repository

### Maintainability
- **Tests**: 80%+ coverage
- **Documentation**: Code comments, README
- **Code Review**: All changes via PRs
- **CI/CD**: Automated tests & deployments

## Risk Management

### Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|---------|------------|
| yt-dlp breaks with platform updates | High | High | Auto-update, fallback to system binary |
| FFmpeg not available | Low | High | Bundle with app, clear error message |
| RabbitMQ downtime | Medium | High | Health checks, retries |
| File system fills up | Medium | Medium | Quotas, cleanup policies |

### Project Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|---------|------------|
| Platform ToS violation | Medium | High | Respect ToS, user responsibility |
| Copyright issues | Low | High | Disclaimer, user responsibility |
| Abandoned by maintainer | Low | High | Open source, community |
| Scope creep | High | Medium | Clear roadmap, feature prioritization |

## Dependencies

### External Dependencies
- **yt-dlp**: Maintained by community
- **FFmpeg**: Stable, mature
- **RabbitMQ**: Mature, widely used
- **.NET**: Microsoft LTS

### Internal Dependencies
- **Cutube.Domain**: Core models
- **Cutube.Contracts**: Shared messages
- **Cutube.Api**: HTTP/SignalR
- **Cutube.Worker**: Background processing

## Version History

### v0.1.0 (Current)
- Initial release
- CLI interface
- YouTube download
- Video cutting
- Audio extraction
- Basic web interface

### Roadmap
- See [ROADMAP.md](./ROADMAP.md)

## Communication Channels

### For Users
- **Issues**: GitHub Issues
- **Discussions**: GitHub Discussions
- **Documentation**: `/docs` folder

### For Developers
- **AGENTS.md**: Development workflow
- **CONVENTIONS.md**: Coding standards
- **ARCHITECTURE.md**: System design

## Related Projects

### Inspiration
- **youtube-dl**: Original Python tool
- **yt-dlp**: Active fork of youtube-dl
- **NewPipe**: Android YouTube client

### Competitors
- **4K Video Downloader**: Commercial
- **YTD**: Commercial
- **youtube-dl**: Command-line only

## Definitions

### Terms
- **Cutube**: The project name
- **yt-dlp**: Video downloader tool
- **FFmpeg**: Video processing tool
- **CLI**: Command-Line Interface
- **PWA**: Progressive Web App

### Acronyms
- **API**: Application Programming Interface
- **DDD**: Domain-Driven Design
- **E2E**: End-to-End
- **LTS**: Long-Term Support
- **PWA**: Progressive Web App
- **ToS**: Terms of Service

## References

### Documentation
- [.NET Documentation](https://learn.microsoft.com/dotnet/)
- [Next.js Documentation](https://nextjs.org/docs)
- [yt-dlp Documentation](https://github.com/yt-dlp/yt-dlp)
- [FFmpeg Documentation](https://ffmpeg.org/documentation.html)

### Standards
- [Semantic Versioning](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [OpenAPI Specification](https://swagger.io/specification/)
