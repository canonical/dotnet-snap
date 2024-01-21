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
            var response = await client.GetAsync("manifest.json");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<IEnumerable<RemoteItem>>();

                if (content is not null)
                {
                    var requestedVersion = DotnetVersion.Parse(version);
                    var requestedComponent = content.FirstOrDefault(c => 
                        c.Component.Equals(component, StringComparison.CurrentCultureIgnoreCase)
                        && c.Version == requestedVersion);

                    if (requestedComponent is null)
                    {
                        System.Console.Error.WriteLine("ERROR: The requested component {0} {1} does not exist.", 
                            component, version);
                        return;
                    }

                    var fileName = requestedComponent.Url.Segments.Last();
                    var filePath = Path.Combine(_installPath, fileName);

                    var shouldDownload = true;
                    // if (File.Exists(filePath))
                    // {
                    //     System.Console.WriteLine("File already exists, comparing hash...");

                    //     var hashString = await GetFileHash(filePath);

                    //     if (hashString.Equals(requestedComponent.Sha256))
                    //     {
                    //         System.Console.WriteLine("Hash matches!");
                    //         shouldDownload = false;
                    //     }
                    //     else
                    //     {
                    //         System.Console.WriteLine("Hash does NOT match!");
                    //         File.Delete(filePath);
                    //     }
                    // }

                    if (shouldDownload) await DownloadFile(client, requestedComponent.Url, filePath);

                    await ExtractFile(filePath, _installPath);
                }
            }

            return;
        }

        System.Console.Error.WriteLine("ERROR: The directory {0} does not exist", _installPath);
    }

    // private async Task<string> GetFileHash(string filepath)
    // {
    //     using var readerStream = File.OpenRead(filepath);
    //     var result = await SHA256.HashDataAsync(readerStream);
    //     return Convert.ToHexString(result).ToLower();
    // }

    private async Task DownloadFile(HttpClient client, Uri url, string destination)
    {
        await using var remoteFileStream = await client.GetStreamAsync(url);
        await using var writerStream = File.OpenWrite(destination);
        await remoteFileStream.CopyToAsync(writerStream);
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
