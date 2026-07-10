using System.Text.RegularExpressions;
using Brenda.Core.Abstractions;
using Brenda.Core.Models;
using Microsoft.Extensions.Logging;

namespace Brenda.Infrastructure.Cleanup;

/// <summary>Scans directories for Blender leftovers: numbered backups (.blend1, .blend2, ...)
/// and temp files (quit.blend, autosaves).</summary>
public sealed partial class CleanupService : ICleanupService
{
    private readonly ILogger<CleanupService> _logger;

    public CleanupService(ILogger<CleanupService> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<CleanupItem>> ScanAsync(
        IEnumerable<string> rootDirectories,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var roots = rootDirectories.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            foreach (var root in roots)
            {
                progress?.Report(root);
                foreach (var path in Directory.EnumerateFiles(root, "*", options))
                {
                    ct.ThrowIfCancellationRequested();

                    var kind = Classify(path);
                    if (kind is null)
                    {
                        continue;
                    }

                    var info = new FileInfo(path);
                    items.Add(new CleanupItem
                    {
                        Path = path,
                        SizeBytes = info.Length,
                        Kind = kind.Value,
                        LastModifiedUtc = info.LastWriteTimeUtc
                    });
                }
            }

            return (IReadOnlyList<CleanupItem>)items;
        }, ct);
    }

    public async Task<long> DeleteAsync(IEnumerable<CleanupItem> items, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            long reclaimed = 0;
            foreach (var item in items)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    if (File.Exists(item.Path))
                    {
                        File.Delete(item.Path);
                        reclaimed += item.SizeBytes;
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.LogWarning(ex, "Could not delete {Path}", item.Path);
                }
            }

            return reclaimed;
        }, ct);
    }

    internal static CleanupKind? Classify(string path)
    {
        var fileName = Path.GetFileName(path);

        if (BlendBackupRegex().IsMatch(fileName))
        {
            return CleanupKind.BlendBackup;
        }

        if (fileName.Equals("quit.blend", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".blend@", StringComparison.OrdinalIgnoreCase))
        {
            return CleanupKind.TempFile;
        }

        return null;
    }

    [GeneratedRegex(@"\.blend\d+$", RegexOptions.IgnoreCase)]
    private static partial Regex BlendBackupRegex();
}
