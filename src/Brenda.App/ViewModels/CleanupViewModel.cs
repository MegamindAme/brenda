using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Brenda.App.Services;
using Brenda.Core.Abstractions;
using Brenda.Core.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace Brenda.App.ViewModels;

public sealed class CleanupItemViewModel : ViewModelBase
{
    private bool _isSelected = true;

    public CleanupItemViewModel(CleanupItem item)
    {
        Item = item;
    }

    public CleanupItem Item { get; }

    public string Path => Item.Path;

    public string KindText => Item.Kind == CleanupKind.BlendBackup ? "Backup" : "Temp";

    public string SizeText => FormatSize(Item.SizeBytes);

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    internal static string FormatSize(long bytes) => bytes switch
    {
        >= 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F2} GB",
        >= 1024L * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        >= 1024L => $"{bytes / 1024.0:F0} KB",
        _ => $"{bytes} B"
    };
}

public sealed class CleanupViewModel : PageViewModel
{
    private readonly ICleanupService _cleanupService;
    private readonly IProjectService _projectService;
    private readonly IDialogService _dialogs;
    private readonly ILogger<CleanupViewModel> _logger;

    private bool _isScanning;
    private string _statusText = "Scan your project folders for Blender leftovers (.blend1 backups, temp files).";
    private string? _customFolder;

    public CleanupViewModel(
        ICleanupService cleanupService,
        IProjectService projectService,
        IDialogService dialogs,
        ILogger<CleanupViewModel> logger)
    {
        _cleanupService = cleanupService;
        _projectService = projectService;
        _dialogs = dialogs;
        _logger = logger;

        ScanProjectsCommand = ReactiveCommand.CreateFromTask(ScanProjectsAsync);
        ScanFolderCommand = ReactiveCommand.CreateFromTask(ScanFolderAsync);
        SelectAllCommand = ReactiveCommand.Create(() => SetAllSelected(true));
        SelectNoneCommand = ReactiveCommand.Create(() => SetAllSelected(false));
        DeleteSelectedCommand = ReactiveCommand.CreateFromTask(DeleteSelectedAsync);

        HandleErrors(ScanProjectsCommand.ThrownExceptions, "Scan failed");
        HandleErrors(ScanFolderCommand.ThrownExceptions, "Scan failed");
        HandleErrors(DeleteSelectedCommand.ThrownExceptions, "Cleanup failed");
    }

    public ObservableCollection<CleanupItemViewModel> Items { get; } = [];

    public ReactiveCommand<Unit, Unit> ScanProjectsCommand { get; }
    public ReactiveCommand<Unit, Unit> ScanFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectAllCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectNoneCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteSelectedCommand { get; }

    public bool IsScanning
    {
        get => _isScanning;
        private set => this.RaiseAndSetIfChanged(ref _isScanning, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    private async Task ScanProjectsAsync()
    {
        var projects = await _projectService.GetAllAsync();
        if (projects.Count == 0)
        {
            StatusText = "No projects registered yet. Use 'Scan Folder...' to scan any directory.";
            return;
        }

        await RunScanAsync(projects.Select(p => p.FolderPath));
    }

    private async Task ScanFolderAsync()
    {
        _customFolder = await _dialogs.PickFolderAsync("Select a folder to scan");
        if (_customFolder is null)
        {
            return;
        }

        await RunScanAsync([_customFolder]);
    }

    private async Task RunScanAsync(IEnumerable<string> roots)
    {
        IsScanning = true;
        try
        {
            var progress = new Progress<string>(root => StatusText = $"Scanning {root}...");
            var found = await _cleanupService.ScanAsync(roots, progress);

            Items.Clear();
            foreach (var item in found.OrderByDescending(i => i.SizeBytes))
            {
                Items.Add(new CleanupItemViewModel(item));
            }

            var totalSize = CleanupItemViewModel.FormatSize(found.Sum(i => i.SizeBytes));
            StatusText = found.Count == 0
                ? "Nothing to clean. Your folders are tidy!"
                : $"Found {found.Count} file(s) taking up {totalSize}.";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void SetAllSelected(bool selected)
    {
        foreach (var item in Items)
        {
            item.IsSelected = selected;
        }
    }

    private async Task DeleteSelectedAsync()
    {
        var selected = Items.Where(i => i.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        var totalSize = CleanupItemViewModel.FormatSize(selected.Sum(i => i.Item.SizeBytes));
        var confirmed = await _dialogs.ConfirmAsync(
            "Delete files",
            $"Permanently delete {selected.Count} file(s) ({totalSize})? This cannot be undone.");
        if (!confirmed)
        {
            return;
        }

        var reclaimed = await _cleanupService.DeleteAsync(selected.Select(i => i.Item));

        foreach (var item in selected)
        {
            Items.Remove(item);
        }

        StatusText = $"Reclaimed {CleanupItemViewModel.FormatSize(reclaimed)}.";
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
