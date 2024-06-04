using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Models.Events;
using Dotnet.Installer.Core.Services.Contracts;
using Moq;

namespace Dotnet.Installer.Core.Tests.Models;

public class ComponentTests
{
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
            MajorVersion = 8,
            DotnetRoot = "/",
            MountPoints = ["mp1"],
            IsLts = false,
            EndOfLife = DateTime.Now
        };

        var fileService = new Mock<IFileService>();
        var manifestService = new Mock<IManifestService>();
        var snapService = new Mock<ISnapService>();

        // Act
        var evt = await Assert.RaisesAsync<InstallationStartedEventArgs>(
            h => component.InstallationStarted += h,
            h => component.InstallationStarted -= h,
            () => component.Install(fileService.Object, manifestService.Object, snapService.Object));

        // Assert
        Assert.NotNull(evt);
        Assert.Equal(component, evt.Sender);
        Assert.Equivalent(new InstallationStartedEventArgs(component.Key), evt.Arguments);
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
            MajorVersion = 8,
            IsLts = false,
            DotnetRoot = "/",
            MountPoints = ["mp1"],
            EndOfLife = DateTime.Now
        };

        var fileService = new Mock<IFileService>();
        var manifestService = new Mock<IManifestService>();
        var snapService = new Mock<ISnapService>();

        // Act
        var evt = await Assert.RaisesAsync<InstallationFinishedEventArgs>(
            h => component.InstallationFinished += h,
            h => component.InstallationFinished -= h,
            () => component.Install(fileService.Object, manifestService.Object, snapService.Object));

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
            MajorVersion = 8,
            DotnetRoot = "/",
            MountPoints = ["mp1"],
            IsLts = false,
            EndOfLife = DateTime.Now
        };
        var component2 = new Component
        {
            Dependencies = [],
            Description = "description",
            Key = "key2",
            Name = "name",
            MajorVersion = 8,
            DotnetRoot = "/",
            MountPoints = ["mp1"],
            IsLts = false,
            EndOfLife = DateTime.Now
        };
        var component3 = new Component
        {
            Dependencies = [],
            Description = "description",
            Key = "key3",
            Name = "name",
            MajorVersion = 8,
            DotnetRoot = "/",
            MountPoints = ["mp1"],
            IsLts = false,
            EndOfLife = DateTime.Now
        };
        
        var fileService = new Mock<IFileService>();
        var manifestService = new Mock<IManifestService>();
        var snapService = new Mock<ISnapService>();

        manifestService.Setup(s => s.Remote).Returns([component1, component2, component3]);
        manifestService.Setup(e => e.Add(
                It.IsAny<Component>(), CancellationToken.None))
            .Callback((Component c, CancellationToken cancellationToken) =>
            {
                installedComponents.Add(c.Key);
            });
        
        // Act
        await component1.Install(fileService.Object, manifestService.Object, snapService.Object);

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
            MajorVersion = 8,
            DotnetRoot = "/",
            MountPoints = ["mp1"],
            IsLts = false,
            EndOfLife = DateTime.Now,
            Installation = new Installation
            {
                InstalledAt = new DateTimeOffset(2024, 3, 19, 19, 3, 0, TimeSpan.FromHours(-3))
            }
        };
        
        var fileService = new Mock<IFileService>();
        var manifestService = new Mock<IManifestService>();
        var snapService = new Mock<ISnapService>();

        fileService.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        manifestService.Setup(m => m.DotnetInstallLocation).Returns("dotnet_install_path");
        manifestService.Setup(m => m.SnapConfigurationLocation).Returns("snap_config_location");
        manifestService.Setup(m => m.Remove(It.IsAny<Component>(), CancellationToken.None))
            .Callback((Component c, CancellationToken cancellationToken) =>
            {
                installedComponents.Remove(c);
            });
        
        installedComponents.Add(component1);
        
        // Act
        await component1.Uninstall(fileService.Object, manifestService.Object, snapService.Object);

        // Assert
        Assert.Null(component1.Installation);
        Assert.Empty(installedComponents);
    }
}
