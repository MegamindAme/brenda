using Brenda.Core.Models;

namespace Brenda.Core.Abstractions;

/// <summary>Provides the list of Blender builds available for download.</summary>
public interface IBlenderReleaseProvider
{
    Task<IReadOnlyList<BlenderRelease>> GetAvailableReleasesAsync(CancellationToken ct = default);
}
