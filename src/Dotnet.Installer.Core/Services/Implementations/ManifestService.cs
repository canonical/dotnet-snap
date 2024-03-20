using System.Text.Json;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Services.Contracts;

namespace Dotnet.Installer.Core.Services.Implementations;

public partial class ManifestService : IManifestService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private List<Component> _local = [];
    private List<Component> _remote = [];
    private List<Component> _merged = [];

    public string DotnetInstallLocation =>
        Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR") 
            ?? throw new ApplicationException("DOTNET_INSTALL_DIR is not set.");
    
    /// <summary>
    /// The local manifest, which includes currently installed components.
    /// </summary>
    public IEnumerable<Component> Local
    {
        get => _local;
        private set => _local = value.ToList();
    }

    /// <summary>
    /// The remote manifest, which includes available components to be downloaded.
    /// </summary>
    public IEnumerable<Component> Remote
    {
        get => _remote;
        private set => _remote = value.ToList();
    }

    /// <summary>
    /// The merged manifest, which is the local and remote manifests merged into one list.
    /// Installed components can be told apart by verifying whether <c>Installation != null</c>.
    /// </summary>
    public IEnumerable<Component> Merged
    {
        get => _merged;
        private set => _merged = value.ToList();
    }

    public async Task Initialize(bool includeArchive = false, CancellationToken cancellationToken = default)
    {
        _local = await LoadLocal(cancellationToken);
        _remote = await LoadRemote(includeArchive, cancellationToken);
        _merged = Merge(_remote, _local);
    }

    public async Task Add(Component component, CancellationToken cancellationToken = default)
    {
        component.Installation = new Installation
        {
            InstalledAt = DateTimeOffset.UtcNow
        };
        _local.Add(component);
        await Save(cancellationToken);
    }

    public async Task Remove(Component component, CancellationToken cancellationToken = default)
    {
        var componentToRemove = _local.FirstOrDefault(c => c.Key == component.Key);
        if (componentToRemove is not null)
        {
            _local.Remove(componentToRemove);
        }
        await Save(cancellationToken);
    }
}
