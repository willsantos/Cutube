# Cutube - Feature Roadmap

## Version Strategy

**Semantic Versioning**: `[major].[minor].[patch]`
- **Major**: Breaking changes, architecture shifts
- **Minor**: New features, backward compatible
- **Patch**: Bug fixes, small improvements

## Roadmap Overview

```
┌─────────────────────────────────────────────────────────┐
│                 v0.1.0          v0.2.0    v0.3.0   │
│  Current ───────────────► Planned ──► Planned       │
│                                                      │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐        │
│  │  CLI     │   │  Web UI  │   │ Database │        │
│  │  Basic   │   │  Polish  │   │          │        │
│  │  YouTube │   │  Multi-  │   │ History  │        │
│  │  only    │   │  platform│   │ Search   │        │
│  └──────────┘   └──────────┘   └──────────┘        │
│                                                      │
└─────────────────────────────────────────────────────────┘
```

## v0.1.0 (Current)

### Status
- ✅ **Released**: Initial version
- ✅ **Stable**: Core functionality works

### Features
- ✅ CLI interface
- ✅ YouTube download (yt-dlp)
- ✅ Video cutting (FFmpeg)
- ✅ Audio extraction
- ✅ Basic web interface
- ✅ Real-time progress (SignalR)
- ✅ Background processing (RabbitMQ)
- ✅ Flexible time input
- ✅ Input validation
- ✅ CTRL+C cancellation

### Limitations
- ❌ YouTube only
- ❌ No download history
- ❌ No authentication
- ❌ No batch downloads
- ❌ No cloud storage

## v0.2.0 - Polish & Multi-Platform

### Timeline
- **Target**: Q2 2026
- **Priority**: High

### Features

#### Critical
- [ ] **Multi-Platform Support**
  - [ ] Instagram
  - [ ] TikTok
  - [ ] Vimeo
  - [ ] Twitter/X
  - [ ] Generic URL support

- [ ] **Error Handling**
  - [ ] Retry policies for network failures
  - [ ] Graceful degradation
  - [ ] User-friendly error messages
  - [ ] Error reporting (Sentry?)

- [ ] **Web UI Polish**
  - [ ] Responsive design improvements
  - [ ] Dark mode (already has `next-themes`)
  - [ ] Accessibility audit (WCAG 2.1 AA)
  - [ ] Loading states

#### High
- [ ] **Batch Downloads**
  - [ ] Queue multiple videos
  - [ ] Bulk operations (cancel all, retry all)
  - [ ] CSV/JSON import

- [ ] **Advanced Time Input**
  - [ ] Time ranges (1h30m-2h15m)
  - [ ] Chapter selection
  - [ ] Scene detection

#### Medium
- [ ] **Custom FFmpeg Options**
  - [ ] Codec selection
  - [ ] Bitrate control
  - [ ] Resolution scaling
  - [ ] Audio normalization

- [ ] **Subtitle Support**
  - [ ] Auto-download subtitles
  - [ ] Burn-in subtitles
  - [ ] Subtitle format selection

### Deprecations
- None planned

## v0.3.0 - Data & History

### Timeline
- **Target**: Q3 2026
- **Priority**: High

### Features

#### Critical
- [ ] **Database**
  - [ ] Choose: SQLite (simple) vs PostgreSQL (production-ready)
  - [ ] EF Core integration
  - [ ] Migration system
  - [ ] Backup/restore

- [ ] **Download History**
  - [ ] Store all downloads
  - [ ] Search by URL, title, date
  - [ ] Re-download from history
  - [ ] Delete from history

- [ ] **User Accounts**
  - [ ] Authentication (Auth0? IdentityServer?)
  - [ ] User-specific download history
  - [ ] API keys for third-party access

#### High
- [ ] **Advanced Search**
  - [ ] Filter by platform, format, quality
  - [ ] Sort by date, size, duration
  - [ ] Full-text search

- [ ] **Analytics**
  - [ ] Download statistics
  - [ ] Most used platforms/formats
  - [ ] Storage usage

#### Medium
- [ ] **Import/Export**
  - [ ] Export history to CSV/JSON
  - [ ] Import from other tools
  - [ ] Backup to cloud

### Deprecations
- File-based history (recovery mode)

## v0.4.0 - Cloud & Scale

### Timeline
- **Target**: Q4 2026
- **Priority**: Medium

### Features

#### Critical
- [ ] **Cloud Storage**
  - [ ] S3 integration
  - [ ] Azure Blob
  - [ ] User-selectable storage

- [ ] **Worker Scaling**
  - [ ] Multiple worker instances
  - [ ] Auto-scaling based on queue depth
  - [ ] Priority queues

#### High
- [ ] **CDN Integration**
  - [ ] CloudFront
  - [ ] Azure CDN
  - [ ] Serve downloads from CDN

- [ ] **Advanced Processing**
  - [ ] GPU acceleration (FFmpeg)
  - [ ] Distributed processing
  - [ ] Parallel FFmpeg

### Deprecations
- None planned

## v0.5.0 - Ecosystem

### Timeline
- **Target**: Q1 2027
- **Priority**: Low

### Features

#### Critical
- [ ] **API Stability**
  - [ ] Versioned API (v1, v2)
  - [ ] OpenAPI documentation
  - [ ] Breaking change policy

- [ ] **Third-Party Integrations**
  - [ ] Webhooks
  - [ ] OAuth apps
  - [ ] Public API keys

#### High
- [ ] **Automation**
  - [ ] Scheduled downloads
  - [ ] Watch channel/playlist
  - [ ] RSS feed monitoring

- [ ] **Plugins**
  - [ ] Custom processing stages
  - [ ] Custom platform support
  - [ ] Plugin marketplace?

### Deprecations
- None planned

## v1.0.0 - Production Ready

### Timeline
- **Target**: Q2 2027
- **Priority**: Critical

### Criteria
- [ ] **Feature Complete**: All v0.x features implemented
- [ ] **Tested**: 80%+ test coverage
- [ ] **Documented**: Comprehensive docs
- [ ] **Secure**: Auth, rate limiting, input sanitization
- [ ] **Scalable**: Horizontal scaling tested
- [ ] **Monitored**: Metrics, logging, alerts

### Milestones
- [ ] Alpha: Internal testing
- [ ] Beta: Public testing
- [ ] RC: Release candidates
- [ ] Stable: Production release

## Backlog

### Features Not Yet Scheduled

#### Low Priority
- [ ] **Mobile Apps** (use PWA instead)
- [ ] **Desktop Apps** (use web or CLI)
- [ ] **Browser Extensions** (use API)
- [ ] **Video Editing** (use dedicated tools)
- [ ] **Social Media Posting** (use dedicated tools)
- [ ] **Content Recommendation** (out of scope)
- [ ] **DRM Circumvention** (illegal)

#### Future Consideration
- [ ] **AI Features**
  - [ ] Auto-chapter detection
  - [ ] Scene segmentation
  - [ ] Content summarization

- [ ] **Collaboration**
  - [ ] Shared workspaces
  - [ ] Team accounts
  - [ ] Approval workflows

## Deprecation Policy

### Version Support
- **Current version**: Full support
- **Previous version**: Security fixes only
- **Older versions**: No support

### API Deprecation
- **Minimum notice**: 6 months
- **Migration guide**: Required
- **Breaking changes**: Major version bump

### Deprecation Process
1. Announce deprecation
2. Provide migration guide
3. Mark as deprecated in code
4. Remove in next major version

## Feature Request Process

### For Users
1. Create GitHub issue
2. Provide use case, not just feature
3. Accept discussion

### For Developers
1. Create PR with specification
2. Discuss with maintainer
3. Get approval before implementation

## Risk Mitigation

### Technical Risks
- **yt-dlp breaks**: Auto-update, fallback
- **Platform changes**: Monitor, rapid response
- **Scaling issues**: Load test, optimize

### Project Risks
- **Scope creep**: Strict prioritization
- **Abandonment**: Open source, community
- **Legal issues**: Disclaimer, ToS compliance

## Communication

### Release Announcements
- **GitHub Releases**: Full changelog
- **Blog Posts**: Major versions
- **Social Media**: User communication

### Roadmap Updates
- **Quarterly**: Review and adjust
- **Publicly**: GitHub Projects
- **Transparent**: Delays, changes

## Metrics

### Progress Tracking
- **Velocity**: Features per sprint
- **Burndown**: Remaining work
- **Cycle Time**: Idea to release

### Success Metrics
- **User Adoption**: Active users
- **Feature Usage**: Most used features
- **Satisfaction**: NPS score

## Dependencies

### External
- **yt-dlp updates**: Monitor, test
- **FFmpeg releases**: Monitor, test
- **.NET releases**: Plan upgrades

### Internal
- **Domain stability**: Avoid breaking changes
- **API stability**: Versioning strategy
- **Database migrations**: Plan carefully

## Resources

### Planning
- **GitHub Projects**: Feature tracking
- **GitHub Milestones**: Release planning
- **bd (Beads)**: Development tasks

### Documentation
- **CHANGELOG.md**: Version history
- **docs/**: User and developer docs
- **AGENTS.md**: Development workflow

## References

### Versioning
- [Semantic Versioning](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)

### Planning
- [Agile Planning](https://www.mountaingoatsoftware.com/blog/the-problem-with-sprints-and-a-solution/)
- [Feature Flagging](https://martinfowler.com/articles/feature-toggles.html)

### Communication
- [Changelog Best Practices](https://keepachangelog.com/)
- [Release Notes](https://github.com/swcarpentry/release-notes/blob/master/README.md)
