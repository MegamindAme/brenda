using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using Brenda.App.Services;
using Brenda.Core.Abstractions;
using ReactiveUI;

namespace Brenda.App.ViewModels;

public sealed class SettingsViewModel : PageViewModel
{
    private readonly IUpdateService _updateService;
    private string _updateStatus = string.Empty;
    private bool _updateAvailable;

    public SettingsViewModel(IAppPaths paths, IUpdateService updateService)
    {
        _updateService = updateService;
        DataDirectory = paths.DataDirectory;
        VersionsDirectory = paths.VersionsDirectory;
        TemplatesDirectory = paths.TemplatesDirectory;

        AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";

        OpenDataFolderCommand = ReactiveCommand.Create(() => OpenFolder(DataDirectory));
        OpenTemplatesFolderCommand = ReactiveCommand.Create(() => OpenFolder(TemplatesDirectory));
        CheckForUpdatesCommand = ReactiveCommand.CreateFromTask(CheckForUpdatesAsync);
        ApplyUpdateCommand = ReactiveCommand.CreateFromTask(
            _updateService.DownloadAndApplyAsync,
            this.WhenAnyValue(vm => vm.UpdateAvailable));
    }

    public string AppVersion { get; }

    public string DataDirectory { get; }

    public string VersionsDirectory { get; }

    public string TemplatesDirectory { get; }

    public ReactiveCommand<Unit, Unit> OpenDataFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenTemplatesFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> CheckForUpdatesCommand { get; }
    public ReactiveCommand<Unit, Unit> ApplyUpdateCommand { get; }

    public string UpdateStatus
    {
        get => _updateStatus;
        private set => this.RaiseAndSetIfChanged(ref _updateStatus, value);
    }

    public bool UpdateAvailable
    {
        get => _updateAvailable;
        private set => this.RaiseAndSetIfChanged(ref _updateAvailable, value);
    }

    private async Task CheckForUpdatesAsync()
    {
        UpdateStatus = "Checking for updates...";
        var newVersion = await _updateService.CheckForUpdateAsync();
        if (newVersion is null)
        {
            UpdateStatus = "You are up to date (or running a development build).";
            UpdateAvailable = false;
        }
        else
        {
            UpdateStatus = $"Version {newVersion} is available.";
            UpdateAvailable = true;
        }
    }

    private static void OpenFolder(string path)
    {
        if (Directory.Exists(path))
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
    }
}
