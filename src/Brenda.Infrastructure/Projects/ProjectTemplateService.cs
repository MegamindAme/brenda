using System.Text.Json;
using Brenda.Core.Abstractions;
using Brenda.Core.Models;
using Microsoft.Extensions.Logging;

namespace Brenda.Infrastructure.Projects;

/// <summary>
/// Supplies built-in templates and loads user templates from JSON files in the
/// templates directory. User template format:
/// { "name": "...", "description": "...", "folders": ["scenes", "assets/textures"] }
/// </summary>
public sealed class ProjectTemplateService : IProjectTemplateService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly ProjectTemplate[] BuiltInTemplates =
    [
        new()
        {
            Name = "Empty",
            Description = "A bare project folder with no predefined structure.",
            Folders = Array.Empty<string>(),
            IsBuiltIn = true
        },
        new()
        {
            Name = "Standard",
            Description = "A general-purpose layout for most Blender projects.",
            Folders = ["scenes", "assets", "textures", "renders", "references", "exports"],
            IsBuiltIn = true
        },
        new()
        {
            Name = "Animation",
            Description = "A layout for animation work: shots, audio, storyboards and editorial.",
            Folders = ["scenes", "assets", "textures", "renders", "references", "storyboards", "audio", "edit", "exports"],
            IsBuiltIn = true
        },
        new()
        {
            Name = "Asset Creation",
            Description = "A layout focused on producing reusable assets.",
            Folders = ["work", "textures", "references", "exports", "previews"],
            IsBuiltIn = true
        }
    ];

    private readonly IAppPaths _paths;
    private readonly ILogger<ProjectTemplateService> _logger;

    public ProjectTemplateService(IAppPaths paths, ILogger<ProjectTemplateService> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProjectTemplate>> GetTemplatesAsync(CancellationToken ct = default)
    {
        var templates = new List<ProjectTemplate>(BuiltInTemplates);

        if (Directory.Exists(_paths.TemplatesDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(_paths.TemplatesDirectory, "*.json"))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await using var stream = File.OpenRead(file);
                    var template = await JsonSerializer.DeserializeAsync<ProjectTemplate>(stream, SerializerOptions, ct);
                    if (template is not null && !string.IsNullOrWhiteSpace(template.Name))
                    {
                        template.IsBuiltIn = false;
                        templates.Add(template);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Skipping invalid template file {File}", file);
                }
            }
        }

        return templates;
    }
}
