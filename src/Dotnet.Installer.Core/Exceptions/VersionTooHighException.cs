namespace Dotnet.Installer.Core.Exceptions;

public class VersionTooHighException(string message) : ApplicationException(message);