using System.CommandLine;
using Dotnet.Installer.Console.Commands;
using Dotnet.Installer.Core.Services.Implementations;

namespace Dotnet.Installer.Console;

class Program
{
    private static async Task Main(string[] args)
    {
        var fileService = new FileService();
        var manifestService = new ManifestService();
        
        var rootCommand = new RootCommand(".NET Installer command-line tool")
        {
            new ListCommand(manifestService),
            new InstallCommand(manifestService),
            new RemoveCommand(fileService, manifestService)
        };

        await rootCommand.InvokeAsync(args);
    }
}
