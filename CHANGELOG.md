# Changelog

## [Unreleased]

### Changed
- Migrated from .NET 9.0 to .NET 10.0 LTS
- Updated YoutubeExplode from 6.5.4 to 6.5.6
- Updated FFmpeg.AutoGen from 7.0.0 to 8.0.0
- Configured ASDF .tool-versions for project-local .NET 10.0

### Fixed
- Improved error messages on Linux

### Performance
- Benefits from .NET 10 JIT improvements
- Faster startup time

### Known Issues
- YoutubeExplode currently experiencing 403 Forbidden errors due to YouTube's new PO token requirement (Jan 2026)
- Issue tracked at: https://github.com/Tyrrrz/YoutubeExplode/issues/933
- Fix PR pending: https://github.com/Tyrrrz/YoutubeExplode/pull/934
- This is NOT related to the .NET 10 migration
