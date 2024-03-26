using System.Runtime.InteropServices;

namespace Dotnet.Installer.Core.Exceptions;

public class UnsupportedArchitectureException(Architecture architecture)
    : ExceptionBase(Error.UnsupportedArchitecture)
{
    public Architecture HostArchitecture { get; } = architecture;

    public override string Message => $"The architecture {HostArchitecture} is currently unsupported";
}
