using CliWrap;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Extensions;

public static class CommandExtensions
{
    public static Command Elevate(this Command command)
    {
        // Check if the current process' effective User ID is root.
        var effectiveUserId = Native.GetCurrentEffectiveUserId();

        // If yes, no need to elevate process.
        if (effectiveUserId == Native.RootUid) return command;
        
        // If not, check if pkexec exists. If yes, use it. If not, use sudo.
        var oldTargetFilePath = command.TargetFilePath;
        var oldArguments = command.Arguments.Split(' ');
        
        var arguments = new List<string> { oldTargetFilePath };
        arguments.AddRange(oldArguments);

        command = File.Exists(Path.Combine("/", "usr", "bin", "pkexec"))
            ? command.WithTargetFile("/usr/bin/pkexec")
            : command.WithTargetFile("/usr/bin/sudo");
        
        command = command.WithArguments(arguments, escape: true);
        
        return command;
    }
}