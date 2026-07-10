namespace Brenda.Core.Abstractions;

/// <summary>Well-known directories used by Brenda for its own data.</summary>
public interface IAppPaths
{
    /// <summary>Root data directory, e.g. %APPDATA%/Brenda.</summary>
    string DataDirectory { get; }

    string DatabasePath { get; }

    /// <summary>Where managed Blender installations are extracted.</summary>
    string VersionsDirectory { get; }

    /// <summary>Where user-defined project template JSON files live.</summary>
    string TemplatesDirectory { get; }

    string LogsDirectory { get; }
}
