using System.Diagnostics;

namespace Dotnet.Installer.Core.Types;

public static class Terminal
{
    public static async Task<int> Invoke(string program, params string[] arguments)
    {
        var process = new Process();

        process.StartInfo.FileName = program;
        
        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);
        
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.RedirectStandardError = false;

        process.Start();
        await process.WaitForExitAsync();

        return process.ExitCode;
    }
}