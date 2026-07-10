using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Brenda.App.ViewModels;

namespace Brenda.App;

/// <summary>Resolves views from view models by naming convention: FooViewModel -> FooView.</summary>
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null)
        {
            return null;
        }

        var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type is not null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "View not found: " + name };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
