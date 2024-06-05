using CliWrap;

namespace Dotnet.Installer.Core.Extensions;

public static class CliExtensions
{
    public static Command Elevate(this Command command)
    {
        var oldTargetFilePath = command.TargetFilePath;
        var oldArguments = command.Arguments.Split(' ');
        
        var arguments = new List<string> { oldTargetFilePath };
        arguments.AddRange(oldArguments);

        command = command.WithTargetFile("/usr/bin/pkexec");
        command = command.WithArguments(arguments, escape: true);

        return command;
    }
}