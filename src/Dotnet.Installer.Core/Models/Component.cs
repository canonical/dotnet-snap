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
    public required string DotnetRoot { get; init; }
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

            // Install content snap on the machine
            if (!snapService.IsSnapInstalled(Key))
            {
                var result = await snapService.Install(Key);
                if (!result.IsSuccess) throw new ApplicationException(result.StandardError);
            }

            // Install Systemd mount units
            await PlaceUnits(fileService, manifestService, logger);

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
            // Uninstall systemd mount units
            await RemoveUnits(fileService, manifestService, logger);

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

    public async Task PlaceUnits(IFileService fileService, IManifestService manifestService, ILogger? logger = default)
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

        await Terminal.Invoke("systemctl", "daemon-reload");
        await Mount(fileService, manifestService, logger);
    }

    public async Task RemoveUnits(IFileService fileService, IManifestService manifestService, ILogger? logger = default)
    {
        await Unmount(fileService, manifestService, logger);

        var units = await File.ReadAllLinesAsync(
            Path.Join(manifestService.SnapConfigurationLocation, MountsFileName));

        foreach (var unit in units)
        {
            logger?.LogDebug($"Removing {unit} from systemd directory.");
            fileService.UninstallSystemdMountUnit(unit);
        }

        await Terminal.Invoke("systemctl", "daemon-reload");
        File.Delete(Path.Join(manifestService.SnapConfigurationLocation, MountsFileName));
    }

    public async Task Mount(IFileService fileService, IManifestService manifestService, ILogger? logger = default)
    {
        var units = await File.ReadAllLinesAsync(
            Path.Join(manifestService.SnapConfigurationLocation, MountsFileName));

        foreach (var unit in units)
        {
            await Terminal.Invoke("systemctl", "enable", unit);
            logger?.LogDebug($"Enabled {unit}");
            await Terminal.Invoke("systemctl", "start", unit);
            logger?.LogDebug($"Started {unit}");
        }
    }

    public async Task Unmount(IFileService fileService, IManifestService manifestService, ILogger? logger = default)
    {
        var units = await File.ReadAllLinesAsync(
            Path.Join(manifestService.SnapConfigurationLocation, MountsFileName));

        foreach (var unit in units)
        {
            await Terminal.Invoke("systemctl", "disable", unit);
            logger?.LogDebug($"Disabled {unit}");
            await Terminal.Invoke("systemctl", "stop", unit);
            logger?.LogDebug($"Unmounted {unit}");
        }
    }
}
