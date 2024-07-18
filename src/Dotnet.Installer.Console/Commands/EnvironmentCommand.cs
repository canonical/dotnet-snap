using System.CommandLine;
using System.Text;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Console.Commands;

public class EnvironmentCommand : Command
{
    public EnvironmentCommand(IFileService fileService, IManifestService manifestService, ILogger logger)
        : base("environment", "Gets information about the current environment.")
    {
        IsHidden = true;

        var infoCommand = new Command("info", "Gets environment information.");
        infoCommand.SetHandler(HandleInfo);

        var mountCommand = new Command("mount", "Mounts all available .NET locations.");
        mountCommand.SetHandler(() => HandleMount(fileService, manifestService, logger));

        var unmountCommand = new Command("unmount", "Unmounts all current .NET bind-mounts.");
        unmountCommand.SetHandler(() => HandleUnmount(fileService, manifestService, logger));

        AddCommand(infoCommand);
        AddCommand(mountCommand);
        AddCommand(unmountCommand);
    }

    private void HandleInfo()
    {
        var environment = new EnvironmentInformation
        {
            EffectiveUserId = Native.GetCurrentEffectiveUserId()
        };

        System.Console.Write(environment);
    }

    private async Task HandleMount(IFileService fileService, IManifestService manifestService, ILogger logger)
    {
        await manifestService.Initialize();
        foreach (var installedComponent in manifestService.Local)
        {
            await installedComponent.Mount(fileService, manifestService, logger);
        }
    }

    private async Task HandleUnmount(IFileService fileService, IManifestService manifestService, ILogger logger)
    {
        await manifestService.Initialize();
        foreach (var installedComponent in manifestService.Local)
        {
            await installedComponent.Unmount(fileService, manifestService, logger);
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

