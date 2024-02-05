using Dotnet.Installer.Core.Models;
using System.CommandLine;
using Spectre.Console;

namespace Dotnet.Installer.Console.Verbs;

public class ListVerb(RootCommand rootCommand)
{
    private readonly RootCommand _rootCommand = rootCommand ?? throw new ArgumentNullException(nameof(rootCommand));

    public void Initialize()
    {
        var listVerb = new Command("list", "List installed and available .NET versions");
        var availableOption = new Option<bool>(
            name: "--available",
            description: "Lists all available .NET versions to install"
        );
        listVerb.AddOption(availableOption);

        listVerb.SetHandler(Handle, availableOption);

        _rootCommand.Add(listVerb);
    }

    private async Task Handle(bool availableOptionValue)
    {
        var manifest = availableOptionValue
            ? await Manifest.Load()
            : await Manifest.LoadLocal();

        var treeTitle = availableOptionValue ? "Available Components" : "Installed Components";
        var tree = new Tree(treeTitle);

        foreach (var item in manifest.GroupBy(c => c.Version.Major))
        {
            var majorVersionNode = tree.AddNode($".NET {item.Key}");

            foreach (var component in item)
            {
                majorVersionNode.AddNode($"{component.Description}: {component.Version}");
            }
        }
        
        AnsiConsole.Write(tree);
    }
}
