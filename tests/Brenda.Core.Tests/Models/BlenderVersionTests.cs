using Brenda.Core.Models;
using FluentAssertions;
using Xunit;

namespace Brenda.Core.Tests.Models;

public class BlenderVersionTests
{
    [Theory]
    [InlineData("4.2.1", "4.2")]
    [InlineData("3.6.0", "3.6")]
    [InlineData("2.93.18", "2.93")]
    [InlineData("4.2", "4.2")]
    [InlineData("4", "4")]
    public void Series_returns_major_minor(string version, string expectedSeries)
    {
        var installed = new BlenderVersion { Version = version };
        var release = new BlenderRelease { Version = version };

        installed.Series.Should().Be(expectedSeries);
        release.Series.Should().Be(expectedSeries);
    }
}
