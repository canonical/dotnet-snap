using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Implementations;

public class SystemdService : ISystemdService
{
    private readonly Terminal.InvocationOptions _globalSystemdOptions = new()
    {
        RedirectStandardError = true,
        RedirectStandardOutput = true,
    };
    
    public async Task<InvocationResult> DaemonReload()
    {
        var result = await Terminal.Invoke("systemctl", _globalSystemdOptions, "daemon-reload");
        return new InvocationResult(result == 0, string.Empty, string.Empty);
    }

    public async Task<InvocationResult> EnableUnit(string unit)
    {
        var result = await Terminal.Invoke("systemctl", _globalSystemdOptions, "enable", unit);
        return new InvocationResult(result == 0, string.Empty, string.Empty);
    }

    public async Task<InvocationResult> DisableUnit(string unit)
    {
        var result = await Terminal.Invoke("systemctl", _globalSystemdOptions, "disable", unit);
        return new InvocationResult(result == 0, string.Empty, string.Empty);
    }

    public async Task<InvocationResult> StartUnit(string unit)
    {
        var result = await Terminal.Invoke("systemctl", _globalSystemdOptions, "start", unit);
        return new InvocationResult(result == 0, string.Empty, string.Empty);
    }

    public async Task<InvocationResult> StopUnit(string unit)
    {
        var result = await Terminal.Invoke("systemctl", _globalSystemdOptions, "stop", unit);
        return new InvocationResult(result == 0, string.Empty, string.Empty);
    }
}
