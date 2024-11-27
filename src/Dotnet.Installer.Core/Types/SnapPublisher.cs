using System.Text.Json.Serialization;

namespace Dotnet.Installer.Core.Types;

/// <summary>
/// Information about the publisher of a snap
/// </summary>
/// <param name="Id">The snap store account identifier of the publisher</param>
/// <param name="Username">The snap store account name of the publisher</param>
/// <param name="DisplayName">The display name of the publisher in the snap store</param>
/// <param name="Validation">
/// The validation status of the publisher in the snap store. (e.g., <c>"verified"</c>, <c>"unproven"</c>)
/// </param>
/// <seealso href="https://snapcraft.io/docs/snapd-api#heading--find"/>
public record SnapPublisher(
    string Id,
    string Username,
    string DisplayName,
    string Validation)
{
    /// <summary>
    /// <see langword="true"/> if <see cref="Validation"/> is <c>"verified"</c>; otherwise <see langword="false"/>
    /// </summary>
    [JsonIgnore]
    public bool IsVerified => Validation.Equals("verified");
}
