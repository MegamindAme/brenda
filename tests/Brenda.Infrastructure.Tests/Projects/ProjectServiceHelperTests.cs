using Brenda.Infrastructure.Projects;
using FluentAssertions;
using Xunit;

namespace Brenda.Infrastructure.Tests.Projects;

public class ProjectServiceHelperTests : IDisposable
{
    private readonly string _tempDir;

    public ProjectServiceHelperTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "brenda-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Theory]
    [InlineData("My Project", "My Project")]
    [InlineData("bad/name", "bad_name")]
    [InlineData("q:*uote", "q__uote")]
    [InlineData("  trimmed  ", "trimmed")]
    public void SanitizeFolderName_removes_invalid_characters(string input, string expected)
    {
        ProjectService.SanitizeFolderName(input).Should().Be(expected);
    }

    [Fact]
    public void FindMainBlendFile_prefers_shallowest_file()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "scenes"));
        File.WriteAllText(Path.Combine(_tempDir, "scenes", "deep.blend"), "");
        File.WriteAllText(Path.Combine(_tempDir, "main.blend"), "");

        ProjectService.FindMainBlendFile(_tempDir).Should().Be("main.blend");
    }

    [Fact]
    public void FindMainBlendFile_returns_null_when_no_blend_exists()
    {
        ProjectService.FindMainBlendFile(_tempDir).Should().BeNull();
    }
}
