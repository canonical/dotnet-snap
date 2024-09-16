using Dotnet.Installer.Core.Services.Contracts;

namespace Dotnet.Installer.Console.Services.Implementations;

public class Logger : ILogger
{
    public void LogInformation(string message) => Serilog.Log.Information(message);
    public void LogDebug(string message) => Serilog.Log.Debug(message);
    public void LogWarning(string message) => Serilog.Log.Warning(message);
    public void LogError(string message) => Serilog.Log.Error(message);
    public void LogVerbose(string message) => Serilog.Log.Verbose(message);
}
