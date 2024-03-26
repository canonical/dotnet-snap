using System.CommandLine;
using Dotnet.Installer.Console.Commands;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnet.Installer.Console;

class Program
{
    static async Task Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IFileService, FileService>()
            .AddSingleton<IManifestService, ManifestService>()
            .AddSingleton<ILimitsService, LimitsService>()
            .AddSingleton(serviceProvider =>
            {
                var fileService = serviceProvider.GetRequiredService<IFileService>();
                var manifestService = serviceProvider.GetRequiredService<IManifestService>();
                var limitsService = serviceProvider.GetRequiredService<ILimitsService>();
                return new RootCommand(".NET Installer command-line tool")
                {
                    new ListCommand(manifestService),
                    new InstallCommand(fileService, limitsService, manifestService),
                    new RemoveCommand(fileService, manifestService),
                    new UpdateCommand(fileService, limitsService, manifestService)
                };
            })
            .BuildServiceProvider();

        var rootCommand = serviceProvider.GetRequiredService<RootCommand>();

        await rootCommand.InvokeAsync(args);
    }
}
