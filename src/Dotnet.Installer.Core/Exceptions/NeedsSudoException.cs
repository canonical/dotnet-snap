namespace Dotnet.Installer.Core.Exceptions;

public class NeedsSudoException(string path) : ExceptionBase(Error.NeedsSudo)
{
    public string Path { get; } = path;

    public override string Message => "Unauthorized access. Run with sudo?";
}
