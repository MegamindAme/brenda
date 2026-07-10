using Brenda.Core.Models;
using Brenda.Infrastructure.Projects;
using FluentAssertions;
using Xunit;

namespace Brenda.Infrastructure.Tests.Projects;

public class ProjectManifestTests : IDisposable
{
    private readonly string _tempDir;

    public ProjectManifestTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "brenda-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task Save_and_load_roundtrip()
    {
        var project = new Project
        {
            Name = "Test Project",
            MainBlendFile = "scenes/main.blend",
            TemplateName = "Standard"
        };

        await ProjectManifest.FromProject(project, "4.2.1").SaveAsync(_tempDir);
        var loaded = await ProjectManifest.LoadAsync(_tempDir);

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Test Project");
        loaded.MainBlendFile.Should().Be("scenes/main.blend");
        loaded.TemplateName.Should().Be("Standard");
        loaded.PinnedBlenderVersion.Should().Be("4.2.1");
    }

    [Fact]
    public async Task Load_returns_null_when_manifest_missing()
    {
        (await ProjectManifest.LoadAsync(_tempDir)).Should().BeNull();
    }

    [Fact]
    public async Task Load_returns_null_for_corrupt_manifest()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, ProjectManifest.FileName), "{ not valid json !");

        (await ProjectManifest.LoadAsync(_tempDir)).Should().BeNull();
    }
}
