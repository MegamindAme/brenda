using Brenda.Core.Models;

namespace Brenda.Core.Abstractions;

/// <summary>Finds and removes Blender leftover files (.blend1 backups, temp files).</summary>
public interface ICleanupService
{
    Task<IReadOnlyList<CleanupItem>> ScanAsync(
        IEnumerable<string> rootDirectories,
        IProgress<string>? progress = null,
        CancellationToken ct = default);

    /// <summary>Deletes the given items. Returns the number of bytes reclaimed.</summary>
    Task<long> DeleteAsync(IEnumerable<CleanupItem> items, CancellationToken ct = default);
}
