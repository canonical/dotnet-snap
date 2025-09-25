using System.Runtime.InteropServices;

namespace Dotnet.Installer.Core.Types;

public static partial class Native
{
    [LibraryImport("libc.so.6")]
    private static partial int geteuid();

    /// <summary>
    /// Returns the effective user ID of the calling process.
    /// This is a wrapper around <c>geteuid(2)</c>.
    /// </summary>
    /// <returns>The effective user ID of the calling process.</returns>
    public static int GetCurrentEffectiveUserId() => geteuid();
}
