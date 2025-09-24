using System.Text.Json;
using System.Text.RegularExpressions;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Services.Contracts;

namespace Dotnet.Installer.Core.Services.Implementations;

public partial class ManifestService : IManifestService
{
    private static readonly Regex DotnetVersionPattern = new (
        pattern: @"\A(?'major'\d+)(?:\.(?'minor'\d+))?\z");

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private List<Component> _local = [];
    private List<Component> _remote = [];
    private List<Component> _merged = [];
    private bool _includeUnsupported = false;
    private bool _includePrerelease = false;

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

    public Task Initialize(bool includeUnsupported = false, bool includePrerelease = false,
        CancellationToken cancellationToken = default)
    {
        _includeUnsupported = includeUnsupported;
        _includePrerelease = includePrerelease;
        return Refresh(cancellationToken);
    }

    public async Task Add(Component component, CancellationToken cancellationToken = default)
    {
        component.Installation = new Installation
        {
            InstalledAt = DateTimeOffset.UtcNow
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

    public Component? MatchRemoteComponent(string component, string version)
    {
        return MatchComponent(component, version, remote: true);
    }

    public Component? MatchLocalComponent(string component, string version)
    {
        return MatchComponent(component, version, remote: false);
    }

    private Component? MatchComponent(string component, string version, bool remote = true)
    {
        if (string.IsNullOrWhiteSpace(component)) return null;
        if (string.IsNullOrWhiteSpace(version)) return null;

        var components = remote ? _remote : _local;

        if (version.Equals("lts", StringComparison.CurrentCultureIgnoreCase))
        {
            return components
                .Where(c => c.IsLts && c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase))
                .MaxBy(c => c.MajorVersion);
        }

        if (version.Equals("latest", StringComparison.CurrentCultureIgnoreCase))
        {
            return components
                .Where(c => c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase))
                .MaxBy(c => c.MajorVersion);
        }

        var parsedVersion = DotnetVersionPattern.Match(version);

        if (!parsedVersion.Success) return null;
        if (parsedVersion.Groups["minor"].Success
            && int.Parse(parsedVersion.Groups["minor"].Value) != 0)
        {
            return null;
        }

        var majorVersion = int.Parse(parsedVersion.Groups["major"].Value);

        return components.Where(c =>
                c.MajorVersion == majorVersion &&
                c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase))
            .MaxBy(c => c.MajorVersion);
    }
}
