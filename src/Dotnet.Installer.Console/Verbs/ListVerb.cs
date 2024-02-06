using Dotnet.Installer.Core.Models;
using System.CommandLine;
using System.Text;
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

    private static async Task Handle(bool availableOptionValue)
    {
        var manifest = await Manifest.Initialize();
        var components = availableOptionValue
            ? manifest.Merged
            : manifest.Local;

        var treeTitle = availableOptionValue ? "Available Components" : "Installed Components";
        var tree = new Tree(treeTitle);

        foreach (var item in components.GroupBy(c => c.Version.Major))
        {
            var majorVersionNode = tree.AddNode($".NET {item.Key}");

            foreach (var component in item)
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append($"{component.Description}: {component.Version}");
                if (component.Installation is not null)
                {
                    stringBuilder.Append(" [bold green]Installed :check_mark_button:[/]");
                }
                
                majorVersionNode.AddNode(stringBuilder.ToString());
            }
        }
        
        AnsiConsole.Write(tree);
    }
}
