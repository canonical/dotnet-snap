using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Dotnet.Installer.Core.Models;

public partial class Manifest
{
    private static readonly string LocalManifestPath = 
        Path.Join(Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR"), "manifest.json");
    
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

    private static async Task<List<Component>> LoadRemote(bool latestOnly = true, CancellationToken cancellationToken = default)
    {
        var content = new List<Component>();
        var response = await HttpClient.GetAsync("latest.json", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var latest = await response.Content.ReadFromJsonAsync<List<Component>>(
                cancellationToken: cancellationToken);
            if (latest is not null) content.AddRange(latest);
        }

        if (!latestOnly)
        {
            // TODO: Implement archive manifest support
        }

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
