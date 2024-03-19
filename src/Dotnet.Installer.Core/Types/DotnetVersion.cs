using System.Text;
using System.Text.Json.Serialization;
using Dotnet.Installer.Core.Converters;

namespace Dotnet.Installer.Core.Types;

[JsonConverter(typeof(DotnetVersionJsonConverter))]
public partial class DotnetVersion : IEquatable<DotnetVersion>, IComparable<DotnetVersion>
{
    public DotnetVersion(int major, int minor, int patch, bool isPreview = false, bool isRc = false,
        int? previewIdentifier = null)
    {
        if (isPreview && isRc)
        {
            throw new ApplicationException("The .NET version can either be a preview, an RC, or none.");
        }

        if ((isPreview || isRc) && previewIdentifier is null)
        {
            throw new ApplicationException(
                "You must specify a Preview Identifier if version is either a preview of an RC.");
        }

        if (!isPreview && !isRc && previewIdentifier is not null)
        {
            throw new ApplicationException(
                "You can't specify a Preview Identifier if the version is neither a preview or an RC.");
        }
        
        Major = major;
        Minor = minor;
        Patch = patch;

        IsPreview = isPreview;
        IsRc = isRc;

        PreviewIdentifier = previewIdentifier;
    }
    
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    public bool IsPreview { get; private set; }
    public bool IsRc { get; private set; }
    public bool IsStable => !IsPreview && !IsRc;
    public int? PreviewIdentifier { get; private set; } = null;
    
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
        var versionSb = new StringBuilder();
        
        versionSb.Append(Major);
        versionSb.Append('.');
        versionSb.Append(Minor);
        versionSb.Append('.');
        versionSb.Append(Patch);

        if (IsPreview) versionSb.Append("-preview.");
        if (IsRc) versionSb.Append("-rc.");

        versionSb.Append(PreviewIdentifier.ToString());

        return versionSb.ToString();
    }
}
