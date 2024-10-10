using System.Diagnostics;

namespace Dotnet.Installer.Core.Types;

public static class Terminal
{
    public static async Task<InvocationResult> Invoke(string program, params string[] arguments)
    {
        return await Invoke(program, options: null, arguments);
    }

    public static async Task<InvocationResult> Invoke(string program, InvocationOptions? options = default,
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

        return new InvocationResult
        {
            ExitCode = process.ExitCode,
            RedirectedStandardError = options.RedirectStandardError,
            RedirectedStandardOutput = options.RedirectStandardOutput,
            StandardError = options.RedirectStandardError ?
                await process.StandardError.ReadToEndAsync() : default,
            StandardOutput = options.RedirectStandardOutput ?
                await process.StandardOutput.ReadToEndAsync() : default
        };
    }

    public class InvocationOptions
    {
        public static InvocationOptions Default => new();

        public bool RedirectStandardOutput { get; set; } = false;
        public bool RedirectStandardError { get; set; } = false;
    }

    public class InvocationResult
    {
        public InvocationResult()
        { }

        public InvocationResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;

            RedirectedStandardError = true;
            RedirectedStandardOutput = true;
        }

        public bool IsSuccess => ExitCode == 0;
        public int ExitCode { get; init; }
        public bool RedirectedStandardOutput { get; init; }
        public bool RedirectedStandardError { get; init; }
        public string? StandardOutput { get; init; }
        public string? StandardError { get; init; }
    }
}
