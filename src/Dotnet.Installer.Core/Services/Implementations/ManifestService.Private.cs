using System.Text;
using System.Text.Json;
using Dotnet.Installer.Core.Models;

namespace Dotnet.Installer.Core.Services.Implementations;

public partial class ManifestService
{
    private static readonly string SnapConfigPath = Path.Join(
        Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR"), "..", "snap");

    private static readonly string LocalManifestPath = Path.Join(SnapConfigPath, "manifest.json");

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
        var filesToRead = new List<string>
        {
            Path.Join("/", "snap", "dotnet-manifest", "current", "supported.json")
        };

        if (includeUnsupported)
            filesToRead.Add(Path.Join("/", "snap", "dotnet-manifest", "current", "unsupported.json"));

        var components = new List<Component>();
        foreach (var contentStream in filesToRead.Select(File.OpenRead))
        {
            var currentComponents = await JsonSerializer.DeserializeAsync<List<Component>>(
                contentStream,
                JsonSerializerOptions,
                cancellationToken: cancellationToken);

            if (currentComponents is not null)
            {
                components.AddRange(currentComponents);
            }
        }

        return components;
    }

    private async Task Refresh(CancellationToken cancellationToken = default)
    {
        _local = await LoadLocal(cancellationToken);
        _remote = await LoadRemote(_includeUnsupported, cancellationToken);
        _merged = Merge(_remote, _local);
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
