using System.CommandLine;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Console.Verbs;

public class RemoveVerb(RootCommand rootCommand)
{
    private readonly RootCommand _rootCommand = rootCommand ?? throw new ArgumentNullException(nameof(rootCommand));

    public void Initialize()
    {
        var removeVerb = new Command("remove", "Removes an installed .NET component from the system");
        var componentArgument = new Argument<string>(
            name: "component",
            description: "The .NET component name to be removed (dotnet-runtime, aspnetcore-runtime, runtime, sdk)."
        );
        var versionArgument = new Argument<string>(
            name: "version",
            description: "The .NET component version to be removed (version)."
        );
        removeVerb.AddArgument(componentArgument);
        removeVerb.AddArgument(versionArgument);

        removeVerb.SetHandler(Handle, componentArgument, versionArgument);

        _rootCommand.Add(removeVerb);
    }

    private static async Task Handle(string component, string version)
    {
        if (Directory.Exists(Manifest.DotnetInstallLocation))
        {
            var manifest = await Manifest.Initialize();

            var requestedVersion = DotnetVersion.Parse(version);
            var requestedComponent = manifest.Local.FirstOrDefault(c => 
                c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase)
                && c.Version == requestedVersion);

            if (requestedComponent is null)
            {
                System.Console.Error.WriteLine("ERROR: The requested component {0} {1} does not exist.", 
                    component, version);
                return;
            }

            await requestedComponent.Uninstall(manifest);

            return;
        }

        System.Console.Error.WriteLine("ERROR: The directory {0} does not exist", Manifest.DotnetInstallLocation);
    }
}
