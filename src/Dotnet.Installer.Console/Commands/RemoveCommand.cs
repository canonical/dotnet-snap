using System.CommandLine;
using System.Text;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;
using Spectre.Console;

namespace Dotnet.Installer.Console.Commands;

public class RemoveCommand : Command
{
    private readonly IFileService _fileService;
    private readonly IManifestService _manifestService;
    private readonly ISnapService _snapService;
    private readonly ISystemdService _systemdService;
    private readonly ILogger _logger;

    public RemoveCommand(
        IFileService fileService,
        IManifestService manifestService,
        ISnapService snapService,
        ISystemdService systemdService,
        ILogger logger)
        : base("remove", "Removes an installed .NET component from the system")
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _snapService = snapService ?? throw new ArgumentNullException(nameof(snapService));
        _systemdService = systemdService ?? throw new ArgumentNullException(nameof(systemdService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var componentArgument = new Argument<string>(
            name: "component",
            description: "The .NET component name to be removed ('runtime', 'aspnetcore-runtime', 'sdk')."
        );
        var versionArgument = new Argument<string>(
            name: "version",
            description: "The .NET component version to be removed (version (e.g. '8' or '8.0'), 'lts', 'latest')."
        );
        var yesOption = new Option<bool>(
            name: "--yes",
            description: "Say yes to all prompts")
        {
            IsRequired = false
        };

        AddArgument(componentArgument);
        AddArgument(versionArgument);
        AddOption(yesOption);

        this.SetHandler(Handle, componentArgument, versionArgument, yesOption);
    }

    private async Task Handle(string component, string version, bool yesOption)
    {
        try
        {
            if (!Directory.Exists(_manifestService.DotnetInstallLocation))
            {
                _logger.LogError($"The directory {_manifestService.DotnetInstallLocation} does not exist");
                Environment.Exit(-1);
            }

            await _manifestService.Initialize();

            var requestedComponent = _manifestService.MatchLocalComponent(component, version);

            if (requestedComponent is null)
            {
                _logger.LogError($"The requested component {component} {version} does not exist.");
                Environment.Exit(-1);
            }

            var dependencyTree = new DependencyTree(_manifestService.Local);
            var reverseDependencies =
                dependencyTree.GetReverseDependencies(requestedComponent.Key);

            if (reverseDependencies.Count != 0 && !yesOption)
            {
                var confirmationPrompt = new StringBuilder();
                confirmationPrompt.AppendLine("The component you are uninstalling also includes:");
                foreach (var reverseDependency in reverseDependencies)
                {
                    confirmationPrompt.AppendLine($"\t* {reverseDependency.Key}");
                }

                confirmationPrompt.AppendLine("Continue?");

                if (!AnsiConsole.Confirm(confirmationPrompt.ToString(), defaultValue: false))
                {
                    return;
                }
            }

            await requestedComponent.Uninstall(_fileService, _manifestService, _snapService, _systemdService, _logger);
            foreach (var reverseDependency in reverseDependencies)
            {
                await reverseDependency.Uninstall(_fileService, _manifestService, _snapService, _systemdService, _logger);
            }
        }
        catch (ApplicationException ex)
        {
            _logger.LogError(ex.Message);
            Environment.Exit(-1);
        }
    }
}
