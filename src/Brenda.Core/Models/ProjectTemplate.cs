namespace Brenda.Core.Models;

/// <summary>A folder-structure template used when creating new projects.</summary>
public class ProjectTemplate
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Folder paths (relative to the project root) to create.</summary>
    public IReadOnlyList<string> Folders { get; set; } = Array.Empty<string>();

    /// <summary>True for templates shipped with Brenda; false for user-defined ones.</summary>
    public bool IsBuiltIn { get; set; }
}
