using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Tests.Types;

public class DotnetVersionTests
{
    [Theory]
    [InlineData(8, 0, 0, false, false, null)]
    [InlineData(8, 0, 0, true, false, 1)]
    [InlineData(8, 0, 0, false, true, 2)]
    public void Constructor_WithValidInput_ShouldConstructObject(int major, int minor, int patch,
        bool isPreview, bool isRc, int? previewIdentifier)
    {
        // Act
        var version = new DotnetVersion(major, minor, patch, isPreview, isRc, previewIdentifier);

        // Assert
        Assert.NotNull(version);
    }
    
    [Theory]
    [InlineData(8, 0, 0, true, true, null)]
    [InlineData(8, 0, 0, true, false, null)]
    [InlineData(8, 0, 0, false, true, null)]
    [InlineData(8, 0, 0, false, false, 1)]
    public void Constructor_WithInvalidInput_ShouldThrowApplicationException(int major, int minor, int patch,
        bool isPreview, bool isRc, int? previewIdentifier)
    {
        // Assert
        Assert.Throws<ApplicationException>(() => new DotnetVersion(major, minor, patch, isPreview, isRc,
            previewIdentifier));
    }
    
    [Theory]
    [InlineData("8.0.0", 8, 0, 0)]
    [InlineData("8.0.101", 8, 0, 101)]
    public void Parse_WithStableVersionInput_ShouldParseCorrectly(string versionString, int major, int minor, int patch)
    {
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
    
    [Theory]
    [InlineData(8, 0, 0, false)]
    [InlineData(8, 0, 100, true)]
    public void IsSdk_WhenCalled_ShouldMapCorrectly(int major, int minor, int patch, bool expectedResult)
    {
        // Arrange
        var version = new DotnetVersion(major, minor, patch);

        // Act
        var actualResult = version.IsSdk;

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }

    [Theory]
    [InlineData(8, 0, 0, false, false, null, "8.0.0")]
    [InlineData(8, 0, 0, true, false, 1, "8.0.0-preview.1")]
    [InlineData(8, 0, 0, false, true, 2, "8.0.0-rc.2")]
    public void ToString_WhenCalled_ShouldStringifyVersionCorrectly(int major, int minor, int patch,
        bool isPreview, bool isRc, int? previewIdentifier, string expectedString)
    {
        // Act
        var version = new DotnetVersion(major, minor, patch, isPreview, isRc, previewIdentifier);
        
        // Assert
        Assert.Equal(expectedString, version.ToString());
    }
}