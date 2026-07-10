using Brenda.Core.Abstractions;
using Brenda.Core.Models;
using Brenda.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Brenda.Infrastructure.Projects;

/// <summary>EF Core-backed project management. Every project stays self-contained:
/// metadata is mirrored into a brenda.json manifest inside the project folder.</summary>
public sealed class ProjectService : IProjectService
{
    private readonly IDbContextFactory<BrendaDbContext> _dbFactory;
    private readonly IBlenderVersionService _versionService;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        IDbContextFactory<BrendaDbContext> dbFactory,
        IBlenderVersionService versionService,
        ILogger<ProjectService> logger)
    {
        _dbFactory = dbFactory;
        _versionService = versionService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Projects
            .AsNoTracking()
            .Include(p => p.PinnedBlenderVersion)
            .OrderByDescending(p => p.LastOpenedUtc ?? p.CreatedUtc)
            .ToListAsync(ct);
    }

    public async Task<Project> CreateAsync(
        string name,
        string parentDirectory,
        ProjectTemplate template,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name is required.", nameof(name));
        }

        var folderPath = Path.Combine(parentDirectory, SanitizeFolderName(name));
        if (Directory.Exists(folderPath) && Directory.EnumerateFileSystemEntries(folderPath).Any())
        {
            throw new InvalidOperationException($"Folder '{folderPath}' already exists and is not empty.");
        }

        Directory.CreateDirectory(folderPath);
        foreach (var relativeFolder in template.Folders)
        {
            Directory.CreateDirectory(Path.Combine(folderPath, relativeFolder));
        }

        var project = new Project
        {
            Name = name.Trim(),
            FolderPath = folderPath,
            TemplateName = template.Name
        };

        await ProjectManifest.FromProject(project, pinnedVersionString: null).SaveAsync(folderPath, ct);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Created project '{Name}' at {Path}", project.Name, folderPath);
        return project;
    }

    public async Task<Project> ImportAsync(string folderPath, CancellationToken ct = default)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder '{folderPath}' does not exist.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var existing = await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.FolderPath == folderPath, ct);
        if (existing is not null)
        {
            throw new InvalidOperationException($"'{folderPath}' is already registered as project '{existing.Name}'.");
        }

        var manifest = await ProjectManifest.LoadAsync(folderPath, ct);
        var project = new Project
        {
            Name = manifest?.Name is { Length: > 0 } manifestName ? manifestName : Path.GetFileName(folderPath),
            FolderPath = folderPath,
            MainBlendFile = manifest?.MainBlendFile ?? FindMainBlendFile(folderPath),
            IconPath = manifest?.IconPath,
            TemplateName = manifest?.TemplateName
        };

        if (manifest is null)
        {
            await ProjectManifest.FromProject(project, pinnedVersionString: null).SaveAsync(folderPath, ct);
        }

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Imported project '{Name}' from {Path}", project.Name, folderPath);
        return project;
    }

    public async Task OpenAsync(Project project, CancellationToken ct = default)
    {
        var blendPath = project.MainBlendFile is { Length: > 0 }
            ? Path.Combine(project.FolderPath, project.MainBlendFile)
            : null;

        BlenderVersion? version;
        if (blendPath is not null && File.Exists(blendPath))
        {
            version = await _versionService.ResolveVersionForFileAsync(blendPath, project.PinnedBlenderVersionId, ct);
        }
        else
        {
            blendPath = null;
            var installed = await _versionService.GetInstalledAsync(ct);
            version = installed.FirstOrDefault(v => v.Id == project.PinnedBlenderVersionId)
                ?? installed.FirstOrDefault(v => v.IsDefault)
                ?? installed.FirstOrDefault();
        }

        if (version is null)
        {
            throw new InvalidOperationException("No Blender versions are installed. Add one on the Versions page first.");
        }

        await _versionService.LaunchAsync(version, blendPath, ct);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var tracked = await db.Projects.FindAsync([project.Id], ct);
        if (tracked is not null)
        {
            tracked.LastOpenedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateAsync(Project project, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var tracked = await db.Projects.FindAsync([project.Id], ct)
            ?? throw new InvalidOperationException($"Project {project.Id} not found.");

        tracked.Name = project.Name;
        tracked.MainBlendFile = project.MainBlendFile;
        tracked.IconPath = project.IconPath;
        tracked.PinnedBlenderVersionId = project.PinnedBlenderVersionId;
        await db.SaveChangesAsync(ct);

        var pinnedVersionString = project.PinnedBlenderVersionId is null
            ? null
            : (await _versionService.GetInstalledAsync(ct)).FirstOrDefault(v => v.Id == project.PinnedBlenderVersionId)?.Version;

        if (Directory.Exists(project.FolderPath))
        {
            await ProjectManifest.FromProject(project, pinnedVersionString).SaveAsync(project.FolderPath, ct);
        }
    }

    public async Task RemoveAsync(int projectId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var project = await db.Projects.FindAsync([projectId], ct);
        if (project is null)
        {
            return;
        }

        db.Projects.Remove(project);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Removed project '{Name}' from Brenda (files kept on disk)", project.Name);
    }

    internal static string? FindMainBlendFile(string folderPath)
    {
        var blend = Directory
            .EnumerateFiles(folderPath, "*.blend", SearchOption.AllDirectories)
            .OrderBy(f => f.Count(c => c is '\\' or '/'))
            .ThenBy(f => f, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return blend is null ? null : Path.GetRelativePath(folderPath, blend);
    }

    private static readonly HashSet<char> InvalidFolderNameChars = new(
        Path.GetInvalidFileNameChars().Concat(new[] { ':', '*', '?', '"', '<', '>', '|', '\\' }));

    internal static string SanitizeFolderName(string name)
    {
        var sanitized = new string(name.Trim().Select(c => InvalidFolderNameChars.Contains(c) ? '_' : c).ToArray());
        return sanitized.Length == 0 ? "Project" : sanitized;
    }
}
