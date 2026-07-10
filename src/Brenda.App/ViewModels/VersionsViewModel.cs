using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Brenda.App.Services;
using Brenda.Core.Abstractions;
using Brenda.Core.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace Brenda.App.ViewModels;

public sealed class VersionsViewModel : PageViewModel
{
    private readonly IBlenderVersionService _versionService;
    private readonly IBlenderReleaseProvider _releaseProvider;
    private readonly IBlenderInstaller _installer;
    private readonly IDialogService _dialogs;
    private readonly ILogger<VersionsViewModel> _logger;

    private bool _isBusy;
    private bool _isInstalling;
    private double _installProgress;
    private string _installStatus = string.Empty;
    private string? _updateBanner;
    private bool _releasesLoaded;

    public VersionsViewModel(
        IBlenderVersionService versionService,
        IBlenderReleaseProvider releaseProvider,
        IBlenderInstaller installer,
        IDialogService dialogs,
        ILogger<VersionsViewModel> logger)
    {
        _versionService = versionService;
        _releaseProvider = releaseProvider;
        _installer = installer;
        _dialogs = dialogs;
        _logger = logger;

        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
        AddExistingCommand = ReactiveCommand.CreateFromTask(AddExistingAsync);
        InstallCommand = ReactiveCommand.CreateFromTask<BlenderRelease>(InstallAsync);
        LaunchCommand = ReactiveCommand.CreateFromTask<BlenderVersion>(LaunchAsync);
        SetDefaultCommand = ReactiveCommand.CreateFromTask<BlenderVersion>(SetDefaultAsync);
        RemoveCommand = ReactiveCommand.CreateFromTask<BlenderVersion>(RemoveAsync);

        HandleErrors(RefreshCommand.ThrownExceptions, "Refreshing versions failed");
        HandleErrors(AddExistingCommand.ThrownExceptions, "Adding Blender failed");
        HandleErrors(InstallCommand.ThrownExceptions, "Installation failed");
        HandleErrors(LaunchCommand.ThrownExceptions, "Launching Blender failed");
        HandleErrors(SetDefaultCommand.ThrownExceptions, "Setting default failed");
        HandleErrors(RemoveCommand.ThrownExceptions, "Removing version failed");
    }

    public ObservableCollection<BlenderVersion> Installed { get; } = [];

    public ObservableCollection<BlenderRelease> Available { get; } = [];

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<Unit, Unit> AddExistingCommand { get; }
    public ReactiveCommand<BlenderRelease, Unit> InstallCommand { get; }
    public ReactiveCommand<BlenderVersion, Unit> LaunchCommand { get; }
    public ReactiveCommand<BlenderVersion, Unit> SetDefaultCommand { get; }
    public ReactiveCommand<BlenderVersion, Unit> RemoveCommand { get; }

    public bool IsBusy
    {
        get => _isBusy;
        private set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public bool IsInstalling
    {
        get => _isInstalling;
        private set => this.RaiseAndSetIfChanged(ref _isInstalling, value);
    }

    public double InstallProgress
    {
        get => _installProgress;
        private set => this.RaiseAndSetIfChanged(ref _installProgress, value);
    }

    public string InstallStatus
    {
        get => _installStatus;
        private set => this.RaiseAndSetIfChanged(ref _installStatus, value);
    }

    /// <summary>Message shown when a newer Blender release than any installed version exists.</summary>
    public string? UpdateBanner
    {
        get => _updateBanner;
        private set => this.RaiseAndSetIfChanged(ref _updateBanner, value);
    }

    public override async Task ActivatedAsync()
    {
        await LoadInstalledAsync();
        if (!_releasesLoaded)
        {
            await RefreshAsync();
        }
    }

    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            await LoadInstalledAsync();

            var releases = await _releaseProvider.GetAvailableReleasesAsync();
            Available.Clear();
            foreach (var release in releases)
            {
                Available.Add(release);
            }

            _releasesLoaded = true;
            UpdateUpdateBanner();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadInstalledAsync()
    {
        var installed = await _versionService.GetInstalledAsync();
        Installed.Clear();
        foreach (var version in installed)
        {
            Installed.Add(version);
        }
    }

    private async Task AddExistingAsync()
    {
        var extensions = OperatingSystem.IsWindows() ? new[] { "exe" } : null;
        var path = await _dialogs.PickFileAsync("Select the Blender executable", extensions);
        if (path is null)
        {
            return;
        }

        await _versionService.AddExistingAsync(path);
        await LoadInstalledAsync();
    }

    private async Task InstallAsync(BlenderRelease release)
    {
        if (!_installer.CanInstall(release))
        {
            await _dialogs.ShowErrorAsync(
                "Not supported yet",
                $"Automatic installation of '{release.FileName}' is not supported on this platform yet. " +
                "Install Blender manually and use 'Add Existing'.");
            return;
        }

        IsInstalling = true;
        InstallStatus = $"Installing Blender {release.Version}...";
        try
        {
            var progress = new Progress<DownloadProgress>(p =>
            {
                InstallProgress = p.Percentage ?? 0;
                InstallStatus = p.Percentage is { } pct
                    ? $"Blender {release.Version}: {p.Stage} {pct:F0}%"
                    : $"Blender {release.Version}: {p.Stage}";
            });

            await _installer.DownloadAndInstallAsync(release, progress);
            await LoadInstalledAsync();
            UpdateUpdateBanner();
        }
        finally
        {
            IsInstalling = false;
            InstallProgress = 0;
            InstallStatus = string.Empty;
        }
    }

    private Task LaunchAsync(BlenderVersion version) => _versionService.LaunchAsync(version);

    private async Task SetDefaultAsync(BlenderVersion version)
    {
        await _versionService.SetDefaultAsync(version.Id);
        await LoadInstalledAsync();
    }

    private async Task RemoveAsync(BlenderVersion version)
    {
        var message = version.IsManaged
            ? $"Remove Blender {version.Version} and delete its files from disk?"
            : $"Remove Blender {version.Version} from Brenda? (Files on disk are kept.)";

        if (!await _dialogs.ConfirmAsync("Remove version", message))
        {
            return;
        }

        await _versionService.RemoveAsync(version.Id, deleteFiles: version.IsManaged);
        await LoadInstalledAsync();
    }

    private void UpdateUpdateBanner()
    {
        var newestAvailable = Available.FirstOrDefault();
        if (newestAvailable is null || Installed.Count == 0)
        {
            UpdateBanner = null;
            return;
        }

        var newestInstalled = Installed
            .Select(v => Version.TryParse(v.Version, out var parsed) ? parsed : new Version(0, 0))
            .Max();

        UpdateBanner = Version.TryParse(newestAvailable.Version, out var available) && available > newestInstalled
            ? $"Blender {newestAvailable.Version} is available."
            : null;
    }

    private void HandleErrors(IObservable<Exception> exceptions, string title)
    {
        exceptions
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ex =>
            {
                _logger.LogError(ex, "{Title}", title);
                _ = _dialogs.ShowErrorAsync(title, ex.Message);
            });
    }
}
