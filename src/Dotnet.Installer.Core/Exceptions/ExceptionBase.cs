namespace Dotnet.Installer.Core.Exceptions;

public abstract class ExceptionBase(Error errorCode) : ApplicationException
{
    public Error ErrorCode { get; } = errorCode;
}
