# Changelog

## [Unreleased]

### Changed
- **Cutube is a standalone CLI again** — the distributed system (REST API,
  Next.js web app, RabbitMQ worker, Docker setup) moved to the **Orotube**
  repository (Azure DevOps: `oroborus/oroborus-auto/orotube`)
- Removed `Cutube.Api`, `Cutube.Web`, `Cutube.Worker` and `Cutube.Contracts`
  projects and their test suites
- Removed the CLI remote API mode (`--api-url`/`--local` flags, `ApiClient`);
  the CLI is local-only again
- Removed Turborepo/pnpm workspace; the repo is a plain .NET solution again
  (build/test via `dotnet build` / `dotnet test`)
- Full pre-split history preserved in the `legacy/cutube-develop` branch
- Migrated from YoutubeExplode to yt-dlp + YoutubeDLSharp
- Removed YoutubeExplode dependencies (6.5.6)
- Added YoutubeDLSharp 1.2.0
- Implemented yt-dlp bundling with auto-update
- Updated code to use yt-dlp for video downloads

### Fixed
- Resolved 403 Forbidden errors caused by YouTube PO token requirement (Jan 2026)
- Improved resilience against YouTube API changes

### Added
- YtDlpHelper.cs with 3-layer fallback system
- Automatic yt-dlp updates on startup
- Zero-config experience for end users

### Removed
- YoutubeExplode (deprecated due to PO token issues)
- YoutubeExplode.Converter

### Performance
- Benefits from .NET 10 JIT improvements
- Faster startup time
- More reliable downloads with yt-dlp community support

### Known Issues
- None currently (previous 403 issue resolved)
