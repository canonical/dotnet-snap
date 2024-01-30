using System.CommandLine;
using Dotnet.Installer.Domain.Models;
using Dotnet.Installer.Domain.Types;

namespace Dotnet.Installer.Console;

public class RemoveVerb
{
    private readonly string? _dotnetRootPath = Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR");
    private readonly RootCommand _rootCommand;

    public RemoveVerb(RootCommand rootCommand)
    {
        _rootCommand = rootCommand ?? throw new ArgumentNullException(nameof(rootCommand));
    }

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

    private async Task Handle(string component, string version)
    {
        if (_dotnetRootPath is null)
        {
            System.Console.Error.WriteLine("Install path is empty");
            return;
        }

        if (Directory.Exists(_dotnetRootPath))
        {
            var manifest = await Manifest.LoadLocal();

            if (manifest is null) return;
            
            var requestedVersion = DotnetVersion.Parse(version);
            var requestedComponent = manifest.FirstOrDefault(c => 
                c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase)
                && c.Version == requestedVersion);

            if (requestedComponent is null)
            {
                System.Console.Error.WriteLine("ERROR: The requested component {0} {1} does not exist.", 
                    component, version);
                return;
            }

            await requestedComponent.Uninstall(_dotnetRootPath);

            return;
        }

        System.Console.Error.WriteLine("ERROR: The directory {0} does not exist", _dotnetRootPath);
    }
}
