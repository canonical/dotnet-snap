using System.Runtime.InteropServices;
using Dotnet.Installer.Core.Helpers;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Models;

public class Component
{
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required Uri BaseUrl { get; set; }
    public required DotnetVersion Version { get; set; }
    public required IEnumerable<Package> Packages { get; set; }
    public required IEnumerable<string> Dependencies { get; set; }
    public Installation? Installation { get; set; }

    public event EventHandler? InstallationStarted;
    public event EventHandler? InstallationFinished;
    public event EventHandler<Package>? InstallingPackageChanged;

    private bool CanInstall(ILimitsService limitsService)
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

    public async Task Install(IManifestService manifestService, ILimitsService limitsService)
    {
        // TODO: Double-check architectures from Architecture enum
        var architecture = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "amd64",
            Architecture.Arm64 => "arm64",
            _ => throw new InvalidOperationException("Unsupported architecture")
        };

        if (!CanInstall(limitsService))
        {
            Console.WriteLine("The component {0} {1} cannot be installed.", Name, Version);
            return;
        }

        if (Installation is null)
        {
            InstallationStarted?.Invoke(this, EventArgs.Empty);

            // If this component already has a previous version installed
            // within the major version/feature band group, uninstall it.
            var previousComponent = manifestService.Local
                .FirstOrDefault(c => c.Name.Equals(Name, StringComparison.CurrentCultureIgnoreCase)
                    && c.Version.IsRuntime ? c.Version < Version :
                        c.Version.FeatureBand == Version.FeatureBand && c.Version < Version);

            if (previousComponent is not null)
            {
                await previousComponent.Uninstall(manifestService);
            }
            
            // Install component packages
            foreach (var package in Packages)
            {
                InstallingPackageChanged?.Invoke(this, package);
                
                var debUrl = new Uri(BaseUrl, $"{package.Name}_{package.Version}_{architecture}.deb");

                var filePath = await FileHandler.DownloadFile(debUrl, manifestService.DotnetInstallLocation);

                await FileHandler.ExtractDeb(filePath, manifestService.DotnetInstallLocation);

                File.Delete(filePath);
            }

            // Register the installation of this component in the local manifest file
            await manifestService.Add(this);
            
            InstallationFinished?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Console.WriteLine("{0} already installed!", Key);
        }
        
        foreach (var dependency in Dependencies)
        {
            var component = manifestService.Remote.First(c => c.Key == dependency);
            await component.Install(manifestService, limitsService);
        }
    }

    public async Task Uninstall(IManifestService manifestService)
    {
        if (Installation is not null)
        {
            foreach (var package in Packages)
            {
                var registrationFileName = Path.Combine(manifestService.DotnetInstallLocation, 
                    $"{package.Name}.files");

                if (!File.Exists(registrationFileName))
                {
                    throw new ApplicationException("Registration file does not exist!");
                }

                var files = await File.ReadAllLinesAsync(registrationFileName);
                foreach (var file in files)
                {
                    File.Delete(file);
                }

                File.Delete(registrationFileName);
            }

            // Check for any empty directories
            DirectoryHandler.RemoveEmptyDirectories(manifestService.DotnetInstallLocation);

            Installation = null;
            await manifestService.Remove(this);
        }
    }
}
