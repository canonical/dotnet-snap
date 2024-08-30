namespace Dotnet.Installer.Core.Services.Contracts;

public interface IFileService
{
    bool FileExists(string path);
    void InstallSystemdMountUnit(string unitPath);
    void UninstallSystemdMountUnit(string unitName);
    void InstallSystemdPathUnit(string snapName);
    void UninstallSystemdPathUnit(string snapName);
    void RemoveEmptyDirectories(string root);
}
