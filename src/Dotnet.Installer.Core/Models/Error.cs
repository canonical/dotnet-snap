namespace Dotnet.Installer.Core;

public enum Error
{
    NeedsSudo = 10,     // Application needs elevated permissions
    VersionTooHigh = 11 // Version installed is higher than available host version
}
