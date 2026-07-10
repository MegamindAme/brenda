namespace Brenda.Core.Abstractions.Future;

/// <summary>
/// Centralized asset library management (roadmap v1.1). Interface reserved so the asset
/// module can be added without restructuring; the current implementation is a stub.
/// </summary>
public interface IAssetLibraryService
{
    /// <summary>True once a real implementation is available.</summary>
    bool IsAvailable { get; }
}
