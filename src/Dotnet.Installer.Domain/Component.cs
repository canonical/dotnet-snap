using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Dotnet.Installer.Domain.Types;

namespace Dotnet.Installer.Domain;

public class Component
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
    
    private static async Task<string> GetFileHash(string filepath)
    {
        using var readerStream = File.OpenRead(filepath);
        var result = await SHA256.HashDataAsync(readerStream);
        return Convert.ToHexString(result).ToLower();
    }

    private static async Task DownloadFile(HttpClient client, Uri url, string destination)
    {
        await using var remoteFileStream = await client.GetStreamAsync(url);

        try
        {
            await using var writerStream = File.OpenWrite(destination);
            await remoteFileStream.CopyToAsync(writerStream);
        }
        catch (UnauthorizedAccessException)
        {
            Console.Error.WriteLine("ERROR: Unauthorized access. Maybe run with sudo?");
        }
    }

    private static async Task ExtractFile(string filePath, string destinationDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/tar",
            Arguments = $"xzf {filePath} -C {destinationDirectory}",
            RedirectStandardInput = false,
            CreateNoWindow = true
        };

        var process = Process.Start(psi);

        await process!.WaitForExitAsync();
    }
}

public class Installation
{
    public DateTimeOffset InstalledAt { get; set; }
}
