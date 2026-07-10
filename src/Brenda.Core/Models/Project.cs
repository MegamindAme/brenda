namespace Brenda.Core.Models;

/// <summary>A Blender project managed by Brenda. Self-contained inside <see cref="FolderPath"/>.</summary>
public class Project
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Absolute path to the project root folder.</summary>
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>Path to the main .blend file, relative to <see cref="FolderPath"/>. Optional.</summary>
    public string? MainBlendFile { get; set; }

    /// <summary>Path to an icon/thumbnail image, relative to <see cref="FolderPath"/>. Optional.</summary>
    public string? IconPath { get; set; }

    /// <summary>Optional Blender version pinned to this project.</summary>
    public int? PinnedBlenderVersionId { get; set; }

    public BlenderVersion? PinnedBlenderVersion { get; set; }

    /// <summary>Name of the template used to create this project, if any.</summary>
    public string? TemplateName { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastOpenedUtc { get; set; }
}
