using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Tests.Types;

public class DotnetVersionTests
{
    [Theory]
    [InlineData("8.0.0", 8, 0, 0)]
    [InlineData("8.0.101", 8, 0, 101)]
    public void Parse_WithStableVersionInput_ShouldParseCorrectly(string versionString, int major, int minor, int patch)
    {
        // Arrange

        // Act
        var version = DotnetVersion.Parse(versionString);

        // Assert
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(patch, version.Patch);
    }
    
    [Theory]
    [InlineData("8.0.0-preview.3", 8, 0, 0, true, false, 3)]
    [InlineData("8.0.101-rc.1", 8, 0, 101, false, true, 1)]
    public void Parse_WithPreviewVersionInput_ShouldParseCorrectly(string versionString, int major, int minor, int patch,
        bool isPreview, bool isRc, int previewIdentifier)
    {
        // Arrange

        // Act
        var version = DotnetVersion.Parse(versionString);

        // Assert
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(patch, version.Patch);
        Assert.Equal(isPreview, version.IsPreview);
        Assert.Equal(isRc, version.IsRc);
        Assert.Equal(previewIdentifier, version.PreviewIdentifier);
    }

    [Theory]
    [InlineData(8, 0, 0, true)]
    [InlineData(8, 0, 100, false)]
    public void IsRuntime_WhenCalled_ShouldMapCorrectly(int major, int minor, int patch, bool expectedResult)
    {
        // Arrange
        var version = new DotnetVersion(major, minor, patch);

        // Act
        var actualResult = version.IsRuntime;

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }
}