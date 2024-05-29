using System.Text.Json.Serialization;
using Dotnet.Installer.Core.Models.Events;
using Dotnet.Installer.Core.Services.Contracts;

namespace Dotnet.Installer.Core.Models;

public class Component
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int MajorVersion { get; init; }
    public required bool IsLts { get; init; }
    [JsonPropertyName("eol")]
    public required DateTime EndOfLife { get; init; }
    public required IEnumerable<string> Dependencies { get; init; }
    public Installation? Installation { get; set; }

    public event EventHandler<InstallationStartedEventArgs>? InstallationStarted;
    public event EventHandler<InstallationFinishedEventArgs>? InstallationFinished;

    public async Task Install(IManifestService manifestService)
    {
        if (Installation is null)
        {
            InstallationStarted?.Invoke(this, new InstallationStartedEventArgs(Key));
            
            // Install component packages
            // TODO snap install <Key>

            // Register the installation of this component in the local manifest file
            await manifestService.Add(this);
        }
        else
        {
            Console.WriteLine("{0} already installed!", Key);
        }
        
        foreach (var dependency in Dependencies)
        {
            var component = manifestService.Remote.First(c => c.Key == dependency);
            
            component.InstallationStarted += InstallationStarted;
            component.InstallationFinished += InstallationFinished;

            await component.Install(manifestService);
        }

        InstallationFinished?.Invoke(this, new InstallationFinishedEventArgs(Key));
    }

    public async Task Uninstall(IFileService fileService, IManifestService manifestService)
    {
        if (Installation is not null)
        {
            // TODO snap remove --purge <Key>

            // Check for any empty directories
            fileService.RemoveEmptyDirectories(manifestService.DotnetInstallLocation);

            Installation = null;
            await manifestService.Remove(this);
        }
    }
}
