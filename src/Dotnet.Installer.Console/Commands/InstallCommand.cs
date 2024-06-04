using System.CommandLine;
using Dotnet.Installer.Core.Exceptions;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Services.Contracts;
using Spectre.Console;

namespace Dotnet.Installer.Console.Commands;

public class InstallCommand : Command
{
    private readonly IFileService _fileService;
    private readonly IManifestService _manifestService;
    private readonly ISnapService _snapService;

    public InstallCommand(IFileService fileService, IManifestService manifestService, ISnapService snapService)
        : base("install", "Installs a new .NET component in the system")
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _snapService = snapService ?? throw new ArgumentNullException(nameof(snapService));

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
                    _ => MatchVersion(component, version)
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
                        await requestedComponent.Install(_fileService, _manifestService, _snapService);
                    });

                return;
            }

            System.Console.Error.WriteLine("ERROR: The directory {0} does not exist", 
                _manifestService.DotnetInstallLocation);
            Environment.Exit(-1);
        }
        catch (ExceptionBase ex)
        {
            await System.Console.Error.WriteLineAsync("ERROR: " + ex.Message);
            Environment.Exit((int)ex.ErrorCode);
        }
    }
    
    private Component? MatchVersion(string component, string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return default;

        return version.Length switch
        {
            // Major version only, e.g. install sdk 8
            1 => _manifestService.Remote
                .Where(c => c.MajorVersion == int.Parse(version) &&
                            c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase))
                .MaxBy(c => c.MajorVersion),
                        
            // Major and minor version only, e.g. install sdk 8.0
            3 => _manifestService.Remote.Where(c => // "8.0"
                    c.MajorVersion == int.Parse(version[..1]) &&
                    c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase))
                .MaxBy(c => c.MajorVersion),
                        
            _ => default
        };
    }
}
