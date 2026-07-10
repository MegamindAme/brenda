using System.Diagnostics;
using System.Text.RegularExpressions;
using Brenda.Core.Abstractions;
using Brenda.Core.Models;
using Brenda.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Brenda.Infrastructure.Blender;

/// <summary>EF Core-backed implementation of Blender version management.</summary>
public sealed partial class BlenderVersionService : IBlenderVersionService, IBlenderRegistry
{
    private readonly IDbContextFactory<BrendaDbContext> _dbFactory;
    private readonly IBlendFileInspector _inspector;
    private readonly ILogger<BlenderVersionService> _logger;

    public BlenderVersionService(
        IDbContextFactory<BrendaDbContext> dbFactory,
        IBlendFileInspector inspector,
        ILogger<BlenderVersionService> logger)
    {
        _dbFactory = dbFactory;
        _inspector = inspector;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BlenderVersion>> GetInstalledAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.BlenderVersions
            .AsNoTracking()
            .OrderByDescending(v => v.Version)
            .ToListAsync(ct);
    }

    public async Task AddAsync(BlenderVersion version, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var existing = await db.BlenderVersions
            .FirstOrDefaultAsync(v => v.ExecutablePath == version.ExecutablePath, ct);
        if (existing is not null)
        {
            existing.Version = version.Version;
            existing.Channel = version.Channel;
            existing.InstallPath = version.InstallPath;
            existing.IsManaged = version.IsManaged;
        }
        else
        {
            if (!await db.BlenderVersions.AnyAsync(ct))
            {
                version.IsDefault = true;
            }

            db.BlenderVersions.Add(version);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<BlenderVersion> AddExistingAsync(string executablePath, CancellationToken ct = default)
    {
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException("Blender executable not found.", executablePath);
        }

        var versionString = await DetectVersionAsync(executablePath, ct)
            ?? throw new InvalidOperationException(
                $"'{executablePath}' does not appear to be a Blender executable (could not detect its version).");

        var version = new BlenderVersion
        {
            Version = versionString,
            Channel = BlenderChannel.Custom,
            ExecutablePath = executablePath,
            InstallPath = Path.GetDirectoryName(executablePath) ?? string.Empty,
            IsManaged = false
        };

        await AddAsync(version, ct);
        return version;
    }

    public async Task SetDefaultAsync(int versionId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var versions = await db.BlenderVersions.ToListAsync(ct);
        foreach (var version in versions)
        {
            version.IsDefault = version.Id == versionId;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(int versionId, bool deleteFiles, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var version = await db.BlenderVersions.FindAsync([versionId], ct);
        if (version is null)
        {
            return;
        }

        if (deleteFiles && version.IsManaged && Directory.Exists(version.InstallPath))
        {
            await Task.Run(() => Directory.Delete(version.InstallPath, recursive: true), ct);
        }

        db.BlenderVersions.Remove(version);
        await db.SaveChangesAsync(ct);
    }

    public Task LaunchAsync(BlenderVersion version, string? blendFilePath = null, CancellationToken ct = default)
    {
        var startInfo = new ProcessStartInfo(version.ExecutablePath)
        {
            UseShellExecute = false,
            WorkingDirectory = version.InstallPath
        };

        if (!string.IsNullOrWhiteSpace(blendFilePath))
        {
            startInfo.ArgumentList.Add(blendFilePath);
        }

        _logger.LogInformation("Launching Blender {Version} {File}", version.Version, blendFilePath ?? "(no file)");
        Process.Start(startInfo);
        return Task.CompletedTask;
    }

    public async Task<BlenderVersion?> ResolveVersionForFileAsync(
        string blendFilePath,
        int? pinnedVersionId = null,
        CancellationToken ct = default)
    {
        var installed = await GetInstalledAsync(ct);
        if (installed.Count == 0)
        {
            return null;
        }

        if (pinnedVersionId is not null)
        {
            var pinned = installed.FirstOrDefault(v => v.Id == pinnedVersionId);
            if (pinned is not null)
            {
                return pinned;
            }
        }

        var series = await _inspector.TryGetSeriesAsync(blendFilePath, ct);
        if (series is not null)
        {
            var match = installed
                .Where(v => v.Series == series)
                .MaxBy(v => v.Version);
            if (match is not null)
            {
                return match;
            }
        }

        return installed.FirstOrDefault(v => v.IsDefault) ?? installed[0];
    }

    private static async Task<string?> DetectVersionAsync(string executablePath, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--version");

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return null;
        }

        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        var match = VersionOutputRegex().Match(output);
        return match.Success ? match.Groups["version"].Value : null;
    }

    [GeneratedRegex(@"Blender\s+(?<version>\d+\.\d+(\.\d+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex VersionOutputRegex();
}
