namespace Dotnet.Installer.Core.Services.Contracts;

public interface IFileService
{
    Stream OpenRead(string path);
}
