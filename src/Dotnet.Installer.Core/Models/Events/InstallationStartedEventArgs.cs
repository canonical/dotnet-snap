namespace Dotnet.Installer.Core.Models.Events;

public class InstallationStartedEventArgs(string key) : EventArgs
{
    public string Key { get; set; } = key;
}
