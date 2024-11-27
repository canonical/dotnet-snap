using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Contracts;

public interface ISnapService : IDisposable
{
    bool IsSnapInstalled(string name, CancellationToken cancellationToken = default);
    Task<Terminal.InvocationResult> Install(string name, CancellationToken cancellationToken = default);
    Task<Terminal.InvocationResult> Remove(string name, bool purge = false, CancellationToken cancellationToken = default);
    Task<SnapInfo?> Find(string name, CancellationToken cancellationToken = default);
}
