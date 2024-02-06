using System.Net.Http.Json;
using System.Text.Json;

namespace Dotnet.Installer.Core.Models;

public partial class Manifest
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private List<Component> _local;
    private List<Component> _remote;
    private List<Component> _merged;

    public static string DotnetInstallLocation =>
        Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR") 
            ?? throw new ApplicationException("DOTNET_INSTALL_DIR is not set.");
    
    public IEnumerable<Component> Local
    {
        get => _local;
        private set => _local = value.ToList();
    }

    public IEnumerable<Component> Remote
    {
        get => _remote;
        private set => _remote = value.ToList();
    }

    public IEnumerable<Component> Merged
    {
        get => _merged;
        private set => _merged = value.ToList();
    }

    private Manifest(List<Component> localManifest, List<Component> remoteManifest, List<Component> mergedManifest)
    {
        _local = localManifest;
        _remote = remoteManifest;
        _merged = mergedManifest;
    }

    public static async Task<Manifest> Initialize(bool includeArchive = false, CancellationToken cancellationToken = default)
    {
        var local = await LoadLocal(cancellationToken);
        var remote = await LoadRemote(includeArchive, cancellationToken);
        var merged = Merge(remote, local);

        return new Manifest(local, remote, merged);
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
