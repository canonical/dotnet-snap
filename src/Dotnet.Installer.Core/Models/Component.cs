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

    public required string DotnetRoot { get; init; }
    [JsonPropertyName("mountpoints")]
    public required IEnumerable<string> MountPoints { get; set; }
    public required IEnumerable<string> Dependencies { get; init; }
    public Installation? Installation { get; set; }

    public event EventHandler<InstallationStartedEventArgs>? InstallationStarted;
    public event EventHandler<InstallationFinishedEventArgs>? InstallationFinished;

    public async Task Install(IFileService fileService, IManifestService manifestService, ISnapService snapService)
    {
        if (Installation is null)
        {
            InstallationStarted?.Invoke(this, new InstallationStartedEventArgs(Key));
            
            // Install content snap on the machine
            if (!snapService.IsSnapInstalled(Key))
            {
                var result = await snapService.Install(Key);
                if (!result.IsSuccess) throw new ApplicationException(result.StandardError);
            }
            
            // Iterate component's mount-points and bind-mount them where appropriate
            var dotnetRootAbsolute = Path.Join($"/snap/{Key}/current", DotnetRoot);
            var mountPoints = fileService.ResolveMountPoints(dotnetRootAbsolute, MountPoints);
            await fileService.ExecuteMountPoints(manifestService.DotnetInstallLocation, mountPoints);

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

            await component.Install(fileService, manifestService, snapService);
        }

        InstallationFinished?.Invoke(this, new InstallationFinishedEventArgs(Key));
    }

    public async Task Uninstall(IFileService fileService, IManifestService manifestService, ISnapService snapService)
    {
        if (Installation is not null)
        {
            // Unmount content directories
            var dotnetRootAbsolute = Path.Join($"/snap/{Key}/current", DotnetRoot);
            var mountPoints = fileService.ResolveMountPoints(dotnetRootAbsolute, MountPoints);
            await fileService.RemoveMountPoints(manifestService.DotnetInstallLocation, mountPoints);
            
            if (snapService.IsSnapInstalled(Key))
            {
                await snapService.Remove(Key, purge: true);
            }

            // Check for any empty directories
            fileService.RemoveEmptyDirectories(manifestService.DotnetInstallLocation);

            Installation = null;
            await manifestService.Remove(this);
        }
    }
}
