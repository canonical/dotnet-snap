using System.CommandLine;
using System.Diagnostics;
using Dotnet.Installer.Console.Commands;
using Dotnet.Installer.Core.Services.Implementations;

namespace Dotnet.Installer.Console;

class Program
{
    private static async Task Main(string[] args)
    {
        var debugEnabled = Environment.GetEnvironmentVariable("DOTNET_INSTALLER_DEBUG")?.Equals("1") ?? false;
        if (debugEnabled)
        {
            Log.Debug("Waiting for debugger...");
            while (!Debugger.IsAttached)
            {
                // Wait for debugger to be attached.
            }
            Log.Debug("Debugger attached");
        }
        
        var fileService = new FileService();
        var manifestService = new ManifestService();
        var snapService = new SnapService();
        
        var rootCommand = new RootCommand(".NET Installer command-line tool")
        {
            new EnvironmentCommand(),
            new ListCommand(manifestService),
            new InstallCommand(fileService, manifestService, snapService),
            new RemoveCommand(fileService, manifestService, snapService)
        };

        await rootCommand.InvokeAsync(args);
    }
}
