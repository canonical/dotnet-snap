using System.Text.Json.Serialization;
using Dotnet.Installer.Domain.Types;

namespace Dotnet.Installer.Domain.Models;

public partial class Component
{
    public required string Key { get; set; }

    public required string Name { get; set; }

    public required Uri Url { get; set; }

    public required string Sha256 { get; set; }

    [JsonConverter(typeof(DotnetVersionJsonConverter))]
    public required DotnetVersion Version { get; set; }
    
    public required IEnumerable<string> Locations { get; set; }

    public required IEnumerable<string> Dependencies { get; set; }

    public Installation? Installation { get; set; }

    public async Task Install(string dotnetRootPath)
    {
        if (Installation is null)
        {
            var fileName = Url.Segments.Last();
            var filePath = Path.Combine(dotnetRootPath, fileName);

            var shouldDownload = true;
            if (File.Exists(filePath))
            {
                Console.WriteLine("File already exists, comparing hash...");

                var hashString = await GetFileHash(filePath);

                if (hashString.Equals(Sha256))
                {
                    Console.WriteLine("Hash matches!");
                    shouldDownload = false;
                }
                else
                {
                    Console.WriteLine("Hash does NOT match!");
                    File.Delete(filePath);
                }
            }

            if (shouldDownload) await DownloadFile(Manifest.HttpClient, Url, filePath);
            var hash = await GetFileHash(filePath);
            if (!hash.Equals(Sha256, StringComparison.CurrentCultureIgnoreCase))
            {
                Console.Error.WriteLine("ERROR: File hashes do not match.");
                return;
            }

            await ExtractFile(filePath, dotnetRootPath);

            File.Delete(filePath);

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
            foreach (var directory in Locations)
            {
                var fullPath = Path.Join(dotnetRootPath, directory);
                Directory.Delete(fullPath, recursive: true);
            }

            Installation = null;
            await Manifest.Remove(this);
        }
    }
}
