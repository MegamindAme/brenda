namespace Brenda.Core.Models;

public enum CleanupKind
{
    /// <summary>Blender backup files: .blend1, .blend2, ...</summary>
    BlendBackup,

    /// <summary>Temporary files such as autosaves and quit.blend.</summary>
    TempFile
}

/// <summary>A file found by the cleanup scanner that can be safely removed.</summary>
public class CleanupItem
{
    public string Path { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public CleanupKind Kind { get; set; }

    public DateTime LastModifiedUtc { get; set; }
}
