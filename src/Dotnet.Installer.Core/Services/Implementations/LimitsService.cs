using System.Text.Json;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Implementations;

public class LimitsService : ILimitsService
{    
    public DotnetVersion Runtime { get; }
    public IEnumerable<DotnetVersion> Sdk { get; }

    public LimitsService(IFileService fileService)
    {
        // Read limits file
        var limitsFilePath = Environment.GetEnvironmentVariable("LIMITS_PATH") ?? string.Empty;

        if (!fileService.FileExists(limitsFilePath)) throw new ApplicationException("Limits file could not be found.");
        
        using var fs = fileService.OpenRead(limitsFilePath);
        var limits = JsonDocument.Parse(fs);

        Runtime = limits.RootElement.GetProperty("runtime").Deserialize<DotnetVersion>()!;
        Sdk = limits.RootElement.GetProperty("sdk").Deserialize<IEnumerable<DotnetVersion>>()!;

        // Max out the revision to avoid installations failing when a new revision
        // comes out and the comparison between e.g. 8.0.101+1 <= 8.0.101 fails
        // when assessing whether that specific version can be installed with
        // the current host.
        Runtime.Revision = int.MaxValue;
        foreach (var sdkVersion in Sdk)
        {
            sdkVersion.Revision = int.MaxValue;
        }
    }
}
