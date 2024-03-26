namespace Dotnet.Installer.Core.Models.Events;

public class InstallingPackageChangedEventArgs(Package package) : EventArgs
{
    public Package Package { get; set; } = package;
}
