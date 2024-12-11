namespace Dotnet.Installer.Core.Types;

/// <summary>
/// Publishing information about a snap
/// </summary>
/// <param name="Name">The snap name</param>
/// <param name="Version">String representation of the snap version published in <see cref="Channel"/></param>
/// <param name="Revision">A number representing the snap revision published in <see cref="Channel"/></param>
/// <param name="Channel">Name of the channel this snap is published to</param>
/// <param name="Publisher">Information about the publisher of this snap</param>
/// <seealso href="https://snapcraft.io/docs/snapd-api#heading--find"/>
public record SnapInfo(
    string Name,
    string Version,
    string Revision,
    string Channel,
    SnapPublisher Publisher)
{
    public DotnetVersion ParseVersionAsDotnetVersion()
    {
        try
        {
            return DotnetVersion.Parse(Version.Split("+git")[0]);
        }
        catch (Exception exception)
        {
            throw new ApplicationException(
                message: $"Could not parse .NET version ({Version}) from snap {Name}",
                innerException: exception);
        }
    }
}
