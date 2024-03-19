using System.Text;
using System.Text.Json;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Services.Implementations;
using Dotnet.Installer.Core.Types;
using Moq;

namespace Dotnet.Installer.Core.Tests.Services.Implementations;

public class LimitsServiceTests
{
    [Fact]
    public void Constructor_WithValidJsonInput_ShouldParseLimitsCorrectly()
    {
        // Arrange
        var jsonInput = """
                        {
                            "runtime": "8.0.3",
                            "sdk": [
                                "8.0.103",
                                "8.0.201"
                            ]
                        }
                        """;

        var fileServiceMock = new Mock<IFileService>();
        fileServiceMock.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(true);
        fileServiceMock.Setup(fs => fs.OpenRead(It.IsAny<string>()))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(jsonInput)));
        
        // Act
        var limitsService = new LimitsService(fileServiceMock.Object);
        
        // Assert
        Assert.Equal(new DotnetVersion(8, 0, 3), limitsService.Runtime);
        Assert.True(limitsService.Sdk.Count() == 2);
        Assert.Contains(new DotnetVersion(8, 0, 103), limitsService.Sdk);
        Assert.Contains(new DotnetVersion(8, 0, 201), limitsService.Sdk);
    }

    [Fact]
    public void Constructor_WithInvalidJson_ShouldThrowApplicationException()
    {
        // Arrange
        var jsonInput = """
                        {
                            "runtime": "8.0.3",
                            "sdk": [
                                "8.0.103",
                                "8.0.201"sss
                            ]
                        }
                        """;

        var fileServiceMock = new Mock<IFileService>();
        fileServiceMock.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(true);
        fileServiceMock.Setup(fs => fs.OpenRead(It.IsAny<string>()))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(jsonInput)));
        
        // Assert
        Assert.ThrowsAny<JsonException>(() => new LimitsService(fileServiceMock.Object));
    }
    
    [Fact]
    public void Constructor_WithInvalidRuntime_ShouldThrowApplicationException()
    {
        // Arrange
        var jsonInput = """
                        {
                            "something-else": "8.0.3",
                            "sdk": [
                                "8.0.103",
                                "8.0.201"
                            ]
                        }
                        """;

        var fileServiceMock = new Mock<IFileService>();
        fileServiceMock.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(true);
        fileServiceMock.Setup(fs => fs.OpenRead(It.IsAny<string>()))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(jsonInput)));
        
        // Assert
        Assert.Throws<KeyNotFoundException>(() => new LimitsService(fileServiceMock.Object));
    }
    
    [Fact]
    public void Constructor_WithInvalidSdk_ShouldThrowApplicationException()
    {
        // Arrange
        var jsonInput = """
                        {
                            "runtime": "8.0.3",
                            "something-else": [
                                "8.0.103",
                                "8.0.201"
                            ]
                        }
                        """;

        var fileServiceMock = new Mock<IFileService>();
        fileServiceMock.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(true);
        fileServiceMock.Setup(fs => fs.OpenRead(It.IsAny<string>()))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(jsonInput)));
        
        // Assert
        Assert.Throws<KeyNotFoundException>(() => new LimitsService(fileServiceMock.Object));
    }
    
    [Fact]
    public void Constructor_WithInvalidVersion_ShouldThrowApplicationException()
    {
        // Arrange
        var jsonInput = """
                        {
                            "runtime": "8.0.3",
                            "sdk": [
                                "aaa",
                                "8.0.201"
                            ]
                        }
                        """;

        var fileServiceMock = new Mock<IFileService>();
        fileServiceMock.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(true);
        fileServiceMock.Setup(fs => fs.OpenRead(It.IsAny<string>()))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(jsonInput)));
        
        // Assert
        Assert.Throws<FormatException>(() => new LimitsService(fileServiceMock.Object));
    }
}