using System.CommandLine;
using System.Text;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Console.Commands;

public class EnvironmentCommand : Command
{
    public EnvironmentCommand(IFileService fileService, IManifestService manifestService,
        ISystemdService systemdService, ILogger logger)
        : base("environment", "Gets information about the current environment.")
    {
        IsHidden = true;
        var snapOption = new Option<string>("--snap", "The snap to execute command against.")
        {
            Arity = ArgumentArity.ExactlyOne,
            IsRequired = false
        };
        var allOption = new Option<bool>(
            "--all",
            getDefaultValue: () => false,
            "Execute action on all snaps.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };

        var infoCommand = new Command("info", "Gets environment information.");
        infoCommand.SetHandler(HandleInfo);

        var mountCommand = new Command("mount", "Mounts all available .NET locations.");
        mountCommand.SetHandler(() => HandleMount(fileService, manifestService, systemdService, logger));

        var unmountCommand = new Command("unmount", "Unmounts all current .NET bind-mounts.");
        unmountCommand.SetHandler(() => HandleUnmount(fileService, manifestService, systemdService, logger));

        var placeUnitsCommand = new Command("place-units", "Installs all systemd-mount units in the system.")
        {
            snapOption,
            allOption
        };
        placeUnitsCommand.SetHandler((snapOptionValue, allOptionValue) =>
            HandlePlaceUnits(snapOptionValue, allOptionValue, fileService, manifestService, systemdService, logger),
                snapOption, allOption);

        var removeUnitsCommand = new Command("remove-units", "Removes all systemd-mount units from the system.")
        {
            snapOption,
            allOption
        };
        removeUnitsCommand.SetHandler((snapOptionValue, allOptionValue) =>
            HandleRemoveUnits(snapOptionValue, allOptionValue, fileService, manifestService, systemdService, logger),
                snapOption, allOption);

        AddCommand(infoCommand);
        AddCommand(mountCommand);
        AddCommand(unmountCommand);
        AddCommand(placeUnitsCommand);
        AddCommand(removeUnitsCommand);
    }

    private void HandleInfo()
    {
        var environment = new EnvironmentInformation
        {
            EffectiveUserId = Native.GetCurrentEffectiveUserId()
        };

        System.Console.Write(environment);
    }

    private async Task HandleMount(IFileService fileService, IManifestService manifestService,
        ISystemdService systemdService, ILogger logger)
    {
        await manifestService.Initialize();
        foreach (var installedComponent in manifestService.Local)
        {
            await installedComponent.Mount(manifestService, fileService, systemdService, logger);
        }
    }

    private async Task HandleUnmount(IFileService fileService, IManifestService manifestService,
        ISystemdService systemdService, ILogger logger)
    {
        await manifestService.Initialize();
        foreach (var installedComponent in manifestService.Local)
        {
            await installedComponent.Unmount(fileService, manifestService, systemdService, logger);
        }
    }

    private async Task HandlePlaceUnits(string snapName, bool allSnaps, IFileService fileService,
        IManifestService manifestService, ISystemdService systemdService, ILogger logger)
    {
        try
        {
            await manifestService.Initialize();
            var components = manifestService.Local.Where(c =>
                    c.Key.Equals(snapName, StringComparison.CurrentCultureIgnoreCase) || allSnaps)
                .ToList();

            logger.LogDebug($"Found {components.Count} snaps.");
            if (components.Count == 0 && !allSnaps)
            {
                throw new ApplicationException($"Snap {snapName} could not be found.");
            }

            foreach (var component in components)
            {
                await component.PlaceMountUnits(fileService, manifestService, systemdService, logger);
                logger.LogDebug($"Placed units from snap {component.Key}");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            Environment.Exit(-1);
        }
    }

    private async Task HandleRemoveUnits(string snapName, bool allSnaps, IFileService fileService,
        IManifestService manifestService, ISystemdService systemdService, ILogger logger)
    {
        try
        {
            await manifestService.Initialize();
            var components = manifestService.Local.Where(c =>
                    c.Key.Equals(snapName, StringComparison.CurrentCultureIgnoreCase) || allSnaps)
                .ToList();

            logger.LogDebug($"Found {components.Count} snaps.");
            if (components.Count == 0 && !allSnaps)
            {
                throw new ApplicationException($"Snap {snapName} could not be found.");
            }

            foreach (var component in components)
            {
                await component.RemoveMountUnits(fileService, manifestService, systemdService, logger);
                logger.LogDebug($"Removed units from snap {component.Key}");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            Environment.Exit(-1);
        }
    }

    private class EnvironmentInformation
    {
        public int EffectiveUserId { get; init; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Process+{nameof(EffectiveUserId)}={EffectiveUserId}");

            return stringBuilder.ToString();
        }
    }
}

