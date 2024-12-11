using System.Collections.Immutable;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Implementations;

public partial class SnapService : ISnapService
{
    public bool IsSnapInstalled(string name, CancellationToken cancellationToken = default)
    {
        return Directory.Exists(Path.Combine("/", "snap", name));
    }

    public Task<Terminal.InvocationResult> Install(string name, CancellationToken cancellationToken = default)
    {
        return Terminal.Invoke("snap", "install", name);
    }

    public Task<Terminal.InvocationResult> Remove(string name, bool purge = false, CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>
        {
            "remove"
        };

        if (purge) arguments.Add("--purge");
        arguments.Add(name);

        return Terminal.Invoke("snap", arguments.ToArray());
    }

    public Task<IImmutableList<SnapInfo>> GetInstalledSnaps(CancellationToken cancellationToken = default)
    {
        SnapdRestClient snapdRestClient;

        try
        {
            snapdRestClient = GetSnapdRestClient();
        }
        catch (Exception exception)
        {
            return Task.FromException<IImmutableList<SnapInfo>>(exception);
        }

        return snapdRestClient.GetInstalledSnapsAsync(cancellationToken);
    }

    public Task<SnapInfo?> GetInstalledSnap(string name, CancellationToken cancellationToken = default)
    {
        SnapdRestClient snapdRestClient;

        try
        {
            snapdRestClient = GetSnapdRestClient();
        }
        catch (Exception exception)
        {
            return Task.FromException<SnapInfo?>(exception);
        }

        return snapdRestClient.GetInstalledSnapAsync(name, cancellationToken);
    }

    public Task<SnapInfo?> FindSnap(string name, CancellationToken cancellationToken = default)
    {
        SnapdRestClient snapdRestClient;

        try
        {
            snapdRestClient = GetSnapdRestClient();
        }
        catch (Exception exception)
        {
            return Task.FromException<SnapInfo?>(exception);
        }

        return snapdRestClient.FindSnapAsync(name, cancellationToken);
    }

    public void Dispose()
    {
        _snapdRestClient?.Dispose();
    }
}
