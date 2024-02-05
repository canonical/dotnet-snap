using System.Text;
using System.Text.Json;

namespace Dotnet.Installer.Core.Models;

public static partial class Manifest
{
    private static readonly string LocalManifestPath;
    private static readonly HttpClient HttpClient;

    private static async Task Save(IEnumerable<Component> components, CancellationToken cancellationToken = default)
    {
        await using var sw = new StreamWriter(LocalManifestPath, append: false, Encoding.UTF8);
        var content = JsonSerializer.Serialize(components, JsonSerializerOptions);
        var stringBuilder = new StringBuilder(content);
        await sw.WriteLineAsync(stringBuilder, cancellationToken);
    }

    private static IEnumerable<Component> Merge(IEnumerable<Component> remoteComponents,
        IEnumerable<Component> localComponents)
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
}
