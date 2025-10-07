namespace Dotnet.Installer.Core.Tests.Types;

public class NativeTests
{
    [Fact]
    public void GetCurrentEffectiveUserId_WhenCalled_ShouldReturnNonNegativeValue()
    {
        // Act
        var uid = Dotnet.Installer.Core.Types.Native.GetCurrentEffectiveUserId();

        // Assert
        Assert.True(uid >= 0, "The effective user ID should be non-negative.");
    }
}
