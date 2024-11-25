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
    private bool _includeUnsupported = false;

    public string SnapConfigurationLocation => SnapConfigPath;
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

    public Task Initialize(bool includeUnsupported = false, CancellationToken cancellationToken = default)
    {
        _includeUnsupported = includeUnsupported;
        return Refresh(cancellationToken);
    }

    public async Task Add(Component component, bool isRootComponent, CancellationToken cancellationToken = default)
    {
        component.Installation = new Installation
        {
            InstalledAt = DateTimeOffset.UtcNow,
            IsRootComponent = isRootComponent
        };
        _local.Add(component);
        await Save(cancellationToken);
        await Refresh(cancellationToken);
    }

    public async Task Remove(Component component, CancellationToken cancellationToken = default)
    {
        var componentToRemove = _local.FirstOrDefault(c => c.Key == component.Key);
        if (componentToRemove is not null)
        {
            _local.Remove(componentToRemove);
        }
        await Save(cancellationToken);
        await Refresh(cancellationToken);
    }

    public Component? MatchVersion(string component, string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return default;

        return version.Length switch
        {
            // Major version only, e.g. install sdk 8
            1 => _remote.Where(c => c.MajorVersion == int.Parse(version) &&
                    c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase))
                .MaxBy(c => c.MajorVersion),

            // Major and minor version only, e.g. install sdk 8.0
            3 => _remote.Where(c => // "8.0"
                    c.MajorVersion == int.Parse(version[..1]) &&
                    c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase))
                .MaxBy(c => c.MajorVersion),

            _ => default
        };
    }
}
