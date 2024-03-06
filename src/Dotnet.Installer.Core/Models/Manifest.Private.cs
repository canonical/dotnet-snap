using CliWrap;
using System.Text;
using System.Text.Json;

namespace Dotnet.Installer.Core.Models;

public partial class Manifest
{
    private static readonly string LocalManifestPath = 
        Path.Join(Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR"), "manifest.json");
    
    private static readonly string[] SupportedVersions = ["6.0", "7.0", "8.0"];
    
    private static readonly HttpClient HttpClient = new()
    {
        #if !DEBUG
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("SERVER_URL")
                              ?? throw new ApplicationException(
                                  "SERVER_URL environment variable is not defined."))
        #endif
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

    private static async Task<List<Component>> LoadRemote(bool includeArchive = false, CancellationToken cancellationToken = default)
    {
        var content = new List<Component>();

        #if DEBUG
        await Cli.Wrap("git")
            .WithArguments(["rev-parse", "--show-toplevel"])
            .WithStandardOutputPipe(PipeTarget.ToDelegate(path =>
            {
                var manifestLocation = Path.Join(path, "manifest", "latest.json");
                if (!File.Exists(manifestLocation)) return;
                var latest = JsonSerializer.Deserialize<List<Component>>(
                    File.ReadAllText(manifestLocation), JsonSerializerOptions);
                content.AddRange(latest ?? []);

                if (includeArchive)
                {
                    foreach (var version in SupportedVersions)
                    {
                        var versionArchiveLocation = Path.Join(path, "manifest", version, "archive.json");
                        if (!File.Exists(versionArchiveLocation)) return;
                        var versionArchive = JsonSerializer.Deserialize<List<Component>>(
                            File.ReadAllText(versionArchiveLocation), JsonSerializerOptions);
                        content.AddRange(versionArchive ?? []);
                    }
                }
            }))
            .ExecuteAsync(cancellationToken);
        #else
        var response = await HttpClient.GetAsync("latest.json", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var latest = await response.Content.ReadFromJsonAsync<List<Component>>(
                cancellationToken: cancellationToken);
            if (latest is not null) content.AddRange(latest);
        }

        if (includeArchive)
        {
            foreach (var version in SupportedVersions)
            {
                response = await HttpClient.GetAsync($"{version}/archive.json", cancellationToken);

                if (!response.IsSuccessStatusCode) continue;
                
                var archive =
                    await response.Content.ReadFromJsonAsync<List<Component>>(cancellationToken: cancellationToken);
                    
                if (archive is not null) content.AddRange(archive);
            }
        }
        #endif

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
