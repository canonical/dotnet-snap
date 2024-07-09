using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
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
            Log.Debug($"Waiting for debugger on PID {Environment.ProcessId}...");
            while (!Debugger.IsAttached)
            {
                // Wait for debugger to be attached.
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            Log.Debug("Debugger attached");
        }
        
        var fileService = new FileService();
        var manifestService = new ManifestService();
        var snapService = new SnapService();
        
        var rootCommand = new RootCommand(".NET Installer command-line tool")
        {
            new EnvironmentCommand(fileService, manifestService),
            new ListCommand(manifestService),
            new InstallCommand(fileService, manifestService, snapService),
            new RemoveCommand(fileService, manifestService, snapService)
        };

        var verboseOption = new Option<bool>("--verbose", "Enables debug output level verbosity.");
        rootCommand.AddGlobalOption(verboseOption);

        var commandLineBuilder = new CommandLineBuilder(rootCommand);

        commandLineBuilder.AddMiddleware(async (context, next) =>
        {
            if (context.ParseResult.Tokens.Any(t => t.Value == "--verbose"))
            {
                System.Console.WriteLine("Enabled verbose output.");
                Trace.Listeners.Add(new TextWriterTraceListener(System.Console.Out));
            }

            await next(context);
        });

        commandLineBuilder.UseDefaults();
        var parser = commandLineBuilder.Build();
        await parser.InvokeAsync(args);
    }
}
