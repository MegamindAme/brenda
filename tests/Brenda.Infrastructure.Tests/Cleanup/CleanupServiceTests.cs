using Brenda.Core.Models;
using Brenda.Infrastructure.Cleanup;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Brenda.Infrastructure.Tests.Cleanup;

public class CleanupServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly CleanupService _service = new(NullLogger<CleanupService>.Instance);

    public CleanupServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "brenda-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Theory]
    [InlineData("scene.blend1", CleanupKind.BlendBackup)]
    [InlineData("scene.blend2", CleanupKind.BlendBackup)]
    [InlineData("SCENE.BLEND1", CleanupKind.BlendBackup)]
    [InlineData("quit.blend", CleanupKind.TempFile)]
    public void Classify_detects_leftover_files(string fileName, CleanupKind expected)
    {
        CleanupService.Classify(fileName).Should().Be(expected);
    }

    [Theory]
    [InlineData("scene.blend")]
    [InlineData("notes.txt")]
    [InlineData("texture.png")]
    public void Classify_keeps_regular_files(string fileName)
    {
        CleanupService.Classify(fileName).Should().BeNull();
    }

    [Fact]
    public async Task Scan_finds_backups_recursively_and_delete_reclaims_space()
    {
        var subDir = Path.Combine(_tempDir, "scenes");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "main.blend"), "keep me");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "main.blend1"), "backup one");
        await File.WriteAllTextAsync(Path.Combine(subDir, "shot.blend2"), "backup two");

        var items = await _service.ScanAsync([_tempDir]);

        items.Should().HaveCount(2);
        items.Should().OnlyContain(i => i.Kind == CleanupKind.BlendBackup);

        var reclaimed = await _service.DeleteAsync(items);

        reclaimed.Should().Be(items.Sum(i => i.SizeBytes));
        File.Exists(Path.Combine(_tempDir, "main.blend")).Should().BeTrue();
        File.Exists(Path.Combine(_tempDir, "main.blend1")).Should().BeFalse();
        File.Exists(Path.Combine(subDir, "shot.blend2")).Should().BeFalse();
    }

    [Fact]
    public async Task Scan_ignores_missing_roots()
    {
        var items = await _service.ScanAsync([Path.Combine(_tempDir, "does-not-exist")]);

        items.Should().BeEmpty();
    }
}
