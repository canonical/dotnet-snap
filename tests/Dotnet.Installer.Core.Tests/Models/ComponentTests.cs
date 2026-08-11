using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Models.Events;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;
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
            IsLts = false,
            Grade = Grade.Rtm,
            EndOfLife = DateTime.Now
        };

        var fileService = new Mock<IFileService>();
        var manifestService = new Mock<IManifestService>();
        var snapService = new Mock<ISnapService>();
        var systemDService = new Mock<ISystemdService>();

        snapService.Setup(s => s.Install(It.IsAny<string>(), It.IsAny<SnapChannel>(), CancellationToken.None))
            .ReturnsAsync(new Terminal.InvocationResult(
                exitCode: 0, standardOutput: string.Empty, standardError: string.Empty));

        systemDService.Setup(s => s.DaemonReload())
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        systemDService.Setup(s => s.EnableUnit(It.IsAny<string>()))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        systemDService.Setup(s => s.StartUnit(It.IsAny<string>()))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));

        // Act
        var evt = await Assert.RaisesAsync<InstallationStartedEventArgs>(
            h => component.InstallationStarted += h,
            h => component.InstallationStarted -= h,
            () => component.Install(fileService.Object, manifestService.Object, snapService.Object,
                systemDService.Object));

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
            Grade = Grade.Rtm,
            EndOfLife = DateTime.Now
        };

        var fileService = new Mock<IFileService>();
        var manifestService = new Mock<IManifestService>();
        var snapService = new Mock<ISnapService>();
        var systemDService = new Mock<ISystemdService>();

        snapService.Setup(s => s.Install(It.IsAny<string>(), It.IsAny<SnapChannel>(), CancellationToken.None))
            .ReturnsAsync(new Terminal.InvocationResult(
                exitCode: 0, standardOutput: string.Empty, standardError: string.Empty));

        systemDService.Setup(s => s.DaemonReload())
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        systemDService.Setup(s => s.EnableUnit(It.IsAny<string>()))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        systemDService.Setup(s => s.StartUnit(It.IsAny<string>()))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));

        // Act
        var evt = await Assert.RaisesAsync<InstallationFinishedEventArgs>(
            h => component.InstallationFinished += h,
            h => component.InstallationFinished -= h,
            () => component.Install(fileService.Object, manifestService.Object, snapService.Object,
                systemDService.Object));

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
            IsLts = false,
            Grade = Grade.Rtm,
            EndOfLife = DateTime.Now
        };
        var component2 = new Component
        {
            Dependencies = [],
            Description = "description",
            Key = "key2",
            Name = "name",
            MajorVersion = 8,
            IsLts = false,
            Grade = Grade.Rtm,
            EndOfLife = DateTime.Now
        };
        var component3 = new Component
        {
            Dependencies = [],
            Description = "description",
            Key = "key3",
            Name = "name",
            MajorVersion = 8,
            IsLts = false,
            Grade = Grade.Rtm,
            EndOfLife = DateTime.Now
        };

        var fileService = new Mock<IFileService>();
        var manifestService = new Mock<IManifestService>();
        var snapService = new Mock<ISnapService>();
        var systemDService = new Mock<ISystemdService>();

        manifestService.Setup(s => s.Remote).Returns([component1, component2, component3]);
        manifestService.Setup(e => e.Add(
                It.IsAny<Component>(), CancellationToken.None))
            .Callback((Component c, CancellationToken cancellationToken) =>
            {
                installedComponents.Add(c.Key);
            });

        snapService.Setup(s => s.Install(It.IsAny<string>(), It.IsAny<SnapChannel>(), CancellationToken.None))
            .ReturnsAsync(new Terminal.InvocationResult(
                exitCode: 0, standardOutput: string.Empty, standardError: string.Empty));

        systemDService.Setup(s => s.DaemonReload())
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        systemDService.Setup(s => s.EnableUnit(It.IsAny<string>()))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        systemDService.Setup(s => s.StartUnit(It.IsAny<string>()))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));

        // Act
        await component1.Install(fileService.Object, manifestService.Object, snapService.Object, systemDService.Object);

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
            IsLts = false,
            Grade = Grade.Rtm,
            EndOfLife = DateTime.Now,
            Installation = new Installation(new DateTimeOffset(2024, 3, 19, 19, 3, 0, TimeSpan.FromHours(-3)))
        };

        var fileService = new Mock<IFileService>();
        var manifestService = new Mock<IManifestService>();
        var snapService = new Mock<ISnapService>();
        var systemDService = new Mock<ISystemdService>();

        fileService.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        manifestService.Setup(m => m.DotnetInstallLocation).Returns("dotnet_install_path");
        manifestService.Setup(m => m.SnapConfigurationLocation).Returns("snap_config_location");
        manifestService.Setup(m => m.Remove(It.IsAny<Component>(), CancellationToken.None))
            .Callback((Component c, CancellationToken cancellationToken) =>
            {
                installedComponents.Remove(c);
            });

        systemDService.Setup(s => s.DaemonReload())
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        systemDService.Setup(s => s.DisableUnit(It.IsAny<string>()))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        systemDService.Setup(s => s.StopUnit(It.IsAny<string>()))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));

        installedComponents.Add(component1);

        // Act
        await component1.Uninstall(fileService.Object, manifestService.Object, snapService.Object,
            systemDService.Object);

        // Assert
        Assert.False(component1.IsInstalled);
        Assert.Empty(installedComponents);
    }

    [Fact]
    public async Task Install_WithDependencyFailure_ShouldNotAddParentToManifest()
    {
        // Arrange
        var parent = new Component
        {
            Dependencies = ["dep1"],
            Description = "Parent",
            Key = "parent",
            Name = "name",
            MajorVersion = 8,
            IsLts = false,
            Grade = Grade.Rtm,
            EndOfLife = DateTime.Now
        };

        var dependency = new Component
        {
            Dependencies = [],
            Description = "Dependency",
            Key = "dep1",
            Name = "name",
            MajorVersion = 8,
            IsLts = false,
            Grade = Grade.Rtm,
            EndOfLife = DateTime.Now
        };

        var fileService = new Mock<IFileService>();
        var manifestService = new Mock<IManifestService>();
        var snapService = new Mock<ISnapService>();
        var systemDService = new Mock<ISystemdService>();

        manifestService.Setup(s => s.Remote).Returns([parent, dependency]);

        snapService.Setup(s => s.Install(It.IsAny<string>(), It.IsAny<SnapChannel>(), CancellationToken.None))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));

        // Make the dependency fail when installing its mount units
        systemDService.Setup(s => s.DaemonReload())
            .ReturnsAsync(() => new Terminal.InvocationResult(1, string.Empty, "daemon reload failed"));

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() =>
            parent.Install(fileService.Object, manifestService.Object, snapService.Object, systemDService.Object));

        manifestService.Verify(m => m.Add(parent, CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task Install_WhenMountUnitsFail_ShouldRollbackSnapInstall()
    {
        // Arrange
        var component = new Component
        {
            Dependencies = [],
            Description = "Component",
            Key = "dotnet-sdk-80",
            Name = "sdk",
            MajorVersion = 8,
            IsLts = false,
            Grade = Grade.Rtm,
            EndOfLife = DateTime.Now
        };

        var fileService = new Mock<IFileService>();
        var manifestService = new Mock<IManifestService>();
        var snapService = new Mock<ISnapService>();
        var systemDService = new Mock<ISystemdService>();

        snapService.SetupSequence(s => s.IsSnapInstalled(component.Key))
            .Returns(false)
            .Returns(true);
        snapService.Setup(s => s.Install(component.Key, It.IsAny<SnapChannel>(), CancellationToken.None))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        snapService.Setup(s => s.FindSnap(component.Key, CancellationToken.None))
            .ReturnsAsync(new SnapInfo(component.Key, "8.0.0", "1", "stable", new SnapPublisher("id", "dotnet", ".NET", "verified")));

        fileService.Setup(f => f.EnumerateContentSnapMountFiles(component.Key))
            .Returns(["/snap/dotnet-sdk-80/current/mounts/unit.mount"]);
        fileService.Setup(f => f.ReadUnitsFile(It.IsAny<string>(), component.Key))
            .ReturnsAsync(["unit.mount"]);

        // Fail daemon-reload so PlaceMountUnits throws
        systemDService.Setup(s => s.DaemonReload())
            .ReturnsAsync(new Terminal.InvocationResult(1, string.Empty, "daemon reload failed"));

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() =>
            component.Install(fileService.Object, manifestService.Object, snapService.Object, systemDService.Object));

        manifestService.Verify(m => m.Add(component, CancellationToken.None), Times.Never);
        snapService.Verify(s => s.Remove(component.Key, true, CancellationToken.None), Times.Once);
        fileService.Verify(f => f.PlaceLinkageFile(component.Key), Times.Once);
        fileService.Verify(f => f.RemoveLinkageFile(component.Key), Times.Once);
    }

    [Fact]
    public async Task Install_WhenPathUnitsFail_ShouldRollbackMountUnitsAndSnap()
    {
        // Arrange
        var component = new Component
        {
            Dependencies = [],
            Description = "Component",
            Key = "dotnet-sdk-80",
            Name = "sdk",
            MajorVersion = 8,
            IsLts = false,
            Grade = Grade.Rtm,
            EndOfLife = DateTime.Now
        };

        var fileService = new Mock<IFileService>();
        var manifestService = new Mock<IManifestService>();
        var snapService = new Mock<ISnapService>();
        var systemDService = new Mock<ISystemdService>();

        snapService.SetupSequence(s => s.IsSnapInstalled(component.Key))
            .Returns(false)
            .Returns(true);
        snapService.Setup(s => s.Install(component.Key, It.IsAny<SnapChannel>(), CancellationToken.None))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        snapService.Setup(s => s.FindSnap(component.Key, CancellationToken.None))
            .ReturnsAsync(new SnapInfo(component.Key, "8.0.0", "1", "stable", new SnapPublisher("id", "dotnet", ".NET", "verified")));

        fileService.Setup(f => f.EnumerateContentSnapMountFiles(component.Key))
            .Returns(["/snap/dotnet-sdk-80/current/mounts/unit.mount"]);
        fileService.Setup(f => f.ReadUnitsFile(It.IsAny<string>(), component.Key))
            .ReturnsAsync(["unit.mount"]);

        // Succeed mount units
        systemDService.Setup(s => s.DaemonReload())
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        systemDService.Setup(s => s.EnableUnit(It.IsAny<string>()))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        systemDService.Setup(s => s.StartUnit(It.IsAny<string>()))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));

        // Fail path unit enable
        systemDService.Setup(s => s.EnableUnit($"{component.Key}-update-watcher.path"))
            .ReturnsAsync(new Terminal.InvocationResult(1, string.Empty, "enable failed"));
        systemDService.Setup(s => s.DisableUnit($"{component.Key}-update-watcher.path"))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        systemDService.Setup(s => s.StopUnit($"{component.Key}-update-watcher.path"))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() =>
            component.Install(fileService.Object, manifestService.Object, snapService.Object, systemDService.Object));

        manifestService.Verify(m => m.Add(component, CancellationToken.None), Times.Never);
        snapService.Verify(s => s.Remove(component.Key, true, CancellationToken.None), Times.Once);
        fileService.Verify(f => f.PlaceLinkageFile(component.Key), Times.Once);
        fileService.Verify(f => f.RemoveLinkageFile(component.Key), Times.Once);
        fileService.Verify(f => f.UninstallSystemdPathUnit(component.Key), Times.Once);
    }

    [Fact]
    public async Task Install_WhenManifestAddFails_ShouldRollbackAllInstallationSteps()
    {
        // Arrange
        var component = new Component
        {
            Dependencies = [],
            Description = "Component",
            Key = "dotnet-sdk-80",
            Name = "sdk",
            MajorVersion = 8,
            IsLts = false,
            Grade = Grade.Rtm,
            EndOfLife = DateTime.Now
        };

        var fileService = new Mock<IFileService>();
        var manifestService = new Mock<IManifestService>();
        var snapService = new Mock<ISnapService>();
        var systemDService = new Mock<ISystemdService>();

        snapService.SetupSequence(s => s.IsSnapInstalled(component.Key))
            .Returns(false)
            .Returns(true);
        snapService.Setup(s => s.Install(component.Key, It.IsAny<SnapChannel>(), CancellationToken.None))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        snapService.Setup(s => s.FindSnap(component.Key, CancellationToken.None))
            .ReturnsAsync(new SnapInfo(component.Key, "8.0.0", "1", "stable", new SnapPublisher("id", "dotnet", ".NET", "verified")));

        fileService.Setup(f => f.EnumerateContentSnapMountFiles(component.Key))
            .Returns(["/snap/dotnet-sdk-80/current/mounts/unit.mount"]);
        fileService.Setup(f => f.ReadUnitsFile(It.IsAny<string>(), component.Key))
            .ReturnsAsync(["unit.mount"]);

        systemDService.Setup(s => s.DaemonReload())
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        systemDService.Setup(s => s.EnableUnit(It.IsAny<string>()))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        systemDService.Setup(s => s.StartUnit(It.IsAny<string>()))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        systemDService.Setup(s => s.DisableUnit(It.IsAny<string>()))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));
        systemDService.Setup(s => s.StopUnit(It.IsAny<string>()))
            .ReturnsAsync(new Terminal.InvocationResult(0, string.Empty, string.Empty));

        manifestService.Setup(m => m.Add(component, CancellationToken.None))
            .ThrowsAsync(new ApplicationException("manifest write failed"));

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() =>
            component.Install(fileService.Object, manifestService.Object, snapService.Object, systemDService.Object));

        snapService.Verify(s => s.Remove(component.Key, true, CancellationToken.None), Times.Once);
        fileService.Verify(f => f.RemoveLinkageFile(component.Key), Times.Once);
        fileService.Verify(f => f.UninstallSystemdPathUnit(component.Key), Times.Once);
    }
}
