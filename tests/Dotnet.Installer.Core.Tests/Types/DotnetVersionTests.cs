using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Tests.Types;

public class DotnetVersionTests
{
    [Theory]
    [InlineData(8, 0, 100, true, false, 1, false)]
    [InlineData(8, 0, 100, false, true, 2, false)]
    [InlineData(6, 0, 206, false, false, null, true)]
    public void IsStable_WhenCalled_ShouldIdentifyWhetherVersionIsStable(int major, int minor, int patch,
        bool isPreview, bool isRc, int? previewIdentifier, bool expectedResult)
    {
        // Arrange
        var version = new DotnetVersion(major, minor, patch, isPreview, isRc, previewIdentifier);
        
        // Act
        var isStable = version.IsStable;

        // Assert
        Assert.Equal(expectedResult, isStable);
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
    [InlineData(8, 0, 103, 100)]
    [InlineData(8, 0, 201, 200)]
    [InlineData(6, 0, 405, 400)]
    [InlineData(6, 0, 26, null)]
    public void FeatureBand_WhenCalled_ShouldIdentifyFeatureBandCorrectly(int major, int minor, int patch,
        int? expectedFeatureBand)
    {
        // Arrange
        var version = new DotnetVersion(major, minor, patch);

        // Act
        var featureBand = version.FeatureBand;

        // Assert
        Assert.Equal(expectedFeatureBand, featureBand);
    }
    
    [Theory]
    [InlineData(8, 0, 0, false, false, null, 1)]
    [InlineData(8, 0, 0, true, false, 1, 2)]
    [InlineData(8, 0, 0, false, true, 2, null)]
    public void Constructor_WithValidInput_ShouldConstructObject(int major, int minor, int patch,
        bool isPreview, bool isRc, int? previewIdentifier, int? revision)
    {
        // Act
        var version = new DotnetVersion(major, minor, patch, isPreview, isRc, previewIdentifier, revision);

        // Assert
        Assert.NotNull(version);
    }
    
    [Theory]
    [InlineData(8, 0, 0, true, true, null, null)]
    [InlineData(8, 0, 0, true, false, null, null)]
    [InlineData(8, 0, 0, false, true, null, null)]
    [InlineData(8, 0, 0, false, false, 1, null)]
    [InlineData(8, 0, 0, false, false, null, 0)]
    public void Constructor_WithInvalidInput_ShouldThrowApplicationException(int major, int minor, int patch,
        bool isPreview, bool isRc, int? previewIdentifier, int? revision)
    {
        // Assert
        Assert.Throws<ApplicationException>(() => new DotnetVersion(major, minor, patch, isPreview, isRc,
            previewIdentifier, revision));
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
    [InlineData("8.0.0", 8, 0, 0, null)]
    [InlineData("8.0.101", 8, 0, 101, null)]
    [InlineData("8.0.0+1", 8, 0, 0, 1)]
    [InlineData("8.0.101+2", 8, 0, 101, 2)]
    public void Parse_WithStableVersionAndRevisionInput_ShouldParseCorrectly(string versionString,
        int major, int minor, int patch, int? revision)
    {
        // Act
        var version = DotnetVersion.Parse(versionString);

        // Assert
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(patch, version.Patch);
        Assert.Equal(revision, version.Revision);
    }

    
    [Theory]
    [InlineData("8.0.0-preview.3", 8, 0, 0, true, false, 3, null)]
    [InlineData("8.0.0-preview.3+2", 8, 0, 0, true, false, 3, 2)]
    [InlineData("8.0.101-rc.1+1", 8, 0, 101, false, true, 1, 1)]
    public void Parse_WithPreviewVersionAndRevisionInput_ShouldParseCorrectly(string versionString, int major, int minor, int patch,
        bool isPreview, bool isRc, int previewIdentifier, int? revision)
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
        Assert.Equal(revision, version.Revision);
    }

    [Theory]
    [InlineData(8, 0, 0, false, false, null, null, "8.0.0")]
    [InlineData(8, 0, 0, true, false, 1, null, "8.0.0-preview.1")]
    [InlineData(8, 0, 0, false, true, 2, null, "8.0.0-rc.2")]
    [InlineData(8, 0, 0, false, false, null, 1, "8.0.0+1")]
    [InlineData(8, 0, 0, true, false, 1, 2, "8.0.0-preview.1+2")]
    [InlineData(8, 0, 0, false, true, 2, 54, "8.0.0-rc.2+54")]
    public void ToString_WhenCalled_ShouldStringifyVersionCorrectly(int major, int minor, int patch,
        bool isPreview, bool isRc, int? previewIdentifier, int? revision, string expectedString)
    {
        // Act
        var version = new DotnetVersion(major, minor, patch, isPreview, isRc, previewIdentifier, revision);
        
        // Assert
        Assert.Equal(expectedString, version.ToString());
    }
}
