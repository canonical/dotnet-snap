using System.Text;
using CliWrap;
using Dotnet.Installer.Core.Extensions;
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
        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();
        
        var command = Cli.Wrap("snap")
            .WithArguments(["install", name])
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(standardOutput))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(standardError))
            .Elevate();

        var result = await command.ExecuteAsync(cancellationToken);
        return new InvocationResult(result.IsSuccess, standardOutput.ToString(), standardError.ToString());
    }

    public async Task<InvocationResult> Remove(string name, bool purge = false, CancellationToken cancellationToken = default)
    {
        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();
        
        List<string> arguments = ["remove"];
        if (purge) arguments.Add("--purge");
        arguments.Add(name);

        var command = Cli.Wrap("snap")
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(standardOutput))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(standardError))
            .Elevate();

        var result = await command.ExecuteAsync(cancellationToken);
        return new InvocationResult(result.IsSuccess, standardOutput.ToString(), standardError.ToString());
    }
}