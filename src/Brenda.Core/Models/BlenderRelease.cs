namespace Brenda.Core.Models;

/// <summary>A Blender build available for download from the official servers.</summary>
public class BlenderRelease
{
    /// <summary>Full version string, e.g. "4.2.1".</summary>
    public string Version { get; set; } = string.Empty;

    public BlenderChannel Channel { get; set; } = BlenderChannel.Stable;

    /// <summary>Direct download URL for the current operating system.</summary>
    public string DownloadUrl { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    /// <summary>"major.minor" series, e.g. "4.2".</summary>
    public string Series
    {
        get
        {
            var parts = Version.Split('.');
            return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : Version;
        }
    }
}
