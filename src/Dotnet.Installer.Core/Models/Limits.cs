using System.Text.Json;
using System.Text.Json.Serialization;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Models;

public class Limits
{
    [JsonIgnore]
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public required DotnetVersion Runtime { get; init; }
    public required IEnumerable<DotnetVersion> Sdk { get; init; }

    public static async Task<Limits> Load()
    {
        // Read limits file
        var snapRoot = Environment.GetEnvironmentVariable("SNAP") ?? string.Empty;
        var limitsFilePath = Path.Combine(snapRoot, "Configuration", "limits.json");

        if (!File.Exists(limitsFilePath)) throw new ApplicationException("Limits file could not be found.");
        
        await using var fs = File.OpenRead(limitsFilePath);
        var limits = await JsonSerializer.DeserializeAsync<Limits>(fs, JsonSerializerOptions);

        if (limits is null) throw new ApplicationException("Could not read limits file.");

        return limits;
    }
}
