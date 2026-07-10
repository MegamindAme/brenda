namespace Brenda.App.ViewModels;

/// <summary>Placeholder page for roadmap v1.1/v2.0 modules.</summary>
public sealed class ComingSoonViewModel : PageViewModel
{
    public ComingSoonViewModel(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; }

    public string Description { get; }
}
