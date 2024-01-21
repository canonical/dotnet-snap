using System.CommandLine;
using System.Net.Http.Json;
using Dotnet.Installer.Console.Binders;
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

        listVerb.SetHandler(Handle, availableOption, new HttpClientBinder());

        _rootCommand.Add(listVerb);
    }

    private async Task Handle(bool availableOptionValue, HttpClient httpClient)
    {
        if (availableOptionValue)
        {
            var response = await httpClient.GetAsync("manifest.json");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<IEnumerable<RemoteItem>>();

                if (content is not null)
                {
                    foreach (var version in content)
                    {
                        System.Console.WriteLine($"{version.Component}: {version.Version}");
                    }
                }
            }
        }
    }
}
