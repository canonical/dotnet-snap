using System.CommandLine;
using Dotnet.Installer.Console.Verbs;

namespace Dotnet.Installer.Console;

class Program
{
    static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand(".NET command-line installer tool");

        // Verbs
        var installVerb = new InstallVerb(rootCommand);
        installVerb.Initialize();
        var updateVerb = new UpdateVerb(rootCommand);
        updateVerb.Initialize();
        var removeVerb = new RemoveVerb(rootCommand);
        removeVerb.Initialize();
        var listVerb = new ListVerb(rootCommand);
        listVerb.Initialize();

        await rootCommand.InvokeAsync(args);
    }
}