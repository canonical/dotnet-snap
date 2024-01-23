using System.CommandLine;
using Dotnet.Installer.Domain;

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
        if (availableOptionValue)
        {
            var manifest = await Manifest.LoadRemote();
            foreach (var component in manifest)
            {
                System.Console.WriteLine($"{component.Name}: {component.Version}");
            }
        }
    }
}
