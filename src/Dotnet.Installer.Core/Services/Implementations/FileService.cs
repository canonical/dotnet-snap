using System.Diagnostics;
using System.Text;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Implementations;

public class FileService : IFileService
{
    /// <inheritdoc cref="File.Exists"/>
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public void InstallSystemdMountUnit(string unitPath)
    {
        var destination = Path.Join("/", "usr", "lib", "systemd", "system");

        File.Copy(unitPath,Path.Join(destination, unitPath.Split('/').Last()), overwrite: true);
    }

    public void UninstallSystemdMountUnit(string unitName)
    {
        var unitPath = Path.Join("/", "usr", "lib", "systemd", "system", unitName);

        if (File.Exists(unitPath)) File.Delete(unitPath);
    }

    public void InstallSystemdPathUnit(string snapName)
    {
        var destination = Path.Join("/", "usr", "lib", "systemd", "system");
        var unitsLocation = Path.Join("/", "snap", "dotnet", "current", "Scripts");
        var unitFilesInLocation = Directory.GetFiles(unitsLocation, "{SNAP}*");
        foreach (var unitFile in unitFilesInLocation)
        {
            var originFilePath = unitFile.Replace("{SNAP}", snapName);
            var fileContent = File.ReadAllText(unitFile).Replace("{SNAP}", snapName);
            File.WriteAllText(Path.Join(destination, originFilePath.Split('/').Last()), fileContent, Encoding.UTF8);
        }
    }

    public void UninstallSystemdPathUnit(string snapName)
    {
        var destination = Path.Join("/", "usr", "lib", "systemd", "system");
        var unitsLocation = Path.Join("/", "snap", "dotnet", "current", "Scripts");
        var unitFilesInLocation = Directory.GetFiles(unitsLocation, "{SNAP}*");
        foreach (var unitFile in unitFilesInLocation)
        {
            var originFilePath = unitFile.Replace("{SNAP}", snapName);
            File.Delete(Path.Join(destination, originFilePath.Split('/').Last()));
        }
    }

    /// <summary>
    /// Iterates recursively through all the directories within <c>root</c> and deletes any empty directories found.
    /// <c>root</c> will NOT be deleted even if it ends up being an empty directory itself.
    /// </summary>
    /// <param name="root">A path to the top-level directory.</param>
    /// <exception cref="DirectoryNotFoundException">When <c>root</c> does not exist.</exception>
    public void RemoveEmptyDirectories(string root)
    {
        var dir = new DirectoryInfo(root);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                $"The directory does not exist ({root})."
            );
        }

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
}
