using Dotnet.Installer.Core.Exceptions;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;
using Moq;

namespace Dotnet.Installer.Core.Tests.Models;

public class ComponentTests
{
    [Fact]
    public void Install_WithHigherRuntimeVersionThanLimit_ShouldThrowApplicationException()
    {
        // Arrange
        var component = new Component
        {
            Dependencies = ["key2", "key3"],
            Description = "description",
            Key = "key1",
            Name = "name",
            Packages = [new Package { Name = "package1", Version = "1.0" }],
            Version = new DotnetVersion(8, 0, 3),
            BaseUrl = new Uri("https://test.com")
        };

        var fileService = new Mock<IFileService>();
        var limitsService = new Mock<ILimitsService>();
        var manifestService = new Mock<IManifestService>();
        limitsService.Setup(l => l.Runtime).Returns(new DotnetVersion(8, 0, 2));
        limitsService.Setup(l => l.Sdk).Returns([
            new DotnetVersion(8, 0, 102),
            new DotnetVersion(8, 0, 201)
        ]);

        // Assert
        Assert.ThrowsAsync<VersionTooHighException>(() => 
            component.Install(fileService.Object, limitsService.Object, manifestService.Object));
    }
    
    [Fact]
    public void Install_WithHigherSdkVersionThanLimit_ShouldThrowApplicationException()
    {
        // Arrange
        var component = new Component
        {
            Dependencies = ["key2", "key3"],
            Description = "description",
            Key = "key1",
            Name = "name",
            Packages = [new Package { Name = "package1", Version = "1.0" }],
            Version = new DotnetVersion(8, 0, 103),
            BaseUrl = new Uri("https://test.com")
        };

        var fileService = new Mock<IFileService>();
        var limitsService = new Mock<ILimitsService>();
        var manifestService = new Mock<IManifestService>();
        limitsService.Setup(l => l.Runtime).Returns(new DotnetVersion(8, 0, 2));
        limitsService.Setup(l => l.Sdk).Returns([
            new DotnetVersion(8, 0, 102),
            new DotnetVersion(8, 0, 201)
        ]);

        // Assert
        Assert.ThrowsAsync<VersionTooHighException>(() => 
            component.Install(fileService.Object, limitsService.Object, manifestService.Object));
    }
}