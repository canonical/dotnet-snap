using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Contracts;

public interface ILimitsService
{
    DotnetVersion Runtime { get; }
    IEnumerable<DotnetVersion> Sdk { get; }
}
