﻿using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using Dotnet.Installer.Console.Commands;
using Dotnet.Installer.Console.Services.Implementations;
using Dotnet.Installer.Core.Services.Implementations;
using Serilog;
using Serilog.Events;

namespace Dotnet.Installer.Console;

class Program
{
    private static async Task Main(string[] args)
    {
#if DEBUG
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
#endif
        var fileService = new FileService();
        var manifestService = new ManifestService();
        using var snapService = new SnapService();
        var systemDService = new SystemdService();
        var logger = new Logger();

        var rootCommand = new RootCommand(".NET Installer command-line tool")
        {
            new EnvironmentCommand(fileService, manifestService, systemDService, logger),
            new ListCommand(fileService, manifestService, snapService, logger),
            new InstallCommand(fileService, manifestService, snapService, systemDService, logger),
            new RemoveCommand(fileService, manifestService, snapService, systemDService, logger)
        };

        var verboseOption = new Option<bool>("--verbose", "Enables debug output level verbosity.");
        rootCommand.AddGlobalOption(verboseOption);

        var commandLineBuilder = new CommandLineBuilder(rootCommand);

        commandLineBuilder.AddMiddleware(async (context, next) =>
        {
            var isVerbose = context.ParseResult.Tokens.Any(t => t.Value == "--verbose");

            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(isVerbose ? LogEventLevel.Verbose : LogEventLevel.Information)
                .WriteTo.Console()
                .CreateLogger();

            await next(context);
        });

        commandLineBuilder.UseDefaults();
        var parser = commandLineBuilder.Build();
        await parser.InvokeAsync(args);

        await Serilog.Log.CloseAndFlushAsync();
    }
}
