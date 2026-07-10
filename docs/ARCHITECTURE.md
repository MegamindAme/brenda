# Brenda Architecture

This document describes the architecture of Brenda: The Blender Hub. Keep it up to date
whenever structural decisions change.

## Overview

Brenda is a cross-platform desktop application (Windows/macOS/Linux) built with:

| Concern | Technology |
|---|---|
| Runtime | .NET 8 (LTS) |
| UI framework | Avalonia 11 (Fluent theme) |
| MVVM | ReactiveUI |
| Dependency injection | Microsoft.Extensions.DependencyInjection |
| Persistence | SQLite via EF Core (migrations) |
| Logging | Serilog (rolling files in the app data folder) |
| Packaging & updates | Velopack (GitHub Releases as update feed) |

## Layers

The solution follows a clean, three-layer architecture. Dependencies point strictly
downward; the domain layer knows nothing about the UI or infrastructure.

```
┌───────────────────────────────────────────────┐
│ Brenda.App          (Avalonia + ReactiveUI)   │
│  Views ── ViewModels ── UI Services (dialogs) │
└───────────────┬───────────────────────────────┘
                │ depends on interfaces only
┌───────────────▼───────────────────────────────┐
│ Brenda.Infrastructure                         │
│  EF Core (SQLite) · Blender CLI/downloads ·   │
│  cleanup scanner · future-module stubs        │
└───────────────┬───────────────────────────────┘
                │ implements
┌───────────────▼───────────────────────────────┐
│ Brenda.Core        (zero dependencies)        │
│  Domain models · service abstractions         │
└───────────────────────────────────────────────┘
```

### Brenda.Core

Pure domain layer with **no external dependencies**.

- `Models/` — `BlenderVersion`, `BlenderRelease`, `Project`, `ProjectTemplate`,
  `CleanupItem`, `DownloadProgress`, `BlenderChannel`.
- `Abstractions/` — service interfaces consumed by view models:
  - `IBlenderVersionService` — installed versions: list, add existing, set default, remove, launch, resolve version for a `.blend` file.
  - `IBlenderReleaseProvider` — available official builds.
  - `IBlenderInstaller` — download + extract managed installations.
  - `IBlendFileInspector` — read the `.blend` header to detect the saving Blender series.
  - `IBlenderRegistry` — persistence seam used by the installer.
  - `IProjectService` / `IProjectTemplateService` — project CRUD and templates.
  - `ICleanupService` — leftover-file scanning and deletion.
  - `IAppPaths` — well-known data directories.
  - `Abstractions/Future/` — reserved interfaces for roadmap modules
    (`IRenderService`, `IGitService`, `IAssetLibraryService`).

### Brenda.Infrastructure

Implements the Core abstractions.

- `Data/` — `BrendaDbContext` (EF Core + SQLite), migrations, design-time factory.
  Migrations are applied automatically at app startup (`Database.Migrate()`).
- `Blender/`
  - `BlenderReleaseProvider` — parses the directory listings of
    `download.blender.org/release/` for the current OS/architecture. Only series
    ≥ 2.83 are considered; known LTS series are tagged.
  - `BlenderInstaller` — streams the archive with progress, extracts `.zip` builds into
    `%APPDATA%/Brenda/versions/`. (Non-zip formats — Linux `.tar.xz`, macOS `.dmg` —
    are not auto-installed yet; users can register existing installs instead.)
  - `BlenderVersionService` — SQLite-backed version registry; detects versions via
    `blender --version`; launches Blender; resolves the best version for a file
    (pinned → matching series → default → newest).
  - `BlendFileInspector` — reads the 12-byte `.blend` header, supporting plain,
    gzip- and zstd-compressed files.
- `Projects/`
  - `ProjectService` — project CRUD. Projects are **self-contained**: metadata is
    mirrored to a `brenda.json` manifest inside each project folder so folders stay
    portable. Removal never deletes files on disk.
  - `ProjectTemplateService` — built-in templates plus user JSON templates from
    `%APPDATA%/Brenda/templates/`.
  - `ProjectManifest` — the `brenda.json` schema.
- `Cleanup/CleanupService` — recursive scan for `.blendN` backups and temp files.
- `Stubs/` — no-op implementations of the future-module interfaces, registered in DI.
- `ServiceCollectionExtensions.AddBrendaInfrastructure()` — single registration
  entry point used by the app.

### Brenda.App

Avalonia UI. View models depend **only on Core interfaces**, making them unit-testable.

- Navigation: a sidebar (`MainWindowViewModel.NavigationItems`) selects a
  `PageViewModel`; views are resolved by naming convention via `ViewLocator`
  (`FooViewModel` → `FooView`). Pages are created lazily and cached.
- MVVM: `ReactiveObject`, `ReactiveCommand` (async commands with centralized
  `ThrownExceptions` handling → error dialogs + logs).
- `Services/` — UI-only services: `IDialogService` (file/folder pickers, confirm/error
  dialogs) and `IUpdateService` (Velopack self-update against GitHub Releases).
- Pages: Projects, Versions, Cleanup, Settings, plus "coming soon" placeholders for
  Rendering, Git and Assets.
- Composition root: `App.axaml.cs` builds the `ServiceProvider`, configures Serilog and
  applies EF migrations before showing the main window. `Program.cs` runs the Velopack
  hooks first.

## Data

- **SQLite database** at `%APPDATA%/Brenda/brenda.db`; tables: `BlenderVersions`,
  `Projects` (FK `PinnedBlenderVersionId`, delete → `SET NULL`).
- **Managed Blender installs** under `%APPDATA%/Brenda/versions/<archive-name>/`.
- **Logs** under `%APPDATA%/Brenda/logs/` (daily rolling, 7 files kept).
- **Schema changes** require an EF migration:
  `dotnet ef migrations add <Name> --project src/Brenda.Infrastructure --startup-project src/Brenda.Infrastructure`.

## Key decisions

1. **Interfaces in Core, implementations in Infrastructure** — enables unit testing and
   future replacement (e.g. a different release feed) without touching view models.
2. **`IDbContextFactory` + short-lived contexts** — services are singletons; each
   operation creates its own `DbContext`, avoiding threading issues with Avalonia.
3. **Self-contained projects (`brenda.json`)** — the database is a cache/index; the
   source of truth travels with the project folder (per the "Self-Contained Projects"
   feature of the proposal).
4. **Future modules as stubs** — roadmap v1.1/v2.0 features (rendering, git, assets)
   have reserved interfaces, DI registrations and navigation entries so they can be
   implemented without restructuring.
5. **Velopack for distribution** — produces installers for all three OSes and gives us
   delta auto-updates from GitHub Releases (see `build/` and `docs/BUILDING.md`).

## Testing

- `tests/Brenda.Core.Tests` — domain model behavior.
- `tests/Brenda.Infrastructure.Tests` — parser/scanner/service tests using temp
  directories and NSubstitute fakes (`IAppPaths`). HTML parsing and `.blend` header
  parsing are covered by unit tests with fixture strings.
- UI view models are constructor-injected and can be tested with substituted services
  (dialogs are behind `IDialogService`).

## Roadmap mapping

| Proposal feature | Status | Where |
|---|---|---|
| Version management (install/add/update) | v0.1 ✅ | `Blender/` + Versions page |
| Project dashboard, templates, icons | v0.1 ✅ | `Projects/` + Projects page |
| Memory cleaning | v0.1 ✅ | `Cleanup/` + Cleanup page |
| Auto-open .blend with correct version | v0.1 ✅ | `BlendFileInspector` + `ResolveVersionForFileAsync` |
| Resource viewer, project docs | v1.1 ⏳ | to be added under Projects |
| Git & GitHub integration | v1.1 ⏳ | `IGitService` reserved |
| Asset libraries | v1.1 ⏳ | `IAssetLibraryService` reserved |
| Background/parallel rendering, render farms | v1.1/v2.0 ⏳ | `IRenderService` reserved |
| Project compression | v2.0 ⏳ | future utility module |
