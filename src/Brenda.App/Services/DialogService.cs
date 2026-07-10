using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace Brenda.App.Services;

public sealed class DialogService : IDialogService
{
    private static Window? MainWindow =>
        (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public async Task<string?> PickFolderAsync(string title)
    {
        var window = MainWindow;
        if (window is null)
        {
            return null;
        }

        var result = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickFileAsync(string title, IReadOnlyList<string>? extensions = null)
    {
        var window = MainWindow;
        if (window is null)
        {
            return null;
        }

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        };

        if (extensions is { Count: > 0 })
        {
            options.FileTypeFilter =
            [
                new FilePickerFileType(title) { Patterns = extensions.Select(e => $"*.{e}").ToList() }
            ];
        }

        var result = await window.StorageProvider.OpenFilePickerAsync(options);
        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var window = MainWindow;
        if (window is null)
        {
            return false;
        }

        var confirmed = false;
        var dialog = CreateDialogWindow(title);

        var yes = new Button { Content = "Yes", MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
        var no = new Button { Content = "No", MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
        yes.Click += (_, _) => { confirmed = true; dialog.Close(); };
        no.Click += (_, _) => dialog.Close();

        dialog.Content = BuildDialogContent(message, yes, no);
        await dialog.ShowDialog(window);
        return confirmed;
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        var window = MainWindow;
        if (window is null)
        {
            return;
        }

        var dialog = CreateDialogWindow(title);
        var ok = new Button { Content = "OK", MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
        ok.Click += (_, _) => dialog.Close();

        dialog.Content = BuildDialogContent(message, ok);
        await dialog.ShowDialog(window);
    }

    private static Window CreateDialogWindow(string title) => new()
    {
        Title = title,
        SizeToContent = SizeToContent.WidthAndHeight,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        CanResize = false,
        MaxWidth = 480,
        SystemDecorations = SystemDecorations.Full
    };

    private static Control BuildDialogContent(string message, params Button[] buttons)
    {
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        foreach (var button in buttons)
        {
            buttonPanel.Children.Add(button);
        }

        return new StackPanel
        {
            Margin = new Avalonia.Thickness(24),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 420 },
                buttonPanel
            }
        };
    }
}
