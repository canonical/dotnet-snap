namespace Dotnet.Installer.Core.Services.Contracts;

public interface ILogger
{
    void LogInformation(string message);
    void LogDebug(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogVerbose(string message);
}
