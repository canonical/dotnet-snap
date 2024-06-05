namespace Dotnet.Installer.Core.Services.Contracts;

public interface IFileService
{
    bool FileExists(string path);
    IDictionary<string, string> ResolveMountPoints(string root, IEnumerable<string> targets);
    Task ExecuteMountPoints(string root, IDictionary<string, string> mountPoints);
    Task RemoveMountPoints(string root, IDictionary<string, string> mountPoints);
    void RemoveEmptyDirectories(string root);
}
