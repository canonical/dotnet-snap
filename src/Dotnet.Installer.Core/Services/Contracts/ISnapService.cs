namespace Dotnet.Installer.Core.Services.Contracts;

public interface ISnapService
{
    Task<bool> IsSnapInstalled(string name, CancellationToken cancellationToken = default);
    Task Install(string name, CancellationToken cancellationToken = default);
    Task Remove(string name, bool purge = false, CancellationToken cancellationToken = default);
}