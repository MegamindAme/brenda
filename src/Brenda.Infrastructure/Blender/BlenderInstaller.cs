using System.IO.Compression;
using Brenda.Core.Abstractions;
using Brenda.Core.Models;
using Microsoft.Extensions.Logging;

namespace Brenda.Infrastructure.Blender;

/// <summary>
/// Downloads Blender archives and extracts them into the managed versions folder.
/// Automatic installation currently supports .zip archives (Windows builds);
/// other platforms can register existing installations instead.
/// </summary>
public sealed class BlenderInstaller : IBlenderInstaller
{
    private readonly HttpClient _httpClient;
    private readonly IAppPaths _paths;
    private readonly IBlenderRegistry _registry;
    private readonly ILogger<BlenderInstaller> _logger;

    public BlenderInstaller(
        HttpClient httpClient,
        IAppPaths paths,
        IBlenderRegistry registry,
        ILogger<BlenderInstaller> logger)
    {
        _httpClient = httpClient;
        _paths = paths;
        _registry = registry;
        _logger = logger;
    }

    public bool CanInstall(BlenderRelease release) =>
        release.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

    public async Task<BlenderVersion> DownloadAndInstallAsync(
        BlenderRelease release,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (!CanInstall(release))
        {
            throw new NotSupportedException(
                $"Automatic installation of '{release.FileName}' is not supported on this platform yet. " +
                "Install Blender manually and use 'Add Existing'.");
        }

        var archivePath = Path.Combine(Path.GetTempPath(), release.FileName);
        try
        {
            await DownloadAsync(release, archivePath, progress, ct);

            progress?.Report(new DownloadProgress(0, null, "Extracting..."));
            var installPath = Path.Combine(_paths.VersionsDirectory, Path.GetFileNameWithoutExtension(release.FileName));
            if (Directory.Exists(installPath))
            {
                Directory.Delete(installPath, recursive: true);
            }

            await Task.Run(() => ZipFile.ExtractToDirectory(archivePath, installPath), ct);

            var executablePath = FindExecutable(installPath)
                ?? throw new InvalidOperationException($"No Blender executable found in '{installPath}'.");

            var version = new BlenderVersion
            {
                Version = release.Version,
                Channel = release.Channel,
                ExecutablePath = executablePath,
                InstallPath = installPath,
                IsManaged = true
            };

            await _registry.AddAsync(version, ct);
            _logger.LogInformation("Installed Blender {Version} to {Path}", release.Version, installPath);
            return version;
        }
        finally
        {
            if (File.Exists(archivePath))
            {
                try
                {
                    File.Delete(archivePath);
                }
                catch (IOException)
                {
                    // Leftover temp archive is harmless.
                }
            }
        }
    }

    private async Task DownloadAsync(
        BlenderRelease release,
        string destinationPath,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;
        await using var source = await response.Content.ReadAsStreamAsync(ct);
        await using var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[81920];
        long received = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, ct)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), ct);
            received += read;
            progress?.Report(new DownloadProgress(received, total, "Downloading..."));
        }
    }

    internal static string? FindExecutable(string installPath)
    {
        var executableName = OperatingSystem.IsWindows() ? "blender.exe" : "blender";
        return Directory
            .EnumerateFiles(installPath, executableName, SearchOption.AllDirectories)
            .FirstOrDefault();
    }
}
