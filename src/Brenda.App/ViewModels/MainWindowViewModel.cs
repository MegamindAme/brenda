using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace Brenda.App.ViewModels;

public sealed class NavigationItem
{
    public NavigationItem(string title, string glyph, Func<PageViewModel> factory, bool isEnabled = true)
    {
        Title = title;
        Glyph = glyph;
        Factory = factory;
        IsEnabled = isEnabled;
    }

    public string Title { get; }

    /// <summary>Simple emoji glyph shown in the sidebar.</summary>
    public string Glyph { get; }

    public Func<PageViewModel> Factory { get; }

    public bool IsEnabled { get; }

    private PageViewModel? _page;

    public PageViewModel GetPage() => _page ??= Factory();
}

public sealed class MainWindowViewModel : ViewModelBase
{
    private NavigationItem? _selectedItem;
    private PageViewModel? _currentPage;

    public MainWindowViewModel(IServiceProvider services)
    {
        NavigationItems =
        [
            new NavigationItem("Projects", "\uD83D\uDCC2", services.GetRequiredService<ProjectsViewModel>),
            new NavigationItem("Versions", "\uD83D\uDD04", services.GetRequiredService<VersionsViewModel>),
            new NavigationItem("Cleanup", "\uD83E\uDDF9", services.GetRequiredService<CleanupViewModel>),
            new NavigationItem("Rendering", "\uD83C\uDFAC", () => new ComingSoonViewModel(
                "Rendering & Automation",
                "Background rendering, parallel rendering and a render farm manager are planned for v1.1 and v2.0.")),
            new NavigationItem("Git", "\uD83D\uDD00", () => new ComingSoonViewModel(
                "Collaboration",
                "Git & GitHub integration for entire project folders is planned for v1.1.")),
            new NavigationItem("Assets", "\uD83D\uDDC3\uFE0F", () => new ComingSoonViewModel(
                "Asset Management",
                "A centralized asset library with global search and previews is planned for v1.1.")),
            new NavigationItem("Settings", "\u2699\uFE0F", services.GetRequiredService<SettingsViewModel>)
        ];

        SelectedItem = NavigationItems[0];
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public NavigationItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedItem, value);
            if (value is not null)
            {
                var page = value.GetPage();
                CurrentPage = page;
                _ = page.ActivatedAsync();
            }
        }
    }

    public PageViewModel? CurrentPage
    {
        get => _currentPage;
        private set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }
}
