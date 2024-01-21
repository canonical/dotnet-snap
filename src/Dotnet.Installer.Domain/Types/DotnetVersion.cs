namespace Dotnet.Installer.Domain.Types;

public class DotnetVersion : IEquatable<DotnetVersion>
{
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Patch { get; set; }

    public DotnetVersion(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

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

    public override bool Equals(object? obj) => Equals(obj as DotnetVersion);

    public bool Equals(DotnetVersion? other)
    {
        if (other is null) return false;

        if (ReferenceEquals(this, other)) return true;

        if (GetType() != other.GetType()) return false;

        return (Major == other.Major) && (Minor == other.Minor) && (Patch == other.Patch);
    }

    public override int GetHashCode() => (Major, Minor, Patch).GetHashCode();

    public static bool operator ==(DotnetVersion lhs, DotnetVersion rhs)
    {
        if (lhs is null)
        {
            if (rhs is null)
            {
                return true;
            }

            return false;
        }

        return lhs.Equals(rhs);
    }

    public static bool operator !=(DotnetVersion lhs, DotnetVersion rhs) => !(lhs == rhs);
}
