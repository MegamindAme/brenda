# Contributing to Brenda

Thanks for your interest in contributing! This document explains how to get set up and
what we expect from contributions.

## Getting set up

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Fork and clone the repository.
3. `dotnet run --project src/Brenda.App` starts the app; `dotnet test Brenda.sln` runs
   the test suite.

Any editor works. The repository ships an `.editorconfig`; please keep it honored.

## Before you start

- **Read [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).** It defines the layering rules:
  - `Brenda.Core` must stay dependency-free.
  - View models depend on Core interfaces only — never on Infrastructure types.
  - New services get an interface in `Core/Abstractions` and an implementation in
    `Infrastructure`, registered in `ServiceCollectionExtensions`.
- For larger features (especially roadmap items like rendering or Git integration),
  open an issue first so we can discuss the design.

## Making changes

- Create a feature branch from `main`.
- Add or update tests for behavior you change. Parser/scanner logic must have unit tests.
- Database schema changes need an EF Core migration:

  ```bash
  dotnet ef migrations add <Name> --project src/Brenda.Infrastructure --startup-project src/Brenda.Infrastructure
  ```

- Make sure `dotnet build Brenda.sln` and `dotnet test Brenda.sln` pass locally.
- Update `docs/ARCHITECTURE.md` if you changed the structure or made a notable decision.

## Pull requests

- Keep PRs focused; unrelated refactors belong in separate PRs.
- Describe *what* and *why* in the PR description.
- CI must be green before review.

## Code style

- C# 12, nullable reference types enabled, file-scoped namespaces.
- Prefer explicit, readable code over cleverness.
- XML doc comments on public Core abstractions.

## Reporting bugs

Open an issue with reproduction steps, your OS, the app version (Settings page), and
relevant logs from `%APPDATA%/Brenda/logs/` (or `~/.config/Brenda/logs/` on Linux).
