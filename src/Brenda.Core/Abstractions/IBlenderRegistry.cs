using Brenda.Core.Models;

namespace Brenda.Core.Abstractions;

/// <summary>Persists Blender installations. Split from <see cref="IBlenderVersionService"/> so
/// the installer can register new versions without depending on the full service.</summary>
public interface IBlenderRegistry
{
    Task AddAsync(BlenderVersion version, CancellationToken ct = default);
}
