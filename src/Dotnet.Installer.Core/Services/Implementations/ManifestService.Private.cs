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
        var supportedJsonFilePath = Path.Join("/", "snap", "dotnet-manifest", "current", "supported.json");
        if (!File.Exists(supportedJsonFilePath))
        {
            throw new ApplicationException("dotnet-manifest snap is not installed. Please run 'sudo snap install dotnet-manifest'.");
        }

        var contentStream = File.OpenRead(supportedJsonFilePath);
        var components = await JsonSerializer.DeserializeAsync<List<Component>>(
            contentStream, JsonSerializerOptions, cancellationToken);

        if (components is null)
        {
            throw new ApplicationException("Could not read the supported.json file.");
        }

        // TODO Add unsupported flag support

        return components;
    }

    private async Task Refresh(CancellationToken cancellationToken = default)
    {
        _local = await LoadLocal(cancellationToken);
        _remote = await LoadRemote(_includeArchive, cancellationToken);
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
