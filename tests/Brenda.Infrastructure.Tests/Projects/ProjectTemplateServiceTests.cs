using Brenda.Core.Abstractions;
using Brenda.Infrastructure.Projects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Brenda.Infrastructure.Tests.Projects;

public class ProjectTemplateServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IAppPaths _paths;
    private readonly ProjectTemplateService _service;

    public ProjectTemplateServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "brenda-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _paths = Substitute.For<IAppPaths>();
        _paths.TemplatesDirectory.Returns(_tempDir);

        _service = new ProjectTemplateService(_paths, NullLogger<ProjectTemplateService>.Instance);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task Returns_built_in_templates()
    {
        var templates = await _service.GetTemplatesAsync();

        templates.Should().Contain(t => t.Name == "Empty" && t.IsBuiltIn);
        templates.Should().Contain(t => t.Name == "Standard" && t.IsBuiltIn);
        templates.Should().Contain(t => t.Name == "Animation" && t.IsBuiltIn);
    }

    [Fact]
    public async Task Loads_user_templates_from_json_files()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_tempDir, "custom.json"),
            """{ "name": "My Custom", "description": "Test", "folders": ["a", "b/c"] }""");

        var templates = await _service.GetTemplatesAsync();

        var custom = templates.Single(t => t.Name == "My Custom");
        custom.IsBuiltIn.Should().BeFalse();
        custom.Folders.Should().BeEquivalentTo(["a", "b/c"]);
    }

    [Fact]
    public async Task Skips_invalid_template_files()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "broken.json"), "{ nope");

        var act = () => _service.GetTemplatesAsync();

        (await act()).Should().NotBeEmpty();
    }
}
