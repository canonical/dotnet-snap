using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Contracts;

public interface ISnapService
{
    bool IsSnapInstalled(string name, CancellationToken cancellationToken = default);
    Task<InvocationResult> Install(string name, CancellationToken cancellationToken = default);
    Task<InvocationResult> Remove(string name, bool purge = false, CancellationToken cancellationToken = default);
}