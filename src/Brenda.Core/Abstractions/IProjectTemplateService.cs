using Brenda.Core.Models;

namespace Brenda.Core.Abstractions;

/// <summary>Provides built-in and user-defined project templates.</summary>
public interface IProjectTemplateService
{
    Task<IReadOnlyList<ProjectTemplate>> GetTemplatesAsync(CancellationToken ct = default);
}
