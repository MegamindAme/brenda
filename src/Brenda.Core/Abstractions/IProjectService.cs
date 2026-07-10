using Brenda.Core.Models;

namespace Brenda.Core.Abstractions;

/// <summary>Manages Brenda projects.</summary>
public interface IProjectService
{
    Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Creates a new self-contained project folder from a template.</summary>
    Task<Project> CreateAsync(string name, string parentDirectory, ProjectTemplate template, CancellationToken ct = default);

    /// <summary>Imports an existing folder as a Brenda project.</summary>
    Task<Project> ImportAsync(string folderPath, CancellationToken ct = default);

    /// <summary>Opens the project's main .blend file (or Blender in the project folder) and updates LastOpened.</summary>
    Task OpenAsync(Project project, CancellationToken ct = default);

    Task UpdateAsync(Project project, CancellationToken ct = default);

    /// <summary>Removes the project from Brenda. Never deletes files on disk.</summary>
    Task RemoveAsync(int projectId, CancellationToken ct = default);
}
