using System.CommandLine;
using System.Text;
using Dotnet.Installer.Core.Exceptions;
using Dotnet.Installer.Core.Services.Contracts;
using Spectre.Console;

namespace Dotnet.Installer.Console.Commands;

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
        try
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
                        var version = component.Version.ToString().Split('+').First();

                        if (component.Installation is not null)
                        {
                            componentHasPreviousVersionInstalled = true;
                            stringBuilder.Append($" [[{version}");
                            stringBuilder.Append(" [bold green]Installed :check_mark_button:[/]");
                            stringBuilder.Append("]]");
                        }
                        else if (component.Installation is null && orderedComponents.Count > 1 && componentHasPreviousVersionInstalled)
                        {
                            stringBuilder.Append($" \u2192 [[{version}");
                            stringBuilder.Append(" [bold yellow]Update available![/]");
                            stringBuilder.Append("]]");
                        }
                        else
                        {
                            stringBuilder.Append($" [[{version}]]");
                        }
                    }
                    
                    majorVersionNode.AddNode(stringBuilder.ToString());
                }
            }
            
            AnsiConsole.Write(tree);
        }
        catch (ExceptionBase ex)
        {
            System.Console.Error.WriteLine("ERROR: " + ex.Message);
            Environment.Exit((int)ex.ErrorCode);
        }
    }
}
