namespace Brenda.App.Services;

/// <summary>Application self-update via Velopack + GitHub Releases.</summary>
public interface IUpdateService
{
    /// <summary>Returns the new version string when an update is available, otherwise null.
    /// Returns null too when the app is not running from a Velopack install (e.g. during development).</summary>
    Task<string?> CheckForUpdateAsync(CancellationToken ct = default);

    /// <summary>Downloads and applies the update, then restarts the app.</summary>
    Task DownloadAndApplyAsync(CancellationToken ct = default);
}
