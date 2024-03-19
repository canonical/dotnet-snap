using System.CommandLine;
using System.Text;
using Spectre.Console;
using Dotnet.Installer.Core.Services.Contracts;

namespace Dotnet.Installer.Console.Verbs;

public class ListCommand : Command
{
    private readonly IManifestService _manifestService;

    public ListCommand(IManifestService manifestService) : base("list", "List installed and available .NET versions")
    {
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));

        var listVerb = new Command("list", "List installed and available .NET versions");
        var allOption = new Option<bool>(
            name: "--all",
            description: "Includes past .NET versions available to install"
        );

        AddOption(allOption);
        this.SetHandler(Handle, allOption);
    }

    private async Task Handle(bool allOption)
    {
        await _manifestService.Initialize(includeArchive: allOption);
        var tree = new Tree("Available Components");

        foreach (var versionGroup in _manifestService.Merged.GroupBy(c => c.Version.Major))
        {
            var majorVersionNode = tree.AddNode($".NET {versionGroup.Key}");
            
            foreach (var componentGroup in versionGroup.GroupBy(c => c.Name))
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append($"{componentGroup.Last().Description}:");

                var orderedComponents = componentGroup
                    .OrderBy(c => c.Version)
                    .ToList();

                var componentHasPreviousVersionInstalled = false;
                foreach (var component in orderedComponents)
                {
                    if (component.Installation is not null)
                    {
                        componentHasPreviousVersionInstalled = true;
                        stringBuilder.Append($" [[{component.Version}");
                        stringBuilder.Append(" [bold green]Installed :check_mark_button:[/]");
                        stringBuilder.Append("]]");
                    }
                    else if (component.Installation is null && orderedComponents.Count > 1 && componentHasPreviousVersionInstalled)
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
