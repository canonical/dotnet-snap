using System.CommandLine;
using Dotnet.Installer.Core.Services.Contracts;

namespace Dotnet.Installer.Console.Commands;

public class InstallCommand : Command
{
    private readonly IFileService _fileService;
    private readonly IManifestService _manifestService;
    private readonly ISnapService _snapService;
    private readonly ISystemDService _systemDService;
    private readonly ILogger _logger;

    public InstallCommand(
        IFileService fileService,
        IManifestService manifestService,
        ISnapService snapService,
        ISystemDService systemDService,
        ILogger logger)
        : base("install", "Installs a new .NET component in the system")
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _snapService = snapService ?? throw new ArgumentNullException(nameof(snapService));
        _systemDService = systemDService ?? throw new ArgumentNullException(nameof(systemDService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var componentArgument = new Argument<string>(
            name: "component",
            description: "The .NET component name to be installed (dotnet-runtime, aspnetcore-runtime, sdk).",
            getDefaultValue: () => "sdk"
        );
        var versionArgument = new Argument<string>(
            name: "version",
            description: "The .NET component version to be installed (version or latest).",
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
                await _manifestService.Initialize(includeArchive: true);

                var requestedComponent = version switch
                {
                    "latest" => _manifestService.Remote
                        .Where(c => c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase))
                        .MaxBy(c => c.MajorVersion),
                    _ => _manifestService.MatchVersion(component, version)
                };

                if (requestedComponent is null)
                {
                    _logger.LogError($"The requested component {component} {version} does not exist.");
                    Environment.Exit(-1);
                }

                await requestedComponent.Install(_fileService, _manifestService, _snapService, _systemDService,
                    isRootComponent: true, _logger);

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
