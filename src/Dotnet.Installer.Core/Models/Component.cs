using System.Runtime.InteropServices;
using Dotnet.Installer.Core.Exceptions;
using Dotnet.Installer.Core.Models.Events;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Models;

public class Component
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required Uri BaseUrl { get; init; }
    public required DotnetVersion Version { get; init; }
    public required IEnumerable<Package> Packages { get; init; }
    public required IEnumerable<string> Dependencies { get; init; }
    public Installation? Installation { get; set; }

    public event EventHandler<InstallationStartedEventArgs>? InstallationStarted;
    public event EventHandler<InstallationFinishedEventArgs>? InstallationFinished;
    public event EventHandler<InstallingPackageChangedEventArgs>? InstallingPackageChanged;

    public bool CanInstall(ILimitsService limitsService)
    {
        if (Version.IsRuntime)
        {
            return Version <= limitsService.Runtime;
        }

        if (Version.IsSdk)
        {
            var limitOnFeatureBand = limitsService.Sdk
                .First(v => v.FeatureBand == Version.FeatureBand);

            return Version <= limitOnFeatureBand;
        }

        return false;
    }

    public async Task Install(
        IFileService fileService,
        ILimitsService limitsService,
        IManifestService manifestService)
    {
        // TODO: Double-check architectures from Architecture enum
        var architecture = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "amd64",
            Architecture.Arm64 => "arm64",
            _ => throw new UnsupportedArchitectureException(RuntimeInformation.OSArchitecture)
        };

        if (!CanInstall(limitsService))
        {
            throw new VersionTooHighException(this,
                Version.IsRuntime ? limitsService.Runtime : limitsService.Sdk.First(v => v.FeatureBand == Version.FeatureBand));
        }

        if (Installation is null)
        {
            InstallationStarted?.Invoke(this, new InstallationStartedEventArgs(Key));
            
            // Install component packages
            foreach (var package in Packages)
            {
                InstallingPackageChanged?.Invoke(this, new InstallingPackageChangedEventArgs(package, this));
                
                var debUrl = new Uri(BaseUrl, $"{package.Name}_{package.Version}_{architecture}.deb");

                var filePath = await fileService.DownloadFile(debUrl, manifestService.DotnetInstallLocation);

                await fileService.ExtractDeb(filePath, manifestService.DotnetInstallLocation, manifestService.SnapConfigurationLocation);

                fileService.DeleteFile(filePath);
            }

            // Register the installation of this component in the local manifest file
            await manifestService.Add(this);
            
            InstallationFinished?.Invoke(this, new InstallationFinishedEventArgs(Key));
        }
        else
        {
            Console.WriteLine("{0} already installed!", Key);
        }
        
        foreach (var dependency in Dependencies)
        {
            var component = manifestService.Remote.First(c => c.Key == dependency);
            await component.Install(fileService, limitsService, manifestService);
        }
    }

    public async Task Uninstall(IFileService fileService, IManifestService manifestService)
    {
        if (Installation is not null)
        {
            foreach (var package in Packages)
            {
                var registrationFileName = Path.Combine(manifestService.SnapConfigurationLocation, 
                    $"{package.Name}.files");

                if (!fileService.FileExists(registrationFileName))
                {
                    throw new ApplicationException("Registration file does not exist!");
                }

                var files = await fileService.ReadAllLines(registrationFileName);
                foreach (var file in files)
                {
                    fileService.DeleteFile(file);
                }

                fileService.DeleteFile(registrationFileName);
            }

            // Check for any empty directories
            fileService.RemoveEmptyDirectories(manifestService.DotnetInstallLocation);

            Installation = null;
            await manifestService.Remove(this);
        }
    }
}
