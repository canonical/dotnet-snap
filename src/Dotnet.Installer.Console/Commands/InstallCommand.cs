using System.CommandLine;
using Dotnet.Installer.Core.Exceptions;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;
using Spectre.Console;

namespace Dotnet.Installer.Console.Commands;

public class InstallCommand : Command
{
    private readonly IManifestService _manifestService;

    public InstallCommand(IManifestService manifestService)
        : base("install", "Installs a new .NET component in the system")
    {
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));

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

                Component? MatchVersion()
                {
                    if (string.IsNullOrWhiteSpace(version)) return default;
                    else if (version.Length == 1) // Major version only, e.g. install sdk 8
                    {
                        return _manifestService.Remote
                            .Where(c => 
                                c.LatestVersion.Major == int.Parse(version) &&
                                c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase))
                            .MaxBy(c => c.LatestVersion);
                    }
                    else if (version.Length == 3) // Major and minor version only, e.g install sdk 8.0
                    {
                        return _manifestService.Remote
                            .Where(c =>                                         // "8.0"
                                c.LatestVersion.Major == int.Parse(version[..1]) &&   // "8"
                                c.LatestVersion.Minor == int.Parse(version[2..3]) &&  // "0"
                                c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase))
                            .MaxBy(c => c.LatestVersion);
                    }

                    return _manifestService.Remote.FirstOrDefault(c =>
                        c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase) &&
                        c.LatestVersion.Equals(DotnetVersion.Parse(version), DotnetVersionComparison.IgnoreRevision));
                }

                var requestedComponent = version switch
                {
                    "latest" => _manifestService.Remote
                        .Where(c => c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase))
                        .MaxBy(c => c.LatestVersion),
                    _ => MatchVersion()
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
                        await requestedComponent.Install(_manifestService);
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
