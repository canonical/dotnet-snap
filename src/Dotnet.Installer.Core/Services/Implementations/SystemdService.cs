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

    public Task<Terminal.InvocationResult> DaemonReload()
    {
        return Terminal.Invoke("systemctl", _globalSystemdOptions, "daemon-reload");
    }

    public Task<Terminal.InvocationResult> EnableUnit(string unit)
    {
        return Terminal.Invoke("systemctl", _globalSystemdOptions, "enable", unit);
    }

    public Task<Terminal.InvocationResult> DisableUnit(string unit)
    {
        return Terminal.Invoke("systemctl", _globalSystemdOptions, "disable", unit);
    }

    public Task<Terminal.InvocationResult> StartUnit(string unit)
    {
        return Terminal.Invoke("systemctl", _globalSystemdOptions, "start", unit);
    }

    public Task<Terminal.InvocationResult> StopUnit(string unit)
    {
        return Terminal.Invoke("systemctl", _globalSystemdOptions, "stop", unit);
    }
}
