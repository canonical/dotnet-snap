namespace Dotnet.Installer.Core.Models.Events;

public class InstallationFinishedEventArgs(string key) : EventArgs
{
    public string Key { get; set; } = key;
}