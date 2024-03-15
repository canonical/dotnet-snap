using System.Text.Json.Serialization;
using Dotnet.Installer.Core.Converters;

namespace Dotnet.Installer.Core.Types;

[JsonConverter(typeof(DotnetVersionJsonConverter))]
public partial class DotnetVersion(int major, int minor, int patch) : IEquatable<DotnetVersion>, IComparable<DotnetVersion>
{
    public int Major { get; } = major;
    public int Minor { get; } = minor;
    public int Patch { get; } = patch;

    public bool IsPreview { get; set; }
    public bool IsRc { get; set; }
    public int? PreviewIdentifier { get; set; } = null;
    
    public bool IsRuntime => Patch < 100;
    public bool IsSdk => !IsRuntime;

    public int? FeatureBand => !IsSdk ? default(int?) : int.Parse($"{Patch.ToString()[..1]}00");

    public static DotnetVersion Parse(string version)
    {
        var previewSplit = version.Split('-');
        var versionSections = previewSplit[0].Split('.');
        var parsedVersion = new DotnetVersion
        (
            int.Parse(versionSections[0]),
            int.Parse(versionSections[1]),
            int.Parse(versionSections[2])
        );
        
        if (previewSplit.Length > 1)
        {
            var previewVersionSections = previewSplit[1].Split('.');
            
            if (string.Equals("preview", previewVersionSections[0]))
            {
                parsedVersion.IsPreview = true;
            }
            else if (string.Equals("rc", previewVersionSections[0]))
            {
                parsedVersion.IsRc = true;
            }

            parsedVersion.PreviewIdentifier = int.Parse(previewVersionSections[1]);
        }
        
        return parsedVersion;
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }
}
