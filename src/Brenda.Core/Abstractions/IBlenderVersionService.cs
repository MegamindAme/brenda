using Brenda.Core.Models;

namespace Brenda.Core.Abstractions;

/// <summary>Manages Blender installations known to Brenda.</summary>
public interface IBlenderVersionService
{
    Task<IReadOnlyList<BlenderVersion>> GetInstalledAsync(CancellationToken ct = default);

    /// <summary>Registers an already-installed Blender by pointing at its executable.</summary>
    Task<BlenderVersion> AddExistingAsync(string executablePath, CancellationToken ct = default);

    Task SetDefaultAsync(int versionId, CancellationToken ct = default);

    /// <summary>Removes a version from Brenda. Deletes files only when the install is managed and <paramref name="deleteFiles"/> is true.</summary>
    Task RemoveAsync(int versionId, bool deleteFiles, CancellationToken ct = default);

    /// <summary>Launches Blender, optionally opening a .blend file.</summary>
    Task LaunchAsync(BlenderVersion version, string? blendFilePath = null, CancellationToken ct = default);

    /// <summary>Picks the best installed version for a .blend file: pinned &gt; matching series &gt; default &gt; newest.</summary>
    Task<BlenderVersion?> ResolveVersionForFileAsync(string blendFilePath, int? pinnedVersionId = null, CancellationToken ct = default);
}
