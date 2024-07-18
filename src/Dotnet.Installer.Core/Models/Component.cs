using System.Text.Json.Serialization;
using Dotnet.Installer.Core.Models.Events;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Models;

public class Component
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int MajorVersion { get; init; }
    public required bool IsLts { get; init; }
    [JsonPropertyName("eol")]
    public required DateTime EndOfLife { get; init; }
    public required string DotnetRoot { get; init; }
    public required IEnumerable<string> Dependencies { get; init; }
    public Installation? Installation { get; set; }

    public event EventHandler<InstallationStartedEventArgs>? InstallationStarted;
    public event EventHandler<InstallationFinishedEventArgs>? InstallationFinished;

    public async Task Install(IFileService fileService, IManifestService manifestService, ISnapService snapService,
        ILogger? logger = default)
    {
        if (Installation is null)
        {
            InstallationStarted?.Invoke(this, new InstallationStartedEventArgs(Key));

            // Install content snap on the machine
            if (!snapService.IsSnapInstalled(Key))
            {
                var result = await snapService.Install(Key);
                if (!result.IsSuccess) throw new ApplicationException(result.StandardError);
            }

            // Install Systemd mount units
            var unitPaths =
                Directory.EnumerateFiles(Path.Join("/", "snap", Key, "current", "mounts"));
            foreach (var unitPath in unitPaths)
            {
                logger?.LogDebug($"Copying {unitPath} to systemd directory.");
                fileService.InstallSystemdMountUnit(unitPath);
            }

            // Mount component locations
            await Mount(fileService, manifestService, logger);

            // Register the installation of this component in the local manifest file
            await manifestService.Add(this);
        }
        else
        {
            logger?.LogInformation($"{Key} already installed!");
        }

        foreach (var dependency in Dependencies)
        {
            var component = manifestService.Remote.First(c => c.Key == dependency);

            component.InstallationStarted += InstallationStarted;
            component.InstallationFinished += InstallationFinished;

            await component.Install(fileService, manifestService, snapService, logger);
        }

        InstallationFinished?.Invoke(this, new InstallationFinishedEventArgs(Key));
    }

    public async Task Uninstall(IFileService fileService, IManifestService manifestService, ISnapService snapService,
        ILogger? logger = default)
    {
        if (Installation is not null)
        {
            await Unmount(fileService, manifestService, logger);

            // Uninstall systemd mount units
            var unitPaths =
                Directory.EnumerateFiles(Path.Join("/", "snap", Key, "current", "mounts"));
            foreach (var unitPath in unitPaths)
            {
                var unitName = unitPath.Split('/').Last();
                logger?.LogDebug($"Removing {unitName} from systemd directory.");
                fileService.UninstallSystemdMountUnit(unitName);
            }

            if (snapService.IsSnapInstalled(Key))
            {
                await snapService.Remove(Key, purge: true);
            }

            // Check for any empty directories
            fileService.RemoveEmptyDirectories(manifestService.DotnetInstallLocation);

            Installation = null;
            await manifestService.Remove(this);
        }
    }

    public async Task Mount(IFileService fileService, IManifestService manifestService, ILogger? logger = default)
    {
        var units = Directory.EnumerateFiles(
            Path.Join("/", "snap", Key, "current", "mounts"));

        foreach (var unit in units)
        {
            var unitName = unit.Split('/').Last();

            await Terminal.Invoke("systemctl", "enable", unitName);
            logger?.LogDebug($"Enabled {unitName}");
            await Terminal.Invoke("systemctl", "start", unitName);
            logger?.LogDebug($"Started {unitName}");
        }
    }

    public async Task Unmount(IFileService fileService, IManifestService manifestService, ILogger? logger = default)
    {
        var units = Directory.EnumerateFiles(
            Path.Join("/", "snap", Key, "current", "mounts"));

        foreach (var unit in units)
        {
            var unitName = unit.Split('/').Last();

            await Terminal.Invoke("systemctl", "disable", unitName);
            logger?.LogDebug($"Disabled {unitName}");
            await Terminal.Invoke("systemctl", "stop", unitName);
            logger?.LogDebug($"Unmounted {unitName}");
        }
    }
}
