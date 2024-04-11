namespace Dotnet.Installer.Core.Models.Events;

public class InstallingPackageChangedEventArgs(Package package, Component component) : EventArgs
{
    public Package Package { get; set; } = package;
    public Component Component { get; set; } = component;
}
