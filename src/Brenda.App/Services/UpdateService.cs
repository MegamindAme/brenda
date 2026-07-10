using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace Brenda.App.Services;

public sealed class UpdateService : IUpdateService
{
    private const string RepositoryUrl = "https://github.com/MegamindAme/brenda";

    private readonly ILogger<UpdateService> _logger;
    private UpdateInfo? _pendingUpdate;

    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
    }

    public async Task<string?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        try
        {
            var manager = new UpdateManager(new GithubSource(RepositoryUrl, null, false));
            if (!manager.IsInstalled)
            {
                _logger.LogInformation("Not a Velopack install; skipping update check");
                return null;
            }

            _pendingUpdate = await manager.CheckForUpdatesAsync();
            return _pendingUpdate?.TargetFullRelease.Version.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed");
            return null;
        }
    }

    public async Task DownloadAndApplyAsync(CancellationToken ct = default)
    {
        if (_pendingUpdate is null)
        {
            return;
        }

        var manager = new UpdateManager(new GithubSource(RepositoryUrl, null, false));
        await manager.DownloadUpdatesAsync(_pendingUpdate, cancelToken: ct);
        manager.ApplyUpdatesAndRestart(_pendingUpdate);
    }
}
