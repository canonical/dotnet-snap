namespace Dotnet.Installer.Core.Types;

public partial class DotnetVersion
{
    public int CompareTo(DotnetVersion? other)
    {
        // Keep null entries last
        if (other is null) return -1;

        if (other.Major != Major) return Major - other.Major;
        if (other.Minor != Minor) return Minor - other.Minor;
        if (other.Patch != Patch) return Patch - other.Patch;

        // Version is the same, but preview information might be different
        if ((other.IsPreview && IsPreview) || (other.IsRc && IsRc))
            return PreviewIdentifier!.Value - other.PreviewIdentifier!.Value;
        
        if (other.IsPreview && IsRc) return 1;
        if (other.IsRc && IsPreview) return -1;

        if (IsStable && !other.IsStable) return 1;
        if (!IsStable && other.IsStable) return -1;

        // It will come down to revisions then
        if (Revision.HasValue && !other.Revision.HasValue) return 1;
        else if (!Revision.HasValue && other.Revision.HasValue) return -1;
        else if (!Revision.HasValue && !other.Revision.HasValue) return 0;
        else return Revision!.Value - other.Revision!.Value;
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

        return (Major == other.Major) && (Minor == other.Minor) && (Patch == other.Patch) &&
               (IsPreview == other.IsPreview) && (IsRc == other.IsRc) && (PreviewIdentifier == other.PreviewIdentifier);
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
