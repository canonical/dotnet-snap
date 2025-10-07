using System.Collections.Immutable;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Contracts;

public enum SnapChannel
{
    Stable,
    Candidate,
    Beta,
    Edge
}

public interface ISnapService : IDisposable
{
    bool IsSnapInstalled(string name, CancellationToken cancellationToken = default);
    Task<Terminal.InvocationResult> Install(string name, SnapChannel channel = SnapChannel.Stable,
        CancellationToken cancellationToken = default);
    Task<Terminal.InvocationResult> Remove(string name, bool purge = false, CancellationToken cancellationToken = default);
    Task<IImmutableList<SnapInfo>> GetInstalledSnaps(CancellationToken cancellationToken = default);
    Task<SnapInfo?> GetInstalledSnap(string name, CancellationToken cancellationToken = default);
    Task<SnapInfo?> FindSnap(string name, CancellationToken cancellationToken = default);
}
