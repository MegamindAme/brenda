using Brenda.Core.Models;

namespace Brenda.Core.Abstractions;

/// <summary>Downloads and installs Blender builds into Brenda's managed versions folder.</summary>
public interface IBlenderInstaller
{
    /// <summary>True when the current OS/archive format is supported for automatic install.</summary>
    bool CanInstall(BlenderRelease release);

    Task<BlenderVersion> DownloadAndInstallAsync(
        BlenderRelease release,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default);
}
