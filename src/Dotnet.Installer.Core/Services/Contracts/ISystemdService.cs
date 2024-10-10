using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Contracts;

public interface ISystemdService
{
    Task<Terminal.InvocationResult> DaemonReload();
    Task<Terminal.InvocationResult> EnableUnit(string unit);
    Task<Terminal.InvocationResult> DisableUnit(string unit);
    Task<Terminal.InvocationResult> StartUnit(string unit);
    Task<Terminal.InvocationResult> StopUnit(string unit);
}
