namespace Dotnet.Installer.Core.Models;

public static class Constants
{
    public const string DotnetRuntimeComponentName = "runtime";
    public const string AspnetCoreRuntimeComponentName = "aspnetcore-runtime";
    public const string SdkComponentName = "sdk";

    /// <summary>
    /// Checks whether a string is a valid .NET component name
    /// ('runtime', 'aspnetcore-runtime' or 'sdk', case-insensitive).
    /// </summary>
    /// <param name="componentName">The component name to validate.</param>
    /// <returns><see langword="true"/> if the name is a valid component; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidComponentName(string componentName)
    {
        return componentName.Equals(DotnetRuntimeComponentName, StringComparison.CurrentCultureIgnoreCase)
               || componentName.Equals(AspnetCoreRuntimeComponentName, StringComparison.CurrentCultureIgnoreCase)
               || componentName.Equals(SdkComponentName, StringComparison.CurrentCultureIgnoreCase);
    }
}
