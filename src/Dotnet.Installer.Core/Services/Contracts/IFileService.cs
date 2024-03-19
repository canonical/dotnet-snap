namespace Dotnet.Installer.Core.Services.Contracts;

public interface IFileService
{
    void DeleteFile(string path);
    Task<string> DownloadFile(Uri url, string destinationDirectory);
    bool FileExists(string path);
    Task ExtractDeb(string debPath, string destinationDirectory);
    Task<string> GetFileHash(string filePath);
    IEnumerable<string> MoveDirectory(string sourceDirectory, string destinationDirectory);
    Stream OpenRead(string path);
    Task<string[]> ReadAllLines(string fileName);
    void RemoveEmptyDirectories(string root);
}
