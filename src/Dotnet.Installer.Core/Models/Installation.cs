namespace Dotnet.Installer.Core.Models;

public class Installation
{
    public DateTimeOffset InstalledAt { get; set; }
    public bool IsRootComponent { get; set; }
}
