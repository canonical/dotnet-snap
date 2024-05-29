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

        this.SetHandler(Handle);
    }

    private async Task Handle()
    {
        try
        {
            await _manifestService.Initialize();
            var tree = new Tree("Available Components");

            foreach (var versionGroup in _manifestService.Merged.GroupBy(c => c.MajorVersion))
            {
                var majorVersionNode = tree.AddNode($".NET {versionGroup.Key}");

                foreach (var component in versionGroup)
                {
                    var stringBuilder = new StringBuilder();

                    stringBuilder.Append(component.Description);

                    if (component.Installation is not null)
                    {
                        stringBuilder.Append("[bold green]Installed :check_mark_button:[/]");
                    }
                    
                    majorVersionNode.AddNode($"{component.Description}");
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
