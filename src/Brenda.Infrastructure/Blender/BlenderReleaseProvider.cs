using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Brenda.Core.Abstractions;
using Brenda.Core.Models;
using Microsoft.Extensions.Logging;

namespace Brenda.Infrastructure.Blender;

/// <summary>
/// Discovers official Blender builds by parsing the directory listings at
/// https://download.blender.org/release/. Only the most recent series are queried.
/// </summary>
public sealed partial class BlenderReleaseProvider : IBlenderReleaseProvider
{
    private const string ReleaseIndexUrl = "https://download.blender.org/release/";
    private const int SeriesToQuery = 6;

    /// <summary>Blender series designated as LTS by the Blender Foundation.</summary>
    private static readonly HashSet<string> LtsSeries = ["2.83", "2.93", "3.3", "3.6", "4.2"];

    private readonly HttpClient _httpClient;
    private readonly ILogger<BlenderReleaseProvider> _logger;

    public BlenderReleaseProvider(HttpClient httpClient, ILogger<BlenderReleaseProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BlenderRelease>> GetAvailableReleasesAsync(CancellationToken ct = default)
    {
        var index = await _httpClient.GetStringAsync(ReleaseIndexUrl, ct);
        var series = ParseSeriesDirectories(index)
            .OrderByDescending(s => s.major)
            .ThenByDescending(s => s.minor)
            .Take(SeriesToQuery)
            .ToList();

        var releases = new List<BlenderRelease>();
        foreach (var (name, _, _) in series)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var listing = await _httpClient.GetStringAsync($"{ReleaseIndexUrl}{name}/", ct);
                releases.AddRange(ParseReleases(listing, $"{ReleaseIndexUrl}{name}/"));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to fetch release listing for series {Series}", name);
            }
        }

        return releases
            .OrderByDescending(r => VersionSortKey(r.Version))
            .ToList();
    }

    internal static IEnumerable<(string name, int major, int minor)> ParseSeriesDirectories(string indexHtml)
    {
        foreach (Match match in SeriesDirectoryRegex().Matches(indexHtml))
        {
            var major = int.Parse(match.Groups["major"].Value);
            var minor = int.Parse(match.Groups["minor"].Value);

            // Automatic installs target modern builds only (2.83+ have consistent archive naming).
            if (major > 2 || (major == 2 && minor >= 83))
            {
                yield return (match.Groups["name"].Value, major, minor);
            }
        }
    }

    internal static IEnumerable<BlenderRelease> ParseReleases(string listingHtml, string baseUrl)
    {
        var pattern = GetPlatformFilePattern();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in pattern.Matches(listingHtml))
        {
            var fileName = match.Groups["file"].Value;
            if (!seen.Add(fileName))
            {
                continue;
            }

            var version = match.Groups["version"].Value;
            yield return new BlenderRelease
            {
                Version = version,
                Channel = LtsSeries.Contains(ToSeries(version)) ? BlenderChannel.Lts : BlenderChannel.Stable,
                FileName = fileName,
                DownloadUrl = baseUrl + fileName
            };
        }
    }

    private static Regex GetPlatformFilePattern()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return WindowsFileRegex();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.OSArchitecture == Architecture.Arm64 ? MacArmFileRegex() : MacX64FileRegex();
        }

        return LinuxFileRegex();
    }

    private static string ToSeries(string version)
    {
        var parts = version.Split('.');
        return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : version;
    }

    private static (int, int, int) VersionSortKey(string version)
    {
        var parts = version.Split('.');
        return (
            parts.Length > 0 && int.TryParse(parts[0], out var a) ? a : 0,
            parts.Length > 1 && int.TryParse(parts[1], out var b) ? b : 0,
            parts.Length > 2 && int.TryParse(parts[2], out var c) ? c : 0);
    }

    [GeneratedRegex(@"href=""(?<name>Blender(?<major>\d+)\.(?<minor>\d+))/?""", RegexOptions.IgnoreCase)]
    private static partial Regex SeriesDirectoryRegex();

    [GeneratedRegex(@"href=""(?<file>blender-(?<version>\d+\.\d+\.\d+)-windows-x64\.zip)""", RegexOptions.IgnoreCase)]
    private static partial Regex WindowsFileRegex();

    [GeneratedRegex(@"href=""(?<file>blender-(?<version>\d+\.\d+\.\d+)-linux-x64\.tar\.xz)""", RegexOptions.IgnoreCase)]
    private static partial Regex LinuxFileRegex();

    [GeneratedRegex(@"href=""(?<file>blender-(?<version>\d+\.\d+\.\d+)-macos-arm64\.dmg)""", RegexOptions.IgnoreCase)]
    private static partial Regex MacArmFileRegex();

    [GeneratedRegex(@"href=""(?<file>blender-(?<version>\d+\.\d+\.\d+)-macos-x64\.dmg)""", RegexOptions.IgnoreCase)]
    private static partial Regex MacX64FileRegex();
}
