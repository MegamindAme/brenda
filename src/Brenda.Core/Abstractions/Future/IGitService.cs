namespace Brenda.Core.Abstractions.Future;

/// <summary>
/// Git &amp; GitHub integration for project folders (roadmap v1.1). Interface reserved so the
/// collaboration module can be added without restructuring; the current implementation is a stub.
/// </summary>
public interface IGitService
{
    /// <summary>True once a real implementation is available.</summary>
    bool IsAvailable { get; }
}
