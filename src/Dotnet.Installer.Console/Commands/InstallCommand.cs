using System.CommandLine;
using Dotnet.Installer.Core.Exceptions;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;
using Spectre.Console;

namespace Dotnet.Installer.Console.Commands;

public class InstallCommand : Command
{
    private readonly IFileService _fileService;
    private readonly ILimitsService _limitsService;
    private readonly IManifestService _manifestService;

    public InstallCommand(IFileService fileService, ILimitsService limitsService, IManifestService manifestService)
        : base("install", "Installs a new .NET component in the system")
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _limitsService = limitsService ?? throw new ArgumentNullException(nameof(limitsService));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));

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
        try
        {
            if (Directory.Exists(_manifestService.DotnetInstallLocation))
            {
                await _manifestService.Initialize(includeArchive: true);

                var requestedComponent = version switch
                {
                    "latest" => _manifestService.Remote
                        .Where(c => c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase))
                        .MaxBy(c => c.Version),
                    _ => _manifestService.Remote.FirstOrDefault(c =>
                        c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase) &&
                        c.Version.Equals(DotnetVersion.Parse(version), DotnetVersionComparison.IgnoreRevision))
                };

                if (requestedComponent is null)
                {
                    System.Console.Error.WriteLine("ERROR: The requested component {0} {1} does not exist.", 
                        component, version);
                    Environment.Exit(-1);
                }

                await AnsiConsole
                    .Status()
                    .Spinner(Spinner.Known.Dots12)
                    .StartAsync("Thinking...", async context =>
                    {
                        requestedComponent.InstallingPackageChanged += (sender, args) =>
                            context.Status($"Installing {args.Package.Name}");

                        await requestedComponent.Install(_fileService, _limitsService, _manifestService);
                    });

                return;
            }

            System.Console.Error.WriteLine("ERROR: The directory {0} does not exist", 
                _manifestService.DotnetInstallLocation);
            Environment.Exit(-1);
        }
        catch (ExceptionBase ex)
        {
            System.Console.Error.WriteLine("ERROR: " + ex.Message);
            Environment.Exit((int)ex.ErrorCode);
        }
    }
}
