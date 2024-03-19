using System.CommandLine;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;
using Spectre.Console;

namespace Dotnet.Installer.Console.Verbs;

public class InstallCommand : Command
{
    private readonly IManifestService _manifestService;
    private readonly ILimitsService _limitsService;

    public InstallCommand(IManifestService manifestService, ILimitsService limitsService)
        : base("install", "Installs a new .NET component in the system")
    {
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _limitsService = limitsService ?? throw new ArgumentNullException(nameof(limitsService));

        var componentArgument = new Argument<string>(
            name: "component",
            description: "The .NET component name to be installed (dotnet-runtime, aspnetcore-runtime, runtime, sdk).",
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
        if (Directory.Exists(_manifestService.DotnetInstallLocation))
        {
            await _manifestService.Initialize(includeArchive: true);
            
            var requestedComponent = default(Component);
            switch (version)
            {
                case "latest":
                    requestedComponent = _manifestService.Remote
                        .Where(c => c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase))
                        .OrderByDescending(c => c.Version)
                        .FirstOrDefault();
                    break;
                default:
                    requestedComponent = _manifestService.Remote.FirstOrDefault(c => 
                        c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase)
                        && c.Version == DotnetVersion.Parse(version));
                    break;
            }

            if (requestedComponent is null)
            {
                System.Console.Error.WriteLine("ERROR: The requested component {0} {1} does not exist.", 
                    component, version);
                return;
            }

            await AnsiConsole
                .Status()
                .Spinner(Spinner.Known.Dots12)
                .StartAsync("Thinking...", async context =>
                {
                    requestedComponent.InstallingPackageChanged += (sender, package) =>
                        context.Status($"Installing {package.Name}");

                    await requestedComponent.Install(_manifestService, _limitsService);
                });

            return;
        }

        System.Console.Error.WriteLine("ERROR: The directory {0} does not exist", 
            _manifestService.DotnetInstallLocation);
    }
}
