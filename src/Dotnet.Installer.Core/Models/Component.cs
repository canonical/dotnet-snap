using System.Text;
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
    public required IEnumerable<string> Dependencies { get; init; }
    public Installation? Installation { get; set; }

    [JsonIgnore]
    private string MountsFileName => $"{Key}.mounts";

    public event EventHandler<InstallationStartedEventArgs>? InstallationStarted;
    public event EventHandler<InstallationFinishedEventArgs>? InstallationFinished;

    public async Task Install(IFileService fileService, IManifestService manifestService, ISnapService snapService,
        ILogger? logger = default)
    {
        if (Installation is null)
        {
            InstallationStarted?.Invoke(this, new InstallationStartedEventArgs(Key));

            if (!CanInstall(manifestService, out var componentKeyToRemove))
            {
                logger?.LogInformation($"A component that contains {Description} is already installed.");
                return;
            }

            if (componentKeyToRemove is not null)
            {
                logger?.LogDebug($"Removing component {componentKeyToRemove} before installing {Key}.");
                var componentToRemove = manifestService.Local.First(x => x.Key == componentKeyToRemove);
                await componentToRemove.Uninstall(fileService, manifestService, snapService, logger);
            }

            // Install content snap on the machine
            if (!snapService.IsSnapInstalled(Key))
            {
                var result = await snapService.Install(Key);
                if (!result.IsSuccess) throw new ApplicationException(result.StandardError);
            }

            // Place linking file in the content snap's $SNAP_COMMON
            await File.WriteAllTextAsync(
                Path.Join("/", "var", "snap", Key, "common", "dotnet-installer"),
                "installer linkage ok\n",
                Encoding.UTF8);

            // Install Systemd mount units
            await PlaceMountUnits(fileService, manifestService, logger);

            // Install update watcher unit
            await PlacePathUnits(fileService, logger);

            // Register the installation of this component in the local manifest file
            await manifestService.Add(this);

            InstallationFinished?.Invoke(this, new InstallationFinishedEventArgs(Key));
        }
        else
        {
            logger?.LogInformation($"{Key} already installed!");
        }
    }

    public async Task Uninstall(IFileService fileService, IManifestService manifestService, ISnapService snapService,
        ILogger? logger = default)
    {
        if (Installation is not null)
        {
            // Uninstall systemd mount units
            await RemoveMountUnits(fileService, manifestService, logger);

            // Uninstall systemd path units
            await RemovePathUnits(fileService, logger);

            if (snapService.IsSnapInstalled(Key))
            {
                await snapService.Remove(Key, purge: true);
            }

            Installation = null;
            await manifestService.Remove(this);
        }
    }

    public async Task PlaceMountUnits(IFileService fileService, IManifestService manifestService, ILogger? logger = default)
    {
        var units = new StringBuilder();
        var unitPaths =
            Directory.EnumerateFiles(Path.Join("/", "snap", Key, "current", "mounts"));

        foreach (var unitPath in unitPaths)
        {
            logger?.LogDebug($"Copying {unitPath} to systemd directory.");
            fileService.InstallSystemdMountUnit(unitPath);
            units.AppendLine(unitPath.Split('/').Last());
        }

        // Save unit names to component .mounts file
        await File.WriteAllTextAsync(
            Path.Join(manifestService.SnapConfigurationLocation, MountsFileName),
            units.ToString(),
            Encoding.UTF8);

        var result = await Terminal.Invoke("systemctl", "daemon-reload");
        if (result != 0)
        {
            throw new ApplicationException("Could not reload systemd daemon");
        }
        await Mount(fileService, manifestService, logger);
    }

    public async Task RemoveMountUnits(IFileService fileService, IManifestService manifestService,
        ILogger? logger = default)
    {
        await Unmount(fileService, manifestService, logger);

        var units = await File.ReadAllLinesAsync(
            Path.Join(manifestService.SnapConfigurationLocation, MountsFileName));

        foreach (var unit in units)
        {
            logger?.LogDebug($"Removing {unit} from systemd directory.");
            fileService.UninstallSystemdMountUnit(unit);
        }

        File.Delete(Path.Join(manifestService.SnapConfigurationLocation, MountsFileName));

        var result = await Terminal.Invoke("systemctl", "daemon-reload");
        if (result != 0)
        {
            throw new ApplicationException("Could not reload systemd daemon");
        }
    }

    public async Task Mount(IFileService fileService, IManifestService manifestService, ILogger? logger = default)
    {
        var units = await File.ReadAllLinesAsync(
            Path.Join(manifestService.SnapConfigurationLocation, MountsFileName));

        foreach (var unit in units)
        {
            var result = await Terminal.Invoke("systemctl", "enable", unit);
            if (result != 0)
            {
                throw new ApplicationException($"Could not enable unit {unit}");
            }
            logger?.LogDebug($"Enabled {unit}");

            result = await Terminal.Invoke("systemctl", "start", unit);
            if (result != 0)
            {
                throw new ApplicationException($"Could not start unit {unit}");
            }
            logger?.LogDebug($"Started {unit}");

            logger?.LogDebug($"Finished mounting {unit}");
        }
    }

    public async Task Unmount(IFileService fileService, IManifestService manifestService, ILogger? logger = default)
    {
        var units = await File.ReadAllLinesAsync(
            Path.Join(manifestService.SnapConfigurationLocation, MountsFileName));

        foreach (var unit in units)
        {
            var result = await Terminal.Invoke("systemctl", "disable", unit);
            if (result != 0)
            {
                throw new ApplicationException($"Could not disable unit {unit}");
            }
            logger?.LogDebug($"Disabled {unit}");

            result = await Terminal.Invoke("systemctl", "stop", unit);
            if (result != 0)
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

    private async Task PlacePathUnits(IFileService fileService, ILogger? logger = default)
    {
        fileService.InstallSystemdPathUnit(Key);
        logger?.LogDebug($"Placed upgrade watcher path and service units for snap {Key}");

        var result = await Terminal.Invoke("systemctl", "daemon-reload");
        if (result != 0)
        {
            throw new ApplicationException("Could not reload systemd daemon");
        }

        result = await Terminal.Invoke("systemctl", "enable", $"{Key}-update-watcher.path");
        if (result != 0)
        {
            throw new ApplicationException($"Could not enable {Key}-update-watcher.path");
        }
        logger?.LogDebug($"Enabled {Key}-update-watcher.path");

        result = await Terminal.Invoke("systemctl", "start", $"{Key}-update-watcher.path");
        if (result != 0)
        {
            throw new ApplicationException($"Could not start {Key}-update-watcher.path");
        }
        logger?.LogDebug($"Started {Key}-update-watcher.path");
    }

    private async Task RemovePathUnits(IFileService fileService, ILogger? logger = default)
    {
        var result = await Terminal.Invoke("systemctl", "disable", $"{Key}-update-watcher.path");
        if (result != 0)
        {
            throw new ApplicationException($"Could not disable {Key}-update-watcher.path");
        }
        logger?.LogDebug($"Disabled {Key}-update-watcher.path");

        result = await Terminal.Invoke("systemctl", "stop", $"{Key}-update-watcher.path");
        if (result != 0)
        {
            throw new ApplicationException($"Could not stop {Key}-update-watcher.path");
        }
        logger?.LogDebug($"Stopped {Key}-update-watcher.path");

        fileService.UninstallSystemdPathUnit(Key);
        logger?.LogDebug($"Removed upgrade watcher path and service units for snap {Key}");

        result = await Terminal.Invoke("systemctl", "daemon-reload");
        if (result != 0)
        {
            throw new ApplicationException("Could not reload systemd daemon");
        }
    }

    /// <summary>
    /// Determines whether a component can be installed based on the components already installed.
    /// The return value of this function indicates whether this component can be installed or not. It also includes
    /// a <c>componentKeyToRemove</c> out-parameter that indicates whether a component must be removed before installing
    /// this component.
    /// </summary>
    /// <param name="manifestService">The manifest service.</param>
    /// <param name="componentKeyToRemove">The key of the component to remove.</param>
    /// <param name="logger">A logger object.</param>
    /// <returns>Whether the current component can be installed.</returns>
    private bool CanInstall(IManifestService manifestService, out string? componentKeyToRemove)
    {
        componentKeyToRemove = null;

        // We will always only have on component per major version installed by design.
        // So, FirstOrDefault() should either return that one component or none.
        var installedComponentOfSameMajorVersion = manifestService.Local.FirstOrDefault(
            c => c.MajorVersion == MajorVersion);

        // We don't have any component of that major version installed.
        if (installedComponentOfSameMajorVersion is null)
        {
            return true;
        }

        // Get all the dependencies of the installed component.
        // If this component is there, then the installed component is higher in the chain.
        var dependenciesOfInstalledComponent =
            installedComponentOfSameMajorVersion.GetAllDependencies(manifestService);

        if (dependenciesOfInstalledComponent.Contains(Key))
        {
            return false;
        }

        componentKeyToRemove = installedComponentOfSameMajorVersion.Key;
        return true;
    }

    /// <summary>
    /// Gets all the dependencies of the current component, as well as all the dependencies of the dependencies
    /// of the current component all the way down to the last component in the chain recursively.
    /// </summary>
    /// <param name="manifestService">The manifest service.</param>
    /// <returns>A list of the component keys of all dependencies of this component and their dependencies.</returns>
    /// <exception cref="ApplicationException">When a key of dependent component is not found in the manifest.</exception>
    private IEnumerable<string> GetAllDependencies(IManifestService manifestService)
    {
        var result = new HashSet<string>();
        foreach (var dependency in Dependencies)
        {
            result.Add(dependency);
            var component = manifestService.Merged.FirstOrDefault(c => c.Key.Equals(dependency));

            if (component is null)
            {
                throw new ApplicationException($"Could not find dependency component {dependency}");
            }

            foreach (var subDependency in component.GetAllDependencies(manifestService))
                result.Add(subDependency);
        }

        return result;
    }
}
