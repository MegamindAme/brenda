namespace Brenda.App.Services;

/// <summary>UI dialogs used by view models (kept behind an interface for testability).</summary>
public interface IDialogService
{
    Task<string?> PickFolderAsync(string title);

    Task<string?> PickFileAsync(string title, IReadOnlyList<string>? extensions = null);

    Task<bool> ConfirmAsync(string title, string message);

    Task ShowErrorAsync(string title, string message);
}
