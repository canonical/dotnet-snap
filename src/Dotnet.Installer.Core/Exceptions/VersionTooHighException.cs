using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Exceptions;

public class VersionTooHighException(Component attemptedComponent, DotnetVersion highestSupportedVersion)
    : ExceptionBase(Error.VersionTooHigh)
{
    public Component AttemptedComponent { get; set; } = attemptedComponent;
    public DotnetVersion HighestSupportedVersion { get; set; } = highestSupportedVersion;

    public override string Message => 
        $"""
        The component {AttemptedComponent.Name} {AttemptedComponent.Version} cannot be installed.
        Update your snap to the latest version to install this component.
        """;
}