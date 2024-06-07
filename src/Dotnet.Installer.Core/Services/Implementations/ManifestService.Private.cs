using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Dotnet.Installer.Core.Models;

namespace Dotnet.Installer.Core.Services.Implementations;

public partial class ManifestService
{
    private static readonly string SnapConfigPath = Path.Join(
        Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR"), "..", "snap");

    private static readonly string LocalManifestPath = Path.Join(SnapConfigPath, "manifest.json");
    
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("SERVER_URL")
                              ?? throw new ApplicationException(
                                  "SERVER_URL environment variable is not defined."))
    };

    private static async Task<List<Component>> LoadLocal(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(LocalManifestPath)) return [];

        await using var fs = File.OpenRead(LocalManifestPath);
        var result = await JsonSerializer.DeserializeAsync<List<Component>>(
            fs, JsonSerializerOptions, cancellationToken
        );

        return result ?? [];
    }

    private static async Task<List<Component>> LoadRemote(bool includeUnsupported = false, 
        CancellationToken cancellationToken = default)
    {
        var content = new List<Component>();
        
        var response = await HttpClient.GetAsync("supported.json", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var latest = await response.Content.ReadFromJsonAsync<List<Component>>(
                cancellationToken: cancellationToken);
            if (latest is not null) content.AddRange(latest);
        }

        // TODO Add unsupported flag support
        // if (includeUnsupported)
        // {
        //     foreach (var version in SupportedVersions)
        //     {
        //         response = await HttpClient.GetAsync($"{version}/archive.json", cancellationToken);
        //
        //         if (!response.IsSuccessStatusCode) continue;
        //         
        //         var archive =
        //             await response.Content.ReadFromJsonAsync<List<Component>>(cancellationToken: cancellationToken);
        //             
        //         if (archive is not null) content.AddRange(archive);
        //     }
        // }

        return content;
    }

    private static List<Component> Merge(List<Component> remoteComponents, List<Component> localComponents)
    {
        var result = new List<Component>();
        result.AddRange(remoteComponents);

        foreach (var localComponent in localComponents)
        {
            if (result.All(c => c.Key != localComponent.Key))
            {
                result.Add(localComponent);
            }
            else
            {
                var remote = result.First(c => c.Key == localComponent.Key);
                remote.Installation = localComponent.Installation;
            }
        }

        return result;
    }
    
    private async Task Save(CancellationToken cancellationToken = default)
    {
        await using var sw = new StreamWriter(LocalManifestPath, append: false, Encoding.UTF8);
        var content = JsonSerializer.Serialize(_local, JsonSerializerOptions);
        var stringBuilder = new StringBuilder(content);
        await sw.WriteLineAsync(stringBuilder, cancellationToken);
    }
}
