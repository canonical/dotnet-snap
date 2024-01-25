using System.Net.Http.Json;
using System.Text.Json;

namespace Dotnet.Installer.Domain.Models;

public static partial class Manifest
{
    private readonly static string _localManifestPath;
    public static HttpClient HttpClient { get; private set; }

    static Manifest()
    {
        _localManifestPath = Path.Join(
            Environment.GetEnvironmentVariable("DOTNET_ROOT"),
            "manifest.json"
        );

        var serverUrl = Environment.GetEnvironmentVariable("SERVER_URL") 
            ?? throw new ApplicationException("SERVER_URL environment variable is not defined.");
        
        HttpClient = new HttpClient
        {
            BaseAddress = new Uri(serverUrl)
        };
    }

    public static async Task<IEnumerable<Component>> Load(CancellationToken cancellationToken = default)
    {
        var localManifest = await LoadLocal(cancellationToken);
        var remoteManifest = await LoadRemote(cancellationToken: cancellationToken);

        return Merge(remoteManifest, localManifest);
    }

    public static async Task<IEnumerable<Component>> LoadLocal(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_localManifestPath))
        {
            using var fs = File.OpenRead(_localManifestPath);
            var result = await JsonSerializer.DeserializeAsync<IEnumerable<Component>>(
                fs, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                },
                cancellationToken
            );

            return result ?? Array.Empty<Component>();
        }

        return Array.Empty<Component>();
    }

    public static async Task<IEnumerable<Component>> LoadRemote(bool latestOnly = true, CancellationToken cancellationToken = default)
    {
        var content = new List<Component>();
        var response = await HttpClient.GetAsync("latest.json", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var latest = await response.Content.ReadFromJsonAsync<IEnumerable<Component>>(
                cancellationToken: cancellationToken);
            if (latest is not null) content.AddRange(latest);
        }

        if (!latestOnly)
        {
            // TODO: Implement archive manifest support
        }

        return content;
    }

    public static async Task Add(Component component, CancellationToken cancellationToken = default)
    {
        var localManifest = (await LoadLocal(cancellationToken)).ToList();
        component.Installation = new Installation
        {
            InstalledAt = DateTimeOffset.UtcNow
        };
        localManifest.Add(component);
        await Save(localManifest, cancellationToken);
    }

    public static async Task Remove(Component component, CancellationToken cancellationToken = default)
    {
        var localManifest = (await LoadLocal(cancellationToken)).ToList();
        var componentToRemove = localManifest.FirstOrDefault(c => c.Key == component.Key);
        if (componentToRemove is not null)
        {
            localManifest.Remove(componentToRemove);
        }
        await Save(localManifest, cancellationToken);
    }
}
