using System.CommandLine;
using System.Text;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Types;
using Spectre.Console;

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
        var yesOption = new Option<bool>(
            name: "--yes",
            description: "Say yes to all prompts")
        {
            IsRequired = false
        };
        
        removeVerb.AddArgument(componentArgument);
        removeVerb.AddArgument(versionArgument);
        removeVerb.AddOption(yesOption);

        removeVerb.SetHandler(Handle, componentArgument, versionArgument, yesOption);

        _rootCommand.Add(removeVerb);
    }

    private static async Task Handle(string component, string version, bool yesOption)
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

            var dependencyTree = new DependencyTree(manifest.Local);
            var reverseDependencies = 
                dependencyTree.GetReverseDependencies(requestedComponent.Key);

            if (reverseDependencies.Count != 0 && !yesOption)
            {
                var confirmationPrompt = new StringBuilder();
                confirmationPrompt.AppendLine("This will also remove:");
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

            await requestedComponent.Uninstall(manifest);
            foreach (var reverseDependency in reverseDependencies)
            {
                await reverseDependency.Uninstall(manifest);
            }

            return;
        }

        System.Console.Error.WriteLine("ERROR: The directory {0} does not exist", Manifest.DotnetInstallLocation);
    }
}
