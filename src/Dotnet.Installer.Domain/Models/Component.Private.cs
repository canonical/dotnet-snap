using System.Diagnostics;
using System.Security.Cryptography;

namespace Dotnet.Installer.Domain.Models;

public partial class Component
{
    private static async Task<string> GetFileHash(string filepath)
    {
        using var readerStream = File.OpenRead(filepath);
        var result = await SHA256.HashDataAsync(readerStream);
        return Convert.ToHexString(result).ToLower();
    }

    private static async Task DownloadFile(HttpClient client, Uri url, string destination)
    {
        await using var remoteFileStream = await client.GetStreamAsync(url);

        try
        {
            await using var writerStream = File.OpenWrite(destination);
            await remoteFileStream.CopyToAsync(writerStream);
        }
        catch (UnauthorizedAccessException)
        {
            Console.Error.WriteLine("ERROR: Unauthorized access. Maybe run with sudo?");
        }
    }

    private static async Task ExtractFile(string filePath, string destinationDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/tar",
            Arguments = $"xzf {filePath} -C {destinationDirectory}",
            RedirectStandardInput = false,
            CreateNoWindow = true
        };

        var process = Process.Start(psi);

        await process!.WaitForExitAsync();
    }
}
