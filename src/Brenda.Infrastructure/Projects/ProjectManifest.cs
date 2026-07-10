using System.Text.Json;
using System.Text.Json.Serialization;
using Brenda.Core.Models;

namespace Brenda.Infrastructure.Projects;

/// <summary>
/// The "brenda.json" file stored inside every project folder, keeping projects
/// self-contained and portable between machines.
/// </summary>
public sealed class ProjectManifest
{
    public const string FileName = "brenda.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Name { get; set; } = string.Empty;

    public string? MainBlendFile { get; set; }

    public string? IconPath { get; set; }

    public string? TemplateName { get; set; }

    /// <summary>Pinned Blender version string (e.g. "4.2.1"); resolved to a local install on import.</summary>
    public string? PinnedBlenderVersion { get; set; }

    public static ProjectManifest FromProject(Project project, string? pinnedVersionString) => new()
    {
        Name = project.Name,
        MainBlendFile = project.MainBlendFile,
        IconPath = project.IconPath,
        TemplateName = project.TemplateName,
        PinnedBlenderVersion = pinnedVersionString
    };

    public static async Task<ProjectManifest?> LoadAsync(string projectFolder, CancellationToken ct = default)
    {
        var path = Path.Combine(projectFolder, FileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<ProjectManifest>(stream, SerializerOptions, ct);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task SaveAsync(string projectFolder, CancellationToken ct = default)
    {
        var path = Path.Combine(projectFolder, FileName);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, this, SerializerOptions, ct);
    }
}
