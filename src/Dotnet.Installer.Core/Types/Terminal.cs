using System.Diagnostics;

namespace Dotnet.Installer.Core.Types;

public static class Terminal
{
    public static async Task<int> Invoke(string program, params string[] arguments)
    {
        return await Invoke(program, options: null, arguments);
    }

    public static async Task<int> Invoke(string program, InvocationOptions? options = default,
        params string[] arguments)
    {
        options ??= InvocationOptions.Default;

        var process = new Process();

        process.StartInfo.FileName = program;

        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = options.RedirectStandardOutput;
        process.StartInfo.RedirectStandardError = options.RedirectStandardError;

        process.Start();
        await process.WaitForExitAsync();

        return process.ExitCode;
    }

    public class InvocationOptions
    {
        public static InvocationOptions Default => new();

        public bool RedirectStandardOutput { get; set; } = false;
        public bool RedirectStandardError { get; set; } = false;
    }
}
