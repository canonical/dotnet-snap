using System.Text;
using CliWrap;
using Dotnet.Installer.Core.Extensions;
using Dotnet.Installer.Core.Services.Contracts;

namespace Dotnet.Installer.Core.Services.Implementations;

public class FileService : IFileService
{
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    /// <summary>
    /// Iterate through a Component's mount-points and resolve their sources from the snap file-system.
    /// If any of these mount-points contain a wild-carded version directory, e.g. 8.0.*, then
    /// the method will look for a directory in the source that starts with '8.0.'.
    /// </summary>
    /// <param name="root">The .NET installation root directory in the snap file system.</param>
    /// <param name="targets">The mount-points read from the online installer manifest.</param>
    /// <returns>A dictionary containing each source-target relationship, with the target as the key.</returns>
    public IDictionary<string, string> ResolveMountPoints(string root, IEnumerable<string> targets)
    {
        var result = new Dictionary<string, string>();
        foreach (var target in targets)
        {
            var source = Path.Join(root, target);
            var resolvedTarget = target;
            
            // Resolve wildcard if it exists
            if (target.Contains('*'))
            {
                var path = Path.Combine(root, target).Split(Path.DirectorySeparatorChar);
                var wildCardedDirectory = path.Last();
                var searchPath = string.Join(Path.DirectorySeparatorChar, path[..^1]);
                
                foreach (var directory in Directory.GetDirectories(searchPath))
                {
                    var indexOfWildcard = wildCardedDirectory.IndexOf('*');
                    if (directory.Contains(wildCardedDirectory[..indexOfWildcard]))
                    {
                        source = directory;
                        resolvedTarget = target.Replace("*",
                            directory.Split(Path.DirectorySeparatorChar).Last().Split('.').Last());
                        break;
                    }
                }
            }
            
            result.Add(resolvedTarget, source);
        }

        return result;
    }

    /// <summary>
    /// Iterates through a list of resolved mount-points and bind-mounts them.
    /// </summary>
    /// <param name="root">The target .NET installation root directory.</param>
    /// <param name="mountPoints">The dictionary of source-target relationships.</param>
    /// <exception cref="ApplicationException">When the bind-mount fails.</exception>
    public async Task ExecuteMountPoints(string root, IDictionary<string, string> mountPoints)
    {
        foreach (var (relativeTarget, source) in mountPoints)
        {
            var target = Path.Join(root, relativeTarget);

            if (!Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
            }

            if (Directory.GetFiles(target).Length != 0)
            {
                // Directory is not empty, check if there is already a bind-mount on it.
                var procMounts = await File.ReadAllTextAsync(Path.Combine("/", "proc", "mounts"));
                if (procMounts.Contains(target))
                    continue;
            }

            var standardOutput = new StringBuilder();
            var standardError = new StringBuilder();
            
            var command = Cli.Wrap("mount")
                .WithArguments(["--bind", source, target])
                .WithValidation(CommandResultValidation.None)
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(standardOutput))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(standardError))
                .Elevate();

            var result = await command.ExecuteAsync();
            if (result.IsSuccess) continue;

            throw new ApplicationException(standardError.ToString());
        }
    }

    /// <summary>
    /// Iterates through a list of resolved mount-points and unmounts them.
    /// </summary>
    /// <param name="root">The target .NET installation root directory.</param>
    /// <param name="mountPoints">The dictionary of source-target relationships.</param>
    /// <exception cref="ApplicationException">When unmount fails.</exception>
    public async Task RemoveMountPoints(string root, IDictionary<string, string> mountPoints)
    {
        foreach (var (relativeTarget, source) in mountPoints)
        {
            var target = Path.Join(root, relativeTarget);

            if (!Directory.Exists(target))
            {
                throw new ApplicationException($"The directory {target} does not exist.");
            }
            
            if (Directory.GetFiles(target).Length != 0)
            {
                // Directory is not empty, check if there is already a bind-mount on it.
                var procMounts = await File.ReadAllTextAsync(Path.Combine("/", "proc", "mounts"));
                if (!procMounts.Contains(target))
                    continue;
            }

            var standardOutput = new StringBuilder();
            var standardError = new StringBuilder();
            
            var command = Cli.Wrap("umount")
                .WithArguments([target])
                .WithValidation(CommandResultValidation.None)
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(standardOutput))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(standardError))
                .Elevate();

            var result = await command.ExecuteAsync();
            if (result.IsSuccess) continue;
            
            throw new ApplicationException(standardError.ToString());
        }
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
}
