using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Implementations;

public class SystemDService : ISystemDService
{
    public async Task<InvocationResult> DaemonReload()
    {
        var result = await Terminal.Invoke("systemctl", "daemon-reload");
        return new InvocationResult(result == 0, string.Empty, string.Empty);
    }

    public async Task<InvocationResult> EnableUnit(string unit)
    {
        var result = await Terminal.Invoke("systemctl", "enable", unit);
        return new InvocationResult(result == 0, string.Empty, string.Empty);
    }

    public async Task<InvocationResult> DisableUnit(string unit)
    {
        var result = await Terminal.Invoke("systemctl", "disable", unit);
        return new InvocationResult(result == 0, string.Empty, string.Empty);
    }

    public async Task<InvocationResult> StartUnit(string unit)
    {
        var result = await Terminal.Invoke("systemctl", "start", unit);
        return new InvocationResult(result == 0, string.Empty, string.Empty);
    }

    public async Task<InvocationResult> StopUnit(string unit)
    {
        var result = await Terminal.Invoke("systemctl", "stop", unit);
        return new InvocationResult(result == 0, string.Empty, string.Empty);
    }
}
