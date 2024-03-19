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

        if (!fileService.Exists(limitsFilePath)) throw new ApplicationException("Limits file could not be found.");
        
        using var fs = fileService.OpenRead(limitsFilePath);
        var limits = JsonDocument.Parse(fs);

        Runtime = limits.RootElement.GetProperty("runtime").Deserialize<DotnetVersion>()!;
        Sdk = limits.RootElement.GetProperty("sdk").Deserialize<IEnumerable<DotnetVersion>>()!;
    }
}
