# Brenda — The Blender Hub

A centralized, professional desktop application to manage your Blender versions,
projects, assets and rendering pipelines. Cross-platform (Windows, macOS, Linux),
built with C# and Avalonia.

> Based on the [Brenda Hub proposal](https://megamindame.com/brenda/).

## Features (v0.1 — roadmap "Foundation")

- **Version management** — browse official Blender releases (stable & LTS), one-click
  install on Windows, register existing installations on any OS, set a default,
  launch any version, and get notified when a newer release is available.
- **Smart file opening** — Brenda reads the `.blend` file header and opens each file
  with the matching installed Blender series (pinned version wins).
- **Project management** — a centralized dashboard for all projects; create new
  projects from folder templates (Standard, Animation, Asset Creation, or your own),
  import existing folders, and keep each project self-contained via a `brenda.json`
  manifest.
- **Memory cleaning** — scan projects or any folder for `.blend1`/`.blend2` backups and
  temp files, review them, and reclaim disk space.

Planned (see the [roadmap](docs/ARCHITECTURE.md#roadmap-mapping)): Git & GitHub
integration, asset libraries, background & farm rendering, project compression.

## Getting started

### Users

Download the installer for your OS from the
[Releases](../../releases) page and run it. The app auto-updates.

### Developers

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
git clone <repo-url>
cd Brenda
dotnet run --project src/Brenda.App
```

Run the tests:

```bash
dotnet test Brenda.sln
```

Build an installer (see [docs/BUILDING.md](docs/BUILDING.md) for details):

```powershell
./build/pack.ps1 -Version 0.1.0
```

## Project structure

```
src/
  Brenda.Core/            Domain models + service interfaces (no dependencies)
  Brenda.Infrastructure/  SQLite persistence, Blender integration, cleanup
  Brenda.App/             Avalonia UI (ReactiveUI MVVM)
tests/                    xUnit test projects
docs/                     Architecture & build documentation
build/                    Velopack packaging scripts
```

Read [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) before contributing — it explains the
layering rules and key decisions.

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE)
