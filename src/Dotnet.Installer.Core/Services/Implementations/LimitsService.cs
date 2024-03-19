using System.Text.Json;
using System.Text.Json.Serialization;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Implementations;

public class LimitsService : ILimitsService
{    
    public DotnetVersion Runtime { get; init; }
    public IEnumerable<DotnetVersion> Sdk { get; init; }

    public LimitsService(IFileService fileService)
    {
        // Read limits file
        var limitsFilePath = Environment.GetEnvironmentVariable("LIMITS_PATH") ?? string.Empty;

        if (!File.Exists(limitsFilePath)) throw new ApplicationException("Limits file could not be found.");
        
        using var fs = fileService.OpenRead(limitsFilePath);
        var limits = JsonDocument.Parse(fs)
            ?? throw new ApplicationException("Could not read limits file.");
        
        Runtime = limits.RootElement.GetProperty("runtime").Deserialize<DotnetVersion>()
            ?? throw new ApplicationException("Could not read Runtime limit.");

        Sdk = limits.RootElement.GetProperty("sdk").Deserialize<IEnumerable<DotnetVersion>>()
            ?? throw new ApplicationException("Could not read SDK limit.");
    }
}
