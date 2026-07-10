using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using Brenda.App.Services;
using Brenda.Core.Abstractions;
using Brenda.Core.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace Brenda.App.ViewModels;

public sealed class ProjectsViewModel : PageViewModel
{
    private readonly IProjectService _projectService;
    private readonly IProjectTemplateService _templateService;
    private readonly IDialogService _dialogs;
    private readonly ILogger<ProjectsViewModel> _logger;

    private bool _isCreatePanelOpen;
    private string _newProjectName = string.Empty;
    private string? _newProjectLocation;
    private ProjectTemplate? _selectedTemplate;

    public ProjectsViewModel(
        IProjectService projectService,
        IProjectTemplateService templateService,
        IDialogService dialogs,
        ILogger<ProjectsViewModel> logger)
    {
        _projectService = projectService;
        _templateService = templateService;
        _dialogs = dialogs;
        _logger = logger;

        ToggleCreatePanelCommand = ReactiveCommand.Create(() => { IsCreatePanelOpen = !IsCreatePanelOpen; });
        PickLocationCommand = ReactiveCommand.CreateFromTask(PickLocationAsync);

        var canCreate = this.WhenAnyValue(
            vm => vm.NewProjectName,
            vm => vm.NewProjectLocation,
            vm => vm.SelectedTemplate,
            (name, location, template) =>
                !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(location) && template is not null);
        CreateCommand = ReactiveCommand.CreateFromTask(CreateAsync, canCreate);

        ImportCommand = ReactiveCommand.CreateFromTask(ImportAsync);
        OpenCommand = ReactiveCommand.CreateFromTask<Project>(OpenAsync);
        OpenFolderCommand = ReactiveCommand.Create<Project>(OpenFolder);
        RemoveCommand = ReactiveCommand.CreateFromTask<Project>(RemoveAsync);

        HandleErrors(CreateCommand.ThrownExceptions, "Creating project failed");
        HandleErrors(ImportCommand.ThrownExceptions, "Importing project failed");
        HandleErrors(OpenCommand.ThrownExceptions, "Opening project failed");
        HandleErrors(RemoveCommand.ThrownExceptions, "Removing project failed");
    }

    public ObservableCollection<Project> Projects { get; } = [];

    public ObservableCollection<ProjectTemplate> Templates { get; } = [];

    public ReactiveCommand<Unit, Unit> ToggleCreatePanelCommand { get; }
    public ReactiveCommand<Unit, Unit> PickLocationCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportCommand { get; }
    public ReactiveCommand<Project, Unit> OpenCommand { get; }
    public ReactiveCommand<Project, Unit> OpenFolderCommand { get; }
    public ReactiveCommand<Project, Unit> RemoveCommand { get; }

    public bool IsCreatePanelOpen
    {
        get => _isCreatePanelOpen;
        set => this.RaiseAndSetIfChanged(ref _isCreatePanelOpen, value);
    }

    public string NewProjectName
    {
        get => _newProjectName;
        set => this.RaiseAndSetIfChanged(ref _newProjectName, value);
    }

    public string? NewProjectLocation
    {
        get => _newProjectLocation;
        set => this.RaiseAndSetIfChanged(ref _newProjectLocation, value);
    }

    public ProjectTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set => this.RaiseAndSetIfChanged(ref _selectedTemplate, value);
    }

    public override async Task ActivatedAsync()
    {
        await LoadProjectsAsync();

        if (Templates.Count == 0)
        {
            var templates = await _templateService.GetTemplatesAsync();
            foreach (var template in templates)
            {
                Templates.Add(template);
            }

            SelectedTemplate = Templates.FirstOrDefault();
        }
    }

    private async Task LoadProjectsAsync()
    {
        var projects = await _projectService.GetAllAsync();
        Projects.Clear();
        foreach (var project in projects)
        {
            Projects.Add(project);
        }
    }

    private async Task PickLocationAsync()
    {
        var folder = await _dialogs.PickFolderAsync("Choose where the project folder will be created");
        if (folder is not null)
        {
            NewProjectLocation = folder;
        }
    }

    private async Task CreateAsync()
    {
        await _projectService.CreateAsync(NewProjectName, NewProjectLocation!, SelectedTemplate!);

        NewProjectName = string.Empty;
        IsCreatePanelOpen = false;
        await LoadProjectsAsync();
    }

    private async Task ImportAsync()
    {
        var folder = await _dialogs.PickFolderAsync("Select an existing project folder");
        if (folder is null)
        {
            return;
        }

        await _projectService.ImportAsync(folder);
        await LoadProjectsAsync();
    }

    private async Task OpenAsync(Project project)
    {
        await _projectService.OpenAsync(project);
        await LoadProjectsAsync();
    }

    private void OpenFolder(Project project)
    {
        if (Directory.Exists(project.FolderPath))
        {
            Process.Start(new ProcessStartInfo(project.FolderPath) { UseShellExecute = true });
        }
    }

    private async Task RemoveAsync(Project project)
    {
        var confirmed = await _dialogs.ConfirmAsync(
            "Remove project",
            $"Remove '{project.Name}' from Brenda? The folder and all files stay on disk.");
        if (!confirmed)
        {
            return;
        }

        await _projectService.RemoveAsync(project.Id);
        await LoadProjectsAsync();
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
