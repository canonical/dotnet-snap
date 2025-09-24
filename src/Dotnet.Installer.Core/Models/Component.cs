using System.Text;
using System.Text.Json.Serialization;
using Dotnet.Installer.Core.Models.Events;
using Dotnet.Installer.Core.Services.Contracts;

namespace Dotnet.Installer.Core.Models;

public class Component
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int MajorVersion { get; init; }
    public required bool IsLts { get; init; }
    public required bool IsStable { get; set; }
    [JsonPropertyName("eol")] public DateTime? EndOfLife { get; init; }
    public required IEnumerable<string> Dependencies { get; init; }
    public Installation? Installation { get; set; }
    public bool IsInstalled => Installation is not null;

    public event EventHandler<InstallationStartedEventArgs>? InstallationStarted;
    public event EventHandler<InstallationFinishedEventArgs>? InstallationFinished;

    public async Task Install(IFileService fileService, IManifestService manifestService, ISnapService snapService,
        ISystemdService systemdService, ILogger? logger = null)
    {
        if (IsInstalled)
        {
            logger?.LogInformation($"{Description} already installed!");
            return;
        }

        InstallationStarted?.Invoke(this, new InstallationStartedEventArgs(Key));

        // Install content snap on the machine
        if (!snapService.IsSnapInstalled(Key))
        {
            // Gather highest channel available
            var snapInfo = await snapService.FindSnap(Key);

            var channel = snapInfo?.Channel switch
            {
                "candidate" => SnapChannel.Candidate,
                "beta" => SnapChannel.Beta,
                "edge" => SnapChannel.Edge,
                _ => SnapChannel.Stable
            };

            var result = await snapService.Install(Key, channel);
            if (!result.IsSuccess) throw new ApplicationException(result.StandardError);
        }

        // Place linking file in the content snap's $SNAP_COMMON
        await fileService.PlaceLinkageFile(Key);

        // Install Systemd mount units
        await PlaceMountUnits(fileService, manifestService, systemdService, logger);

        // Install update watcher unit
        await PlacePathUnits(fileService, systemdService, logger);

        // Register the installation of this component in the local manifest file
        await manifestService.Add(this);

        foreach (var dependency in Dependencies)
        {
            var component = manifestService.Remote.First(c => c.Key == dependency);
            await component.Install(fileService, manifestService, snapService, systemdService, logger);
        }

        InstallationFinished?.Invoke(this, new InstallationFinishedEventArgs(Key));
    }

    public async Task Uninstall(IFileService fileService, IManifestService manifestService, ISnapService snapService,
        ISystemdService systemdService, ILogger? logger = default)
    {
        if (IsInstalled)
        {
            // Uninstall systemd mount units
            await RemoveMountUnits(fileService, manifestService, systemdService, logger);

            // Uninstall systemd path units
            await RemovePathUnits(fileService, systemdService, logger);

            if (snapService.IsSnapInstalled(Key))
            {
                await snapService.Remove(Key, purge: true);
            }

            Installation = null;
            await manifestService.Remove(this);
        }
    }

    public async Task PlaceMountUnits(IFileService fileService, IManifestService manifestService,
        ISystemdService systemdService, ILogger? logger = default)
    {
        var units = new StringBuilder();
        var unitPaths = fileService.EnumerateContentSnapMountFiles(Key);

        foreach (var unitPath in unitPaths)
        {
            logger?.LogDebug($"Copying {unitPath} to systemd directory.");
            fileService.InstallSystemdMountUnit(unitPath);
            units.AppendLine(unitPath.Split('/').Last());
        }

        // Save unit names to component .mounts file
        await fileService.PlaceUnitsFile(manifestService.SnapConfigurationLocation, contentSnapName: Key,
            units.ToString());

        var result = await systemdService.DaemonReload();
        if (!result.IsSuccess)
        {
            throw new ApplicationException("Could not reload systemd daemon");
        }
        await Mount(manifestService, fileService, systemdService, logger);
    }

    public async Task RemoveMountUnits(IFileService fileService, IManifestService manifestService,
        ISystemdService systemdService, ILogger? logger = default)
    {
        await Unmount(fileService, manifestService, systemdService, logger);

        var units = await fileService.ReadUnitsFile(manifestService.SnapConfigurationLocation, Key);

        foreach (var unit in units)
        {
            logger?.LogDebug($"Removing {unit} from systemd directory.");
            fileService.UninstallSystemdMountUnit(unit);
        }

        fileService.DeleteUnitsFile(manifestService.SnapConfigurationLocation, Key);

        var result = await systemdService.DaemonReload();
        if (!result.IsSuccess)
        {
            throw new ApplicationException("Could not reload systemd daemon");
        }
    }

    public async Task Mount(IManifestService manifestService, IFileService fileService, ISystemdService systemdService,
        ILogger? logger = default)
    {
        var units = await fileService.ReadUnitsFile(manifestService.SnapConfigurationLocation, Key);

        foreach (var unit in units)
        {
            var result = await systemdService.EnableUnit(unit);
            if (!result.IsSuccess)
            {
                throw new ApplicationException($"Could not enable unit {unit}");
            }
            logger?.LogDebug($"Enabled {unit}");

            result = await systemdService.StartUnit(unit);
            if (!result.IsSuccess)
            {
                throw new ApplicationException($"Could not start unit {unit}");
            }
            logger?.LogDebug($"Started {unit}");

            logger?.LogDebug($"Finished mounting {unit}");
        }
    }

    public async Task Unmount(IFileService fileService, IManifestService manifestService,
        ISystemdService systemdService, ILogger? logger = default)
    {
        var units = await fileService.ReadUnitsFile(manifestService.SnapConfigurationLocation, Key);

        foreach (var unit in units)
        {
            var result = await systemdService.DisableUnit(unit);
            if (!result.IsSuccess)
            {
                throw new ApplicationException($"Could not disable unit {unit}");
            }
            logger?.LogDebug($"Disabled {unit}");

            result = await systemdService.StopUnit(unit);
            if (!result.IsSuccess)
            {
                throw new ApplicationException($"Could not stop unit {unit}");
            }
            logger?.LogDebug($"Stopped {unit}");

            logger?.LogDebug($"Finished unmounting {unit}");
        }

        // Check for any empty directories
        fileService.RemoveEmptyDirectories(manifestService.DotnetInstallLocation);
        logger?.LogDebug("Removed empty directories.");
    }

    private async Task PlacePathUnits(IFileService fileService, ISystemdService systemdService, ILogger? logger = null)
    {
        fileService.InstallSystemdPathUnit(Key);
        logger?.LogDebug($"Placed upgrade watcher path and service units for snap {Key}");

        var result = await systemdService.DaemonReload();
        if (!result.IsSuccess)
        {
            throw new ApplicationException("Could not reload systemd daemon");
        }

        result = await systemdService.EnableUnit($"{Key}-update-watcher.path");
        if (!result.IsSuccess)
        {
            throw new ApplicationException($"Could not enable {Key}-update-watcher.path");
        }
        logger?.LogDebug($"Enabled {Key}-update-watcher.path");

        result = await systemdService.StartUnit($"{Key}-update-watcher.path");
        if (!result.IsSuccess)
        {
            throw new ApplicationException($"Could not start {Key}-update-watcher.path");
        }
        logger?.LogDebug($"Started {Key}-update-watcher.path");
    }

    private async Task RemovePathUnits(IFileService fileService, ISystemdService systemdService,
        ILogger? logger = default)
    {
        var result = await systemdService.DisableUnit($"{Key}-update-watcher.path");
        if (!result.IsSuccess)
        {
            throw new ApplicationException($"Could not disable {Key}-update-watcher.path");
        }
        logger?.LogDebug($"Disabled {Key}-update-watcher.path");

        result = await systemdService.StopUnit($"{Key}-update-watcher.path");
        if (!result.IsSuccess)
        {
            throw new ApplicationException($"Could not stop {Key}-update-watcher.path");
        }
        logger?.LogDebug($"Stopped {Key}-update-watcher.path");

        fileService.UninstallSystemdPathUnit(Key);
        logger?.LogDebug($"Removed upgrade watcher path and service units for snap {Key}");

        result = await systemdService.DaemonReload();
        if (!result.IsSuccess)
        {
            throw new ApplicationException("Could not reload systemd daemon");
        }
    }
}
