using Brenda.Core.Abstractions;

namespace Brenda.Infrastructure;

/// <summary>Default implementation of <see cref="IAppPaths"/> rooted in the user's application-data folder.</summary>
public sealed class AppPaths : IAppPaths
{
    public AppPaths()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Brenda"))
    {
    }

    public AppPaths(string dataDirectory)
    {
        DataDirectory = dataDirectory;
        DatabasePath = Path.Combine(dataDirectory, "brenda.db");
        VersionsDirectory = Path.Combine(dataDirectory, "versions");
        TemplatesDirectory = Path.Combine(dataDirectory, "templates");
        LogsDirectory = Path.Combine(dataDirectory, "logs");

        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(VersionsDirectory);
        Directory.CreateDirectory(TemplatesDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }

    public string DataDirectory { get; }
    public string DatabasePath { get; }
    public string VersionsDirectory { get; }
    public string TemplatesDirectory { get; }
    public string LogsDirectory { get; }
}
