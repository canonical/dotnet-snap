using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Dotnet.Installer.Domain;

public static class Manifest
{
    private readonly static string _localManifestPath;
    private readonly static HttpClient _httpClient;

    static Manifest()
    {
        _localManifestPath = Path.Join(
            Environment.GetEnvironmentVariable("DOTNET_ROOT"),
            "manifest.json"
        );
        _httpClient = new HttpClient
        {
            // BaseAddress = new Uri("http://10.83.58.1:3000/")
            BaseAddress = new Uri("http://localhost:3000/")
        };
    }

    public static async Task<IEnumerable<Component>> Load(CancellationToken cancellationToken = default)
    {
        var localManifest = await LoadLocal(cancellationToken);
        var remoteManifest = await LoadRemote(cancellationToken);

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

    public static async Task<IEnumerable<Component>> LoadRemote(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("manifest.json", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<IEnumerable<Component>>(
                cancellationToken: cancellationToken);

            return content ?? Array.Empty<Component>();
        }

        return Array.Empty<Component>();
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

    private static Task Save(IEnumerable<Component> components, CancellationToken cancellationToken = default)
    {
        using var sw = new StreamWriter(_localManifestPath, append: false, Encoding.UTF8);
        var content = JsonSerializer.Serialize(components, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        var stringBuilder = new StringBuilder(content);
        return sw.WriteLineAsync(stringBuilder, cancellationToken);
    }

    private static IEnumerable<Component> Merge(IEnumerable<Component> remoteComponents,
        IEnumerable<Component> localComponents)
    {
        var result = new List<Component>();
        result.AddRange(remoteComponents);

        foreach (var localComponent in localComponents)
        {
            if (!result.Any(c => c.Key == localComponent.Key))
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
}
