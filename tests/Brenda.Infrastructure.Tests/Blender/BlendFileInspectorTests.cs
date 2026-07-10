using System.IO.Compression;
using System.Text;
using Brenda.Infrastructure.Blender;
using FluentAssertions;
using Xunit;

namespace Brenda.Infrastructure.Tests.Blender;

public class BlendFileInspectorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly BlendFileInspector _inspector = new();

    public BlendFileInspectorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "brenda-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Theory]
    [InlineData("BLENDER-v402", "4.2")]
    [InlineData("BLENDER_v306", "3.6")]
    [InlineData("BLENDER-v293", "2.93")]
    [InlineData("BLENDER-v279", "2.79")]
    public void ParseHeader_extracts_series(string header, string expected)
    {
        BlendFileInspector.ParseHeader(Encoding.ASCII.GetBytes(header)).Should().Be(expected);
    }

    [Theory]
    [InlineData("NOTABLEND999")]
    [InlineData("BLENDER-vXYZ")]
    [InlineData("short")]
    public void ParseHeader_returns_null_for_invalid_input(string header)
    {
        BlendFileInspector.ParseHeader(Encoding.ASCII.GetBytes(header)).Should().BeNull();
    }

    [Fact]
    public async Task Reads_uncompressed_blend_file()
    {
        var path = Path.Combine(_tempDir, "plain.blend");
        await File.WriteAllBytesAsync(path, Encoding.ASCII.GetBytes("BLENDER-v402" + new string('\0', 32)));

        (await _inspector.TryGetSeriesAsync(path)).Should().Be("4.2");
    }

    [Fact]
    public async Task Reads_gzip_compressed_blend_file()
    {
        var path = Path.Combine(_tempDir, "gzip.blend");
        await using (var file = File.Create(path))
        await using (var gzip = new GZipStream(file, CompressionMode.Compress))
        {
            await gzip.WriteAsync(Encoding.ASCII.GetBytes("BLENDER_v293" + new string('\0', 32)));
        }

        (await _inspector.TryGetSeriesAsync(path)).Should().Be("2.93");
    }

    [Fact]
    public async Task Returns_null_for_missing_file()
    {
        (await _inspector.TryGetSeriesAsync(Path.Combine(_tempDir, "nope.blend"))).Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_for_non_blend_content()
    {
        var path = Path.Combine(_tempDir, "junk.blend");
        await File.WriteAllTextAsync(path, "this is not a blend file at all");

        (await _inspector.TryGetSeriesAsync(path)).Should().BeNull();
    }
}
