using System.Security.Cryptography;
using CliWrap;
using Dotnet.Installer.Core.Services.Contracts;

namespace Dotnet.Installer.Core.Services.Implementations;

public class FileService : IFileService
{
    public void DeleteFile(string path)
    {
        File.Delete(path);
    }

    public async Task<string> DownloadFile(Uri url, string destinationDirectory)
    {
        using var client = new HttpClient();
        await using var remoteFileStream = await client.GetStreamAsync(url);

        var fileName = Path.Combine(destinationDirectory, url.Segments.Last());

        try
        {
            if (File.Exists(fileName)) File.Delete(fileName);
            await using var writerStream = File.OpenWrite(fileName);
            await remoteFileStream.CopyToAsync(writerStream);
        }
        catch (UnauthorizedAccessException)
        {
            await Console.Error.WriteLineAsync("ERROR: Unauthorized access. Maybe run with sudo?");
        }

        return fileName;
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public async Task ExtractDeb(string debPath, string destinationDirectory)
    {
        var scratchDirectory = Path.Combine(destinationDirectory, "scratch");

        await Cli.Wrap("dpkg")
            .WithArguments([ "--extract", debPath, scratchDirectory ])
            .WithWorkingDirectory(destinationDirectory)
            .ExecuteAsync();
        
        var files = MoveDirectory($"{scratchDirectory}/usr/lib/dotnet", destinationDirectory);

        var packageName = Path.GetFileNameWithoutExtension(debPath).Split('_').First();
        await SavePackageContentToFile(files, destinationDirectory, packageName);

        Directory.Delete(scratchDirectory, recursive: true);
    }

    public async Task<string> GetFileHash(string filePath)
    {
        await using var readerStream = File.OpenRead(filePath);
        var result = await SHA256.HashDataAsync(readerStream);
        return Convert.ToHexString(result).ToLower();
    }

    public IEnumerable<string> MoveDirectory(string sourceDirectory, string destinationDirectory)
    {
        var result = new List<string>();
        var dir = new DirectoryInfo(sourceDirectory);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                $"The source directory does not exist ({sourceDirectory})."
            );
        }

        // If the destination directory does not exist, create it.
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        // Get the files in the directory and copy them to the new location.
        var files = dir.GetFiles();
        foreach (var file in files)
        {
            var path = Path.Combine(destinationDirectory, file.Name);
            result.Add(path);
            file.MoveTo(path, overwrite: true);
        }

        // Copy subdirectories and their contents to the new location.
        var subDirs = dir.GetDirectories();
        foreach (var subDir in subDirs)
        {
            var path = Path.Combine(destinationDirectory, subDir.Name);
            result.AddRange(MoveDirectory(subDir.FullName, path));
        }

        return result;
    }

    public Stream OpenRead(string path)
    {
        return File.OpenRead(path);
    }

    public Task<string[]> ReadAllLines(string fileName)
    {
        return File.ReadAllLinesAsync(fileName);
    }

    public void RemoveEmptyDirectories(string root)
    {
        var dir = new DirectoryInfo(root);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                $"The directory does not exist ({root})."
            );
        }

        // Recursively search for empty directories and remove them
        foreach (var directory in dir.GetDirectories())
        {
            RemoveEmptyDirectories(directory.FullName);

            if (IsDirectoryEmpty(directory.FullName))
            {
                directory.Delete();
            }
        }
    }

    private static bool IsDirectoryEmpty(string path)
    {
        return Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0;
    }
    
    private static async Task SavePackageContentToFile(IEnumerable<string> files, string installationDirectory, string packageName)
    {
        var registrationFile = Path.Combine(installationDirectory, $"{packageName}.files");
        await File.WriteAllLinesAsync(registrationFile, files);
    }
}
