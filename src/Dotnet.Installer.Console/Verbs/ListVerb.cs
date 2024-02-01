using System.CommandLine;
using ConsoleTables;
using Dotnet.Installer.Core.Models;

namespace Dotnet.Installer.Console.Verbs;

public class ListVerb
{
    private readonly RootCommand _rootCommand;

    public ListVerb(RootCommand rootCommand)
    {
        _rootCommand = rootCommand ?? throw new ArgumentNullException(nameof(rootCommand));
    }

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
        IEnumerable<Component> manifest;
        if (availableOptionValue)
        {
            manifest = await Manifest.Load();
        }
        else
        {
            manifest = await Manifest.LoadLocal();
        }

        var table = new ConsoleTable("Component", "Version", "Installed")
            .Configure(c => c.EnableCount = false);

        foreach (var component in manifest)
        {
            table.AddRow(
                component.Name,
                component.Version,
                component.Installation is null ? "" : "Yes");
        }
        table.Write(Format.Default);
    }
}
