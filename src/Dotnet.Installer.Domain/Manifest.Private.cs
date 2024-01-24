﻿using System.Text;
using System.Text.Json;

namespace Dotnet.Installer.Domain;

public static partial class Manifest
{
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