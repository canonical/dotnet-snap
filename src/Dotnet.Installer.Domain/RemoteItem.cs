using System.Text.Json.Serialization;
using Dotnet.Installer.Domain.Types;

namespace Dotnet.Installer.Domain;

public class RemoteItem
{
    public string Key { get; set; }

    public string Component { get; set; }

    public Uri Url { get; set; }

    public string Sha256 { get; set; }

    [JsonConverter(typeof(DotnetVersionJsonConverter))]
    public DotnetVersion Version { get; set; }
    
    public IEnumerable<string> Locations { get; set; }
}
