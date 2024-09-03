using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Contracts;

public interface ISystemDService
{
    Task<InvocationResult> DaemonReload();
    Task<InvocationResult> EnableUnit(string unit);
    Task<InvocationResult> DisableUnit(string unit);
    Task<InvocationResult> StartUnit(string unit);
    Task<InvocationResult> StopUnit(string unit);
}
