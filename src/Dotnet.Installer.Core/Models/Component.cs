using System.Runtime.InteropServices;
using Dotnet.Installer.Core.Helpers;
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
    public event EventHandler<string>? InstallingPackageChanged; 

    private async Task<bool> CanInstall()
    {
        var limits = await Limits.Load();

        if (Version.IsRuntime)
        {
            return Version <= limits.Runtime;
        }

        if (Version.IsSdk)
        {
            var limitOnFeatureBand = limits.Sdk
                .First(v => v.FeatureBand == Version.FeatureBand);

            return Version <= limitOnFeatureBand;
        }

        return false;
    }

    public async Task Install(Manifest manifest)
    {
        // TODO: Double-check architectures from Architecture enum
        var architecture = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "amd64",
            Architecture.Arm64 => "arm64",
            _ => throw new InvalidOperationException("Unsupported architecture")
        };

        if (!await CanInstall())
        {
            Console.WriteLine("The component {0} {1} cannot be installed.", Name, Version);
            return;
        }

        if (Installation is null)
        {
            InstallationStarted?.Invoke(this, EventArgs.Empty);
            
            foreach (var package in Packages)
            {
                InstallingPackageChanged?.Invoke(this, package.Name);
                
                var debUrl = new Uri(BaseUrl, $"{package.Name}_{package.Version}_{architecture}.deb");

                var filePath = await FileHandler.DownloadFile(debUrl, Manifest.DotnetInstallLocation);

                await FileHandler.ExtractDeb(filePath, Manifest.DotnetInstallLocation);

                File.Delete(filePath);
            }

            // Register the installation of this component in the local manifest file
            await manifest.Add(this);
            
            InstallationFinished?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Console.WriteLine("{0} already installed!", Key);
        }
        
        foreach (var dependency in Dependencies)
        {
            var component = manifest.Remote.First(c => c.Key == dependency);
            await component.Install(manifest);
        }
    }

    public async Task Uninstall(Manifest manifest)
    {
        if (Installation is not null)
        {
            foreach (var package in Packages)
            {
                var registrationFileName = Path.Combine(Manifest.DotnetInstallLocation, $"{package.Name}.files");

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
            DirectoryHandler.RemoveEmptyDirectories(Manifest.DotnetInstallLocation);

            Installation = null;
            await manifest.Remove(this);
        }
    }
}
