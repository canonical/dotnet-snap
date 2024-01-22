using System.Text.Json.Serialization;
using Dotnet.Installer.Domain.Types;

namespace Dotnet.Installer.Domain;

public class RemoteItem
{
    public required string Key { get; set; }

    public required string Component { get; set; }

    public required Uri Url { get; set; }

    public required string Sha256 { get; set; }

    [JsonConverter(typeof(DotnetVersionJsonConverter))]
    public required DotnetVersion Version { get; set; }
    
    public required IEnumerable<string> Locations { get; set; }

    public required IEnumerable<string> Dependencies { get; set; }
}
