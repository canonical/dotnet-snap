namespace Dotnet.Installer.Core.Services.Contracts;

public interface IFileService
{
    Task<string> DownloadFile(Uri url, string destinationDirectory);
    bool Exists(string path);
    Task ExtractDeb(string debPath, string destinationDirectory);
    Task<string> GetFileHash(string filePath);
    IEnumerable<string> MoveDirectory(string sourceDirectory, string destinationDirectory);
    Stream OpenRead(string path);
    void RemoveEmptyDirectories(string root);
}
