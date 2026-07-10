namespace Brenda.Core.Abstractions;

/// <summary>Reads metadata from .blend files without launching Blender.</summary>
public interface IBlendFileInspector
{
    /// <summary>
    /// Returns the "major.minor" Blender series that saved the file (e.g. "4.2"),
    /// or null when the file cannot be parsed.
    /// </summary>
    Task<string?> TryGetSeriesAsync(string blendFilePath, CancellationToken ct = default);
}
