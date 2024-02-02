using System.Text.Json.Serialization;
using Dotnet.Installer.Core.Converters;

namespace Dotnet.Installer.Core.Types;

[JsonConverter(typeof(DotnetVersionJsonConverter))]
public partial class DotnetVersion(int major, int minor, int patch) : IEquatable<DotnetVersion>, IComparable<DotnetVersion>
{
    public int Major { get; } = major;
    public int Minor { get; } = minor;
    public int Patch { get; } = patch;

    public bool IsRuntime => Patch < 100;
    public bool IsSdk => !IsRuntime;

    public int? FeatureBand => !IsSdk ? default(int?) : int.Parse($"{Patch.ToString()[..1]}00");

    public static DotnetVersion Parse(string version)
    {
        var sections = version.Split('.');
        var parsedVersion = new DotnetVersion
        (
            int.Parse(sections[0]),
            int.Parse(sections[1]),
            int.Parse(sections[2])
        );
        
        return parsedVersion;
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }
}
