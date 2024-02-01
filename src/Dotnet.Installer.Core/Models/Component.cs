using System.Text.Json.Serialization;
using Dotnet.Installer.Core.Enums;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Models;

public class Component
{
    public required string Key { get; set; }

    public required string Name { get; set; }

    public required Uri BaseUrl { get; set; }

    [JsonConverter(typeof(DotnetVersionJsonConverter))]
    public required DotnetVersion Version { get; set; }

    [JsonPropertyName("arch")]
    [JsonConverter(typeof(StringToArchitectureJsonConverter))]
    public required Architecture Architecture { get; set; }
    
    public required IEnumerable<Package> Packages { get; set; }

    public required IEnumerable<string> Dependencies { get; set; }

    public Installation? Installation { get; set; }

    public async Task Install(string dotnetRootPath)
    {
        if (Installation is null)
        {
            foreach (var package in Packages)
            {
                var debUrl = new Uri(BaseUrl, $"{package.Name}_{package.Version}_{Architecture.ToString().ToLower()}.deb");

                var filePath = await FileHandler.DownloadFile(debUrl, dotnetRootPath);

                await FileHandler.ExtractDeb(filePath, dotnetRootPath);

                File.Delete(filePath);
            }

            // Register the installation of this component in the local manifest file
            await Manifest.Add(this);
        }
        else
        {
            Console.WriteLine("{0} already installed!", Key);
        }

        var manifest = await Manifest.Load();
        foreach (var dependency in Dependencies)
        {
            var component = manifest.First(c => c.Key == dependency);
            await component.Install(dotnetRootPath);
        }
    }

    public async Task Uninstall(string dotnetRootPath)
    {
        if (Installation is not null)
        {
            foreach (var package in Packages)
            {
                var registrationFileName = Path.Combine(dotnetRootPath, $"{package.Name}.files");

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
            DirectoryHandler.RemoveEmptyDirectories(dotnetRootPath);

            Installation = null;
            await Manifest.Remove(this);
        }
    }
}
