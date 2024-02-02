namespace Dotnet.Installer.Core.Types;

public partial class DotnetVersion
{
    public int CompareTo(DotnetVersion? other)
    {
        // Keep null entries last
        if (other is null) return -1;

        if (other.Major != Major) return Major - other.Major;
        if (other.Minor != Minor) return Minor - other.Minor;
        return Patch - other.Patch;
    }

    public static bool operator <(DotnetVersion lhs, DotnetVersion rhs) => lhs.CompareTo(rhs) < 0;
    public static bool operator >(DotnetVersion lhs, DotnetVersion rhs) => lhs.CompareTo(rhs) > 0;
    public static bool operator <=(DotnetVersion lhs, DotnetVersion rhs) => lhs.CompareTo(rhs) <= 0;
    public static bool operator >=(DotnetVersion lhs, DotnetVersion rhs) => lhs.CompareTo(rhs) >= 0;

    public override bool Equals(object? obj) => Equals(obj as DotnetVersion);

    public bool Equals(DotnetVersion? other)
    {
        if (other is null) return false;

        if (ReferenceEquals(this, other)) return true;

        if (GetType() != other.GetType()) return false;

        return (Major == other.Major) && (Minor == other.Minor) && (Patch == other.Patch);
    }

    public override int GetHashCode() => (Major, Minor, Patch).GetHashCode();

    public static bool operator ==(DotnetVersion? lhs, DotnetVersion? rhs)
    {
        if (lhs is null)
        {
            return rhs is null;
        }

        return lhs.Equals(rhs);
    }

    public static bool operator !=(DotnetVersion lhs, DotnetVersion rhs) => !(lhs == rhs);
}