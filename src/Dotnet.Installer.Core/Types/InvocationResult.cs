namespace Dotnet.Installer.Core.Types;

public class InvocationResult(bool isSuccess, string standardOutput, string standardError)
{
    public bool IsSuccess { get; init; } = isSuccess;
    public string StandardOutput { get; init; } = standardOutput;
    public string StandardError { get; init; } = standardError;
}