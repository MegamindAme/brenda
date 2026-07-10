using ReactiveUI;

namespace Brenda.App.ViewModels;

public abstract class ViewModelBase : ReactiveObject
{
}

/// <summary>Base class for top-level pages shown in the navigation shell.</summary>
public abstract class PageViewModel : ViewModelBase
{
    /// <summary>Called by the shell every time the page becomes active.</summary>
    public virtual Task ActivatedAsync() => Task.CompletedTask;
}
