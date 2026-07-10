# Changelog

All notable changes to this project will be documented in this file.

## [0.1.0] - 2026-07-10

### Added

- Initial release of Brenda: The Blender Hub.
- Blender version management (browse, install, register, launch, updates).
- Project management dashboard with folder templates and a `brenda.json` manifest.
- Memory cleanup scanner for `.blend1`/`.blend2` backups and temp files.
- Smart file opening that reads `.blend` headers and launches the matching Blender version.
- Cross-platform Avalonia desktop UI with ReactiveUI MVVM.
- SQLite persistence via EF Core with automatic migrations.
- Velopack packaging and auto-updater integration.
- CI/CD workflows for build, test, and release.
