using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Tests.Types;

public class DotnetVersionOperatorsTests
{
    [Theory]
    [InlineData(8, 0, 100, false, false, null)]
    [InlineData(8, 0, 101, true, false, 2)]
    [InlineData(8, 0, 101, false, true, 1)]
    public void CompareTo_WithLowerVersion_ShouldReturnGreaterThanZero(int major, int minor, int patch, bool isPreview,
        bool isRc, int? previewIdentifier)
    {
        // Arrange
        var higherVersion = new DotnetVersion(8, 0, 101);
        var lowerVersion = new DotnetVersion(major, minor, patch, isPreview, isRc, previewIdentifier);

        // Act
        var result = higherVersion.CompareTo(lowerVersion);

        // Assert
        Assert.True(result > 0);
    }

    [Theory]
    [InlineData(8, 0, 102, false, false, null)]
    [InlineData(8, 0, 100, true, false, 3)]
    [InlineData(8, 0, 100, false, true, 1)]
    public void CompareTo_WithHigherVersion_ShouldReturnSmallerThanZero(int major, int minor, int patch, bool isPreview,
        bool isRc, int? previewIdentifier)
    {
        // Arrange
        var higherVersion = new DotnetVersion(8, 0, 103);
        var lowerVersion = new DotnetVersion(major, minor, patch, isPreview, isRc, previewIdentifier);

        // Act
        var result = lowerVersion.CompareTo(higherVersion);

        // Assert
        Assert.True(result < 0);
        Assert.True(lowerVersion.CompareTo(null) < 0);
    }

    [Theory]
    [InlineData(8, 0, 102, false, false, null)]
    [InlineData(8, 0, 100, true, false, 3)]
    [InlineData(8, 0, 100, false, true, 1)]
    public void CompareTo_WithEqualVersion_ShouldReturnZero(int major, int minor, int patch, bool isPreview,
        bool isRc, int? previewIdentifier)
    {
        // Arrange
        var version1 = new DotnetVersion(major, minor, patch, isPreview, isRc, previewIdentifier);
        var version2 = new DotnetVersion(major, minor, patch, isPreview, isRc, previewIdentifier);

        // Act
        var result = version1.CompareTo(version2);

        // Assert
        Assert.True(result == 0);
    }

    [Fact]
    public void CompareTo_WithBothPreviews_ShouldCompareCorrectly()
    {
        // Arrange
        var version1 = new DotnetVersion(8, 0, 100, true, false, 2);
        var version2 = new DotnetVersion(8, 0, 100, true, false, 3);

        // Act
        var result = version1.CompareTo(version2);
        
        // Assert
        Assert.True(result < 0);
    }

    [Fact]
    public void Equals_WithEqualVersions_ShouldReturnTrue()
    {
        // Arrange
        var version1 = new DotnetVersion(8, 0, 100, true, false, 2);
        var version2 = new DotnetVersion(8, 0, 100, true, false, 2);
        
        var version3 = new DotnetVersion(8, 0, 102);
        var version4 = new DotnetVersion(8, 0, 102);
        
        // Act
        var result1 = version1.Equals(version2);
        var result2 = version3.Equals(version4);
        
        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.True(version1.Equals(version1));
    }
    
    [Fact]
    public void Equals_WithDifferentVersions_ShouldReturnFalse()
    {
        // Arrange
        var version1 = new DotnetVersion(8, 0, 100, true, false, 2);
        var version2 = new DotnetVersion(8, 0, 100, false, true, 2);
        
        var version3 = new DotnetVersion(8, 0, 102);
        var version4 = new DotnetVersion(8, 0, 104);
        
        // Act
        var result1 = version1.Equals(version2);
        var result2 = version3.Equals(version4);
        
        // Assert
        Assert.False(result1);
        Assert.False(result2);
        Assert.False(version1.Equals(null));
    }
}
