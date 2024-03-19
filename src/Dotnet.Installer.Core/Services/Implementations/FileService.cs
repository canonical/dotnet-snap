using Dotnet.Installer.Core;
using Dotnet.Installer.Core.Services.Contracts;

namespace Dotnet.Installer.Core.Services.Implementations;

public class FileService : IFileService
{
    public Stream OpenRead(string path)
    {
        return File.OpenRead(path);
    }
}
