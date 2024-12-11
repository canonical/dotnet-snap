using System.Text;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Implementations;

public class FileService : IFileService
{
    /// <summary>
    /// Enumerates the .mount unit files for a .NET content snap by looking at the files present in the directory
    /// <c>$SNAP/mounts</c>.
    /// </summary>
    /// <param name="contentSnapName">The .NET content snap name.</param>
    /// <returns>A list of absolute paths of the .mount unit files for <c>contentSnapName</c>.</returns>
    public IEnumerable<string> EnumerateContentSnapMountFiles(string contentSnapName)
    {
        return Directory.EnumerateFiles(Path.Join("/", "snap", contentSnapName, "current", "mounts"));
    }

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
    /// The linkage file is a flag that marks that a .NET content snap is present in a system
    /// and the .NET installer tool is tracking its updates for eventual .mount path updates
    /// on content snap refreshes. Check each .NET content snap's refresh hook for the code that
    /// checks for the present of the <c>$SNAP_COMMON/dotnet-installer</c> file.
    /// </summary>
    /// <param name="contentSnapName">The name of the .NET content snap.</param>
    /// <returns></returns>
    public Task PlaceLinkageFile(string contentSnapName)
    {
        return File.WriteAllTextAsync(
            Path.Join("/", "var", "snap", contentSnapName, "common", "dotnet-installer"),
            "installer linkage ok\n",
            Encoding.UTF8);
    }

    public Task PlaceUnitsFile(string snapConfigDirLocation, string contentSnapName, string units)
    {
        var mountsFileName = $"{contentSnapName}.mounts";
        return File.WriteAllTextAsync(
            Path.Join(snapConfigDirLocation, mountsFileName),
            contents: units,
            Encoding.UTF8);
    }

    public Task<string[]> ReadUnitsFile(string snapConfigDirLocation, string contentSnapName)
    {
        var mountsFileName = $"{contentSnapName}.mounts";
        return File.ReadAllLinesAsync(Path.Join(snapConfigDirLocation, mountsFileName));
    }

    public void DeleteUnitsFile(string snapConfigDirLocation, string contentSnapName)
    {
        var mountsFileName = $"{contentSnapName}.mounts";
        File.Delete(Path.Join(snapConfigDirLocation, mountsFileName));
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
