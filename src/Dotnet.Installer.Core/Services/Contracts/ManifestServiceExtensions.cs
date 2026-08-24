namespace Dotnet.Installer.Core.Services.Contracts;

public static class ManifestServiceExtensions
{
    /// <summary>
    /// Returns the major versions available in the remote manifest for a given component name
    /// (e.g. 'runtime', 'aspnetcore-runtime', 'sdk'), sorted from newest to oldest.
    /// </summary>
    /// <param name="manifestService">The manifest service.</param>
    /// <param name="componentName">The .NET component name.</param>
    /// <returns>A list of available major versions, or an empty list if the component name is unknown.</returns>
    public static List<int> GetAvailableVersions(this IManifestService manifestService, string componentName)
    {
        return manifestService.Remote
            .Where(c => c.Name.Equals(componentName, StringComparison.CurrentCultureIgnoreCase))
            .Select(c => c.MajorVersion)
            .Distinct()
            .OrderByDescending(v => v)
            .ToList();
    }
}
