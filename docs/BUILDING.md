# Building & Packaging Brenda

## Development build

```bash
dotnet run --project src/Brenda.App
```

## Tests

```bash
dotnet test Brenda.sln
```

## Building an installer (Velopack)

Brenda uses [Velopack](https://velopack.io) to create installers and enable delta
auto-updates. The `vpk` CLI is registered as a local dotnet tool
(`.config/dotnet-tools.json`), so no global installs are needed.

### Windows

```powershell
./build/pack.ps1 -Version 0.1.0
```

Produces in `releases/`:

- `Brenda-win-Setup.exe` — the installer to share with users
- `Brenda-*-full.nupkg` / `*-delta.nupkg` — update packages
- `RELEASES` — update feed metadata

### Linux

```bash
./build/pack.sh 0.1.0 linux-x64
```

### macOS

```bash
./build/pack.sh 0.1.0 osx-arm64   # Apple Silicon
./build/pack.sh 0.1.0 osx-x64     # Intel
```

## Releases & auto-update

The GitHub Actions workflow `.github/workflows/release.yml` builds installers for all
platforms and attaches them to a GitHub Release whenever a `v*` tag is pushed:

```bash
git tag v0.1.0
git push origin v0.1.0
```

The app checks GitHub Releases for updates (Settings → Check for Updates). The
repository URL used by the updater lives in
`src/Brenda.App/Services/UpdateService.cs` — update it after forking/publishing.

## Versioning

Set the version in one place per release: pass `-Version`/first argument to the pack
scripts (which flows into `dotnet publish -p:Version=` and `--packVersion`). The
default assembly version is defined in `Directory.Build.props`.
