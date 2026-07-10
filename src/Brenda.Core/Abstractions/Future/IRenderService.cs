namespace Brenda.Core.Abstractions.Future;

/// <summary>
/// Background/parallel rendering (roadmap v1.1+). Interface reserved so the rendering
/// module can be added without restructuring; the current implementation is a stub.
/// </summary>
public interface IRenderService
{
    /// <summary>True once a real implementation is available.</summary>
    bool IsAvailable { get; }
}
