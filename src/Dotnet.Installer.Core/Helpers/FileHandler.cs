using System.Security.Cryptography;
using CliWrap;

namespace Dotnet.Installer.Core.Helpers;

public static class FileHandler
{
    public static async Task<string> GetFileHash(string filepath)
    {
        await using var readerStream = File.OpenRead(filepath);
        var result = await SHA256.HashDataAsync(readerStream);
        return Convert.ToHexString(result).ToLower();
    }

    public static async Task<string> DownloadFile(Uri url, string destinationDirectory)
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

    public static async Task ExtractDeb(string debPath, string destinationDirectory)
    {
        var scratchDirectory = Path.Combine(destinationDirectory, "scratch");

        await Cli.Wrap("dpkg")
            .WithArguments([ "--extract", debPath, scratchDirectory ])
            .WithWorkingDirectory(destinationDirectory)
            .ExecuteAsync();
        
        var files = DirectoryHandler.MoveDirectory($"{scratchDirectory}/usr/lib/dotnet", destinationDirectory);

        var packageName = Path.GetFileNameWithoutExtension(debPath).Split('_').First();
        await SavePackageContentToFile(files, destinationDirectory, packageName);

        Directory.Delete(scratchDirectory, recursive: true);
    }

    private static async Task SavePackageContentToFile(IEnumerable<string> files, string installationDirectory, string packageName)
    {
        var registrationFile = Path.Combine(installationDirectory, $"{packageName}.files");
        await File.WriteAllLinesAsync(registrationFile, files);
    }
}
