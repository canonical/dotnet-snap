using Dotnet.Installer.Core.Models;

namespace Dotnet.Installer.Core.Services.Contracts;

public interface IManifestService
{
    string SnapConfigurationLocation { get; }
    string DotnetInstallLocation { get; }

    IEnumerable<Component> Local { get; }
    IEnumerable<Component> Remote { get; }
    IEnumerable<Component> Merged { get; }

    Task Initialize(bool includeArchive = false, CancellationToken cancellationToken = default);
    Task Add(Component component, CancellationToken cancellationToken = default);
    Task Remove(Component component, CancellationToken cancellationToken = default);
}
