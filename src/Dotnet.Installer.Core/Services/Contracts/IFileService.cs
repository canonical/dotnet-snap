using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Contracts;

public interface IFileService
{
    IEnumerable<string> EnumerateContentSnapMountFiles(string contentSnapName);
    bool FileExists(string path);
    void InstallSystemdMountUnit(string unitPath);
    void UninstallSystemdMountUnit(string unitName);
    void InstallSystemdPathUnit(string snapName);
    void UninstallSystemdPathUnit(string snapName);
    Task PlaceLinkageFile(string contentSnapName);
    Task PlaceUnitsFile(string snapConfigDirLocation, string contentSnapName, string units);
    Task<string[]> ReadUnitsFile(string snapConfigDirLocation, string contentSnapName);
    void DeleteUnitsFile(string snapConfigDirLocation, string contentSnapName);
    DotnetVersion ReadDotVersionFile(string dotNetRoot, string componentPath, int majorVersion);
    void RemoveEmptyDirectories(string root);
}
