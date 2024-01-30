namespace Dotnet.Installer.Domain;

public static class DirectoryHandler
{
    public static IEnumerable<string> MoveDirectory(string sourceDirectory, string destinationDirectory)
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
            file.MoveTo(path);
        }

        // Copy subdirectories and their contents to the new location.
        var subdirs = dir.GetDirectories();
        foreach (var subdir in subdirs)
        {
            var path = Path.Combine(destinationDirectory, subdir.Name);
            result.AddRange(MoveDirectory(subdir.FullName, path));
        }

        return result;
    }

    public static void RemoveEmptyDirectories(string root)
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

    public static bool IsDirectoryEmpty(string path)
    {
        return Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0;
    }
}
