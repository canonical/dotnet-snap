using System.Runtime.InteropServices;
using Dotnet.Installer.Core.Exceptions;

namespace Dotnet.Installer.Core;

public class UnsupportedArchitectureException(Architecture architecture)
    : ExceptionBase(Error.UnsupportedArchitecture)
{
    public Architecture HostArchitecture { get; } = architecture;

    public override string Message => $"The architecture {HostArchitecture} is currently unsupported";
}
