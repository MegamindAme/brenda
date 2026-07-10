namespace Brenda.Core.Models;

/// <summary>A Blender installation registered with (or managed by) Brenda.</summary>
public class BlenderVersion
{
    public int Id { get; set; }

    /// <summary>Full version string, e.g. "4.2.1".</summary>
    public string Version { get; set; } = string.Empty;

    public BlenderChannel Channel { get; set; } = BlenderChannel.Custom;

    /// <summary>Absolute path to the Blender executable.</summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>Root folder of the installation.</summary>
    public string InstallPath { get; set; } = string.Empty;

    /// <summary>True when Brenda downloaded and owns this installation.</summary>
    public bool IsManaged { get; set; }

    /// <summary>True when this is the default version for opening .blend files.</summary>
    public bool IsDefault { get; set; }

    public DateTime AddedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>"major.minor" series, e.g. "4.2". Used to match .blend files to versions.</summary>
    public string Series
    {
        get
        {
            var parts = Version.Split('.');
            return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : Version;
        }
    }
}
