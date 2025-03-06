using System.CommandLine;
using Dotnet.Installer.Core.Services.Contracts;

namespace Dotnet.Installer.Console.Commands;

public class InstallCommand : Command
{
    private readonly IFileService _fileService;
    private readonly IManifestService _manifestService;
    private readonly ISnapService _snapService;
    private readonly ISystemdService _systemdService;
    private readonly ILogger _logger;

    public InstallCommand(
        IFileService fileService,
        IManifestService manifestService,
        ISnapService snapService,
        ISystemdService systemdService,
        ILogger logger)
        : base("install", "Installs a new .NET component in the system")
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _snapService = snapService ?? throw new ArgumentNullException(nameof(snapService));
        _systemdService = systemdService ?? throw new ArgumentNullException(nameof(systemdService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var componentArgument = new Argument<string>(
            name: "component",
            description: "The .NET component name to be installed ('runtime', 'aspnetcore-runtime', 'sdk').",
            getDefaultValue: () => "sdk"
        );
        var versionArgument = new Argument<string>(
            name: "version",
            description: "The .NET component version to be installed (version (e.g. '8' or '8.0'), lts, latest).",
            getDefaultValue: () => "latest"
        );
        AddArgument(componentArgument);
        AddArgument(versionArgument);

        this.SetHandler(Handle, componentArgument, versionArgument);
    }

    private async Task Handle(string component, string version)
    {
        try
        {
            if (Directory.Exists(_manifestService.DotnetInstallLocation))
            {
                await _manifestService.Initialize(includeUnsupported: true);

                var requestedComponent = _manifestService.MatchRemoteComponent(component, version);

                if (requestedComponent is null)
                {
                    _logger.LogError($"The requested component {component} {version} does not exist.");
                    Environment.Exit(-1);
                }

                await requestedComponent.Install(
                    _fileService,
                    _manifestService,
                    _snapService,
                    _systemdService,
                    _logger);

                return;
            }

            _logger.LogError($"The directory {_manifestService.DotnetInstallLocation} does not exist");
            Environment.Exit(-1);
        }
        catch (ApplicationException ex)
        {
            _logger.LogError(ex.Message);
            Environment.Exit(-1);
        }
    }
}
