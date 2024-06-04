using System.Text;
using CliWrap;
using Dotnet.Installer.Core.Services.Contracts;

namespace Dotnet.Installer.Core.Services.Implementations;

public class SnapService : ISnapService
{
    public async Task<bool> IsSnapInstalled(string name, CancellationToken cancellationToken = default)
    {
        var cliOutput = new StringBuilder();
        await Cli.Wrap("snap")
            .WithArguments(["list"])
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(cliOutput))
            .ExecuteAsync(cancellationToken);

        return cliOutput.ToString().Contains(name);
    }

    public Task Install(string name, CancellationToken cancellationToken = default)
    {
        return Cli.Wrap("snap")
            .WithArguments(["install", name])
            .ExecuteAsync(cancellationToken);
    }

    public Task Remove(string name, bool purge = false, CancellationToken cancellationToken = default)
    {
        List<string> arguments = ["remove"];
        if (purge) arguments.Add("--purge");
        arguments.Add(name);
        
        return Cli.Wrap("snap")
            .WithArguments(arguments)
            .ExecuteAsync(cancellationToken);
    }
}