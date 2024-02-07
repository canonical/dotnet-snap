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
        var allOption = new Option<bool>(
            name: "--all",
            description: "Includes past .NET versions available to install"
        );
        listVerb.AddOption(allOption);

        listVerb.SetHandler(Handle, allOption);

        _rootCommand.Add(listVerb);
    }

    private static async Task Handle(bool allOption)
    {
        var manifest = await Manifest.Initialize(includeArchive: allOption);
        var tree = new Tree("Available Components");

        foreach (var versionGroup in manifest.Merged.GroupBy(c => c.Version.Major))
        {
            var majorVersionNode = tree.AddNode($".NET {versionGroup.Key}");
            
            foreach (var componentGroup in versionGroup.GroupBy(c => c.Name))
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append($"{componentGroup.Last().Description}:");

                var orderedComponents = componentGroup
                    .OrderBy(c => c.Version)
                    .ToList();
                
                foreach (var component in orderedComponents)
                {
                    if (component.Installation is not null)
                    {
                        stringBuilder.Append($" [[{component.Version}");
                        stringBuilder.Append(" [bold green]Installed :check_mark_button:[/]");
                        stringBuilder.Append("]]");
                    }
                    else if (component.Installation is null && orderedComponents.Count > 1)
                    {
                        stringBuilder.Append($" \u2192 [[{component.Version}");
                        stringBuilder.Append(" [bold yellow]Update available![/]");
                        stringBuilder.Append("]]");
                    }
                    else
                    {
                        stringBuilder.Append($" [[{component.Version}]]");
                    }
                }
                
                majorVersionNode.AddNode(stringBuilder.ToString());
            }
        }
        
        AnsiConsole.Write(tree);
    }
}
