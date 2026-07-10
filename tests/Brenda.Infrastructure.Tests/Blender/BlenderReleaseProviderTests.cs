using Brenda.Infrastructure.Blender;
using FluentAssertions;
using Xunit;

namespace Brenda.Infrastructure.Tests.Blender;

public class BlenderReleaseProviderTests
{
    [Fact]
    public void ParseSeriesDirectories_returns_modern_series_only()
    {
        const string html = """
            <a href="Blender1.0/">Blender1.0/</a>
            <a href="Blender2.79/">Blender2.79/</a>
            <a href="Blender2.83/">Blender2.83/</a>
            <a href="Blender3.6/">Blender3.6/</a>
            <a href="Blender4.2/">Blender4.2/</a>
            """;

        var series = BlenderReleaseProvider.ParseSeriesDirectories(html).Select(s => s.name).ToList();

        series.Should().BeEquivalentTo(["Blender2.83", "Blender3.6", "Blender4.2"]);
    }

    [Fact]
    public void ParseReleases_extracts_platform_files_without_duplicates()
    {
        // Directory listings repeat each file name in the anchor text; the parser must dedupe.
        const string html = """
            <a href="blender-4.2.0-windows-x64.zip">blender-4.2.0-windows-x64.zip</a>
            <a href="blender-4.2.0-windows-x64.zip">blender-4.2.0-windows-x64.zip</a>
            <a href="blender-4.2.1-windows-x64.zip">blender-4.2.1-windows-x64.zip</a>
            <a href="blender-4.2.1-linux-x64.tar.xz">blender-4.2.1-linux-x64.tar.xz</a>
            <a href="blender-4.2.1-windows-x64.zip.sha256">checksum</a>
            """;

        var releases = BlenderReleaseProvider.ParseReleases(html, "https://example.com/Blender4.2/").ToList();

        if (OperatingSystem.IsWindows())
        {
            releases.Should().HaveCount(2);
            releases.Select(r => r.Version).Should().BeEquivalentTo(["4.2.0", "4.2.1"]);
            releases.Should().OnlyContain(r => r.DownloadUrl.StartsWith("https://example.com/Blender4.2/blender-"));
        }
    }

    [Fact]
    public void ParseReleases_marks_lts_series()
    {
        const string html = """<a href="blender-4.2.1-windows-x64.zip">x</a>""";

        var releases = BlenderReleaseProvider.ParseReleases(html, "https://example.com/").ToList();

        if (OperatingSystem.IsWindows())
        {
            releases.Single().Channel.Should().Be(Brenda.Core.Models.BlenderChannel.Lts);
        }
    }
}
