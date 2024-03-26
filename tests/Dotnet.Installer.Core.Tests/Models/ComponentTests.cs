using Dotnet.Installer.Core.Exceptions;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Models.Events;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;
using Moq;

namespace Dotnet.Installer.Core.Tests.Models;

public class ComponentTests
{
    [Fact]
    public async Task Install_WithHigherRuntimeVersionThanLimit_ShouldThrowApplicationException()
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
        await Assert.ThrowsAsync<VersionTooHighException>(() => 
            component.Install(fileService.Object, limitsService.Object, manifestService.Object));
    }
    
    [Fact]
    public async Task Install_WithHigherSdkVersionThanLimit_ShouldThrowApplicationException()
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
        await Assert.ThrowsAsync<VersionTooHighException>(() => 
            component.Install(fileService.Object, limitsService.Object, manifestService.Object));
    }

    [Fact]
    public async Task Install_WithValidVersions_ShouldInvokeInstallationStartedEvent()
    {
        // Arrange
        var component = new Component
        {
            Dependencies = [],
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
        limitsService.Setup(l => l.Sdk).Returns([
            new DotnetVersion(8, 0, 103),
            new DotnetVersion(8, 0, 201)
        ]);

        // Act
        var evt = await Assert.RaisesAsync<InstallationStartedEventArgs>(
            h => component.InstallationStarted += h,
            h => component.InstallationStarted -= h,
            () => component.Install(fileService.Object, limitsService.Object, manifestService.Object));

        // Assert
        Assert.NotNull(evt);
        Assert.Equal(component, evt.Sender);
        Assert.Equivalent(new InstallationStartedEventArgs(component.Key), evt.Arguments);
    }
    
    [Fact]
    public async Task Install_WithValidVersions_ShouldInvokeInstallingPackageChangedEvent()
    {
        // Arrange
        var component = new Component
        {
            Dependencies = [],
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
        limitsService.Setup(l => l.Sdk).Returns([
            new DotnetVersion(8, 0, 103),
            new DotnetVersion(8, 0, 201)
        ]);

        // Act
        var evt = await Assert.RaisesAsync<InstallingPackageChangedEventArgs>(
            h => component.InstallingPackageChanged += h,
            h => component.InstallingPackageChanged -= h,
            () => component.Install(fileService.Object, limitsService.Object, manifestService.Object));

        // Assert
        Assert.NotNull(evt);
        Assert.Equal(component, evt.Sender);
        Assert.Equivalent(new InstallingPackageChangedEventArgs(
            new Package { Name = "package1", Version = "1.0" }), evt.Arguments);
    }
    
    [Fact]
    public async Task Install_WithValidVersions_ShouldInvokeInstallationFinishedEvent()
    {
        // Arrange
        var component = new Component
        {
            Dependencies = [],
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
        limitsService.Setup(l => l.Sdk).Returns([
            new DotnetVersion(8, 0, 103),
            new DotnetVersion(8, 0, 201)
        ]);

        // Act
        var evt = await Assert.RaisesAsync<InstallationFinishedEventArgs>(
            h => component.InstallationFinished += h,
            h => component.InstallationFinished -= h,
            () => component.Install(fileService.Object, limitsService.Object, manifestService.Object));

        // Assert
        Assert.NotNull(evt);
        Assert.Equal(component, evt.Sender);
        Assert.Equivalent(new InstallationFinishedEventArgs(component.Key), evt.Arguments);
    }

    [Fact]
    public async Task Install_WithMultipleDependencies_ShouldTraverseAndInstallDependencies()
    {
        var installedComponents = new List<string>();
        var expectedInstalledComponents = new List<string> { "key1", "key2", "key3" };
        var component1 = new Component
        {
            Dependencies = ["key2", "key3"],
            Description = "description",
            Key = "key1",
            Name = "name",
            Packages = [new Package { Name = "package1", Version = "1.0" }],
            Version = new DotnetVersion(8, 0, 103),
            BaseUrl = new Uri("https://test.com")
        };
        var component2 = new Component
        {
            Dependencies = [],
            Description = "description",
            Key = "key2",
            Name = "name",
            Packages = [new Package { Name = "package1", Version = "1.0" }],
            Version = new DotnetVersion(8, 0, 103),
            BaseUrl = new Uri("https://test.com")
        };
        var component3 = new Component
        {
            Dependencies = [],
            Description = "description",
            Key = "key3",
            Name = "name",
            Packages = [new Package { Name = "package1", Version = "1.0" }],
            Version = new DotnetVersion(8, 0, 103),
            BaseUrl = new Uri("https://test.com")
        };
        
        var fileService = new Mock<IFileService>();
        var limitsService = new Mock<ILimitsService>();
        var manifestService = new Mock<IManifestService>();

        limitsService.Setup(l => l.Sdk).Returns([
            new DotnetVersion(8, 0, 103),
            new DotnetVersion(8, 0, 201)
        ]);
        manifestService.Setup(s => s.Remote).Returns([component1, component2, component3]);
        manifestService.Setup(e => e.Add(
                It.IsAny<Component>(), CancellationToken.None))
            .Callback((Component c, CancellationToken cancellationToken) =>
            {
                installedComponents.Add(c.Key);
            });
        
        // Act
        await component1.Install(fileService.Object, limitsService.Object, manifestService.Object);

        // Assert
        Assert.True(installedComponents.Count == 3);
        Assert.Equivalent(expectedInstalledComponents, installedComponents);
    }

    [Fact]
    public async Task Uninstall_WithInstalledComponent_ShouldUninstall()
    {
        // Arrange
        var installedComponents = new List<Component>();
        var component1 = new Component
        {
            Dependencies = [],
            Description = "description",
            Key = "key1",
            Name = "name",
            Packages = [new Package { Name = "package1", Version = "1.0" }],
            Version = new DotnetVersion(8, 0, 103),
            BaseUrl = new Uri("https://test.com"),
            Installation = new Installation
            {
                InstalledAt = new DateTimeOffset(2024, 3, 19, 19, 3, 0, TimeSpan.FromHours(-3))
            }
        };
        
        var fileService = new Mock<IFileService>();
        var manifestService = new Mock<IManifestService>();

        fileService.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        manifestService.Setup(m => m.DotnetInstallLocation).Returns("test");
        manifestService.Setup(m => m.Remove(It.IsAny<Component>(), CancellationToken.None))
            .Callback((Component c, CancellationToken cancellationToken) =>
            {
                installedComponents.Remove(c);
            });
        
        installedComponents.Add(component1);
        
        // Act
        await component1.Uninstall(fileService.Object, manifestService.Object);

        // Assert
        Assert.Null(component1.Installation);
        Assert.Empty(installedComponents);
    }
    
    [Fact]
    public async Task Uninstall_WhenRegistrationFileDoesNotExist_ShouldThrowApplicationException()
    {
        // Arrange
        var component1 = new Component
        {
            Dependencies = [],
            Description = "description",
            Key = "key1",
            Name = "name",
            Packages = [new Package { Name = "package1", Version = "1.0" }],
            Version = new DotnetVersion(8, 0, 103),
            BaseUrl = new Uri("https://test.com"),
            Installation = new Installation
            {
                InstalledAt = new DateTimeOffset(2024, 3, 19, 19, 3, 0, TimeSpan.FromHours(-3))
            }
        };
        
        var fileService = new Mock<IFileService>();
        var manifestService = new Mock<IManifestService>();

        fileService.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        manifestService.Setup(m => m.DotnetInstallLocation).Returns("test");

        // Assert
        await Assert.ThrowsAsync<ApplicationException>(() =>
            component1.Uninstall(fileService.Object, manifestService.Object));
    }
}
