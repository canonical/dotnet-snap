using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Implementations;

public class SnapService : ISnapService
{
    public bool IsSnapInstalled(string name, CancellationToken cancellationToken = default)
    {
        return Directory.Exists(Path.Combine("/", "snap", name));
    }

    public async Task<InvocationResult> Install(string name, CancellationToken cancellationToken = default)
    {
        var result = await Terminal.Invoke("snap", sudo: true, "install", name);
        return new InvocationResult(result == 0, "", "");
    }

    public async Task<InvocationResult> Remove(string name, bool purge = false, CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>
        {
            "remove"
        };
        
        if (purge) arguments.Add("--purge");
        arguments.Add(name);
        
        var result = await Terminal.Invoke("snap", sudo: true, arguments.ToArray());
        return new InvocationResult(result == 0, "", "");
    }
}