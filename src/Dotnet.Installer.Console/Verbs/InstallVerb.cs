using System.CommandLine;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Types;
using Spectre.Console;

namespace Dotnet.Installer.Console.Verbs;

public class InstallVerb(RootCommand rootCommand)
{
    private readonly RootCommand _rootCommand = rootCommand ?? throw new ArgumentNullException(nameof(rootCommand));

    public void Initialize()
    {
        var installVerb = new Command("install", "Installs a new .NET component in the system");
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
        installVerb.AddArgument(componentArgument);
        installVerb.AddArgument(versionArgument);

        installVerb.SetHandler(Handle, componentArgument, versionArgument);

        _rootCommand.Add(installVerb);
    }

    private static async Task Handle(string component, string version)
    {
        if (Directory.Exists(Manifest.DotnetInstallLocation))
        {
            var manifest = await Manifest.Initialize(includeArchive: true);
            
            var requestedComponent = default(Component);
            switch (version)
            {
                case "latest":
                    requestedComponent = manifest.Remote
                        .Where(c => c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase))
                        .OrderByDescending(c => c.Version)
                        .FirstOrDefault();
                    break;
                default:
                    requestedComponent = manifest.Remote.FirstOrDefault(c => 
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

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots12)
                .StartAsync("Thinking...", async context =>
                {
                    requestedComponent.InstallingPackageChanged += (sender, package) =>
                        context.Status($"Installing {package}");

                    await requestedComponent.Install(manifest);
                });

            return;
        }

        System.Console.Error.WriteLine("ERROR: The directory {0} does not exist", Manifest.DotnetInstallLocation);
    }
}
