using System.CommandLine;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Dotnet.Installer.Console.Binders;
using Dotnet.Installer.Domain;
using Dotnet.Installer.Domain.Types;

namespace Dotnet.Installer.Console.Verbs;

public class InstallVerb
{
    private readonly string? _installPath = Environment.GetEnvironmentVariable("DOTNET_ROOT");
    private readonly RootCommand _rootCommand;

    public InstallVerb(RootCommand rootCommand)
    {
        _rootCommand = rootCommand ?? throw new ArgumentNullException(nameof(rootCommand));
    }

    public void Initialize()
    {
        var installVerb = new Command("install", "Installs a new .NET component in the system");
        var componentArgument = new Argument<string>(
            name: "component",
            description: "The .NET component name to be installed (dotnet-runtime, aspnetcore-runtime, runtime, sdk).",
            getDefaultValue: () => "sdk"
        );
        var versionArgument = new Argument<string>(
            name: "version",
            description: "The .NET component version to be installed (version or latest).",
            getDefaultValue: () => "latest"
        );
        installVerb.AddArgument(componentArgument);
        installVerb.AddArgument(versionArgument);

        installVerb.SetHandler(Handle, componentArgument, versionArgument, new HttpClientBinder());

        _rootCommand.Add(installVerb);
    }

    private async Task Handle(string component, string version, HttpClient client)
    {
        if (_installPath is null)
        {
            System.Console.Error.WriteLine("Install path is empty");
            return;
        }

        if (Directory.Exists(_installPath))
        {
            var manifest = await GetManifest(client);

            if (manifest is null) return;
            
            var requestedVersion = DotnetVersion.Parse(version);
            var requestedComponent = manifest.FirstOrDefault(c => 
                c.Component.Equals(component, StringComparison.CurrentCultureIgnoreCase)
                && c.Version == requestedVersion);

            if (requestedComponent is null)
            {
                System.Console.Error.WriteLine("ERROR: The requested component {0} {1} does not exist.", 
                    component, version);
                return;
            }

            await ProcessComponent(manifest, requestedComponent, _installPath, client);

            return;
        }

        System.Console.Error.WriteLine("ERROR: The directory {0} does not exist", _installPath);
    }

    private async Task<IEnumerable<RemoteItem>?> GetManifest(HttpClient client)
    {
        var response = await client.GetAsync("manifest.json");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<IEnumerable<RemoteItem>>();

            return content;
        }

        return default;
    }

    private async Task ProcessComponent(IEnumerable<RemoteItem> manifest, RemoteItem item, 
        string installPath, HttpClient client)
    {
        var fileName = item.Url.Segments.Last();
        var filePath = Path.Combine(installPath, fileName);

        var shouldDownload = true;
        if (File.Exists(filePath))
        {
            System.Console.WriteLine("File already exists, comparing hash...");

            var hashString = await GetFileHash(filePath);

            if (hashString.Equals(item.Sha256))
            {
                System.Console.WriteLine("Hash matches!");
                shouldDownload = false;
            }
            else
            {
                System.Console.WriteLine("Hash does NOT match!");
                File.Delete(filePath);
            }
        }

        if (shouldDownload) await DownloadFile(client, item.Url, filePath);
        var hash = await GetFileHash(filePath);
        if (!hash.Equals(item.Sha256, StringComparison.CurrentCultureIgnoreCase))
        {
            System.Console.Error.WriteLine("ERROR: File hashes do not match.");
            return;
        }

        await ExtractFile(filePath, installPath);

        File.Delete(filePath);

        foreach (var dependency in item.Dependencies)
        {
            var component = manifest.First(c => c.Key == dependency);
            await ProcessComponent(manifest, component, installPath, client);
        }
    }

    private async Task<string> GetFileHash(string filepath)
    {
        using var readerStream = File.OpenRead(filepath);
        var result = await SHA256.HashDataAsync(readerStream);
        return Convert.ToHexString(result).ToLower();
    }

    private async Task DownloadFile(HttpClient client, Uri url, string destination)
    {
        await using var remoteFileStream = await client.GetStreamAsync(url);

        try
        {
            await using var writerStream = File.OpenWrite(destination);
            await remoteFileStream.CopyToAsync(writerStream);
        }
        catch (UnauthorizedAccessException)
        {
            System.Console.Error.WriteLine("ERROR: Unauthorized access. Maybe run with sudo?");
        }
    }

    private async Task ExtractFile(string filePath, string destinationDirectory)
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
