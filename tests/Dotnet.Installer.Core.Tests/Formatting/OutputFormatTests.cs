using Dotnet.Installer.Console;
using Dotnet.Installer.Core.Models;

namespace Dotnet.Installer.Core.Tests.Formatting;

public class OutputFormatTests
{
    [Fact]
    public void Status_WhenInstalled_ShouldContainInstalled()
    {
        var component = new Component
        {
            Dependencies = [],
            Description = "SDK",
            Key = "sdk-9",
            Name = "sdk",
            MajorVersion = 9,
            IsLts = false,
            Grade = Grade.Rtm,
            EndOfLife = DateTime.Now,
            Installation = new Installation(DateTimeOffset.UtcNow)
        };

        var status = OutputFormat.Status(component);

        Assert.Contains("Installed", status);
    }

    [Fact]
    public void Status_WhenAvailable_ShouldContainAvailable()
    {
        var component = new Component
        {
            Dependencies = [],
            Description = "SDK",
            Key = "sdk-9",
            Name = "sdk",
            MajorVersion = 9,
            IsLts = false,
            Grade = Grade.Rtm,
            EndOfLife = DateTime.Now
        };

        var status = OutputFormat.Status(component);

        Assert.Contains("Available", status);
    }

    [Fact]
    public void Eol_WhenEndOfLifeIsNull_ShouldReturnDash()
    {
        var component = new Component
        {
            Dependencies = [],
            Description = "SDK",
            Key = "sdk-9",
            Name = "sdk",
            MajorVersion = 9,
            IsLts = false,
            Grade = Grade.Rtm,
            EndOfLife = null
        };

        var eol = OutputFormat.Eol(component, installed: false);

        Assert.Contains("-", eol);
    }

    [Fact]
    public void Eol_WhenComponentIsPreview_ShouldIndicatePreview()
    {
        var component = new Component
        {
            Dependencies = [],
            Description = "SDK",
            Key = "sdk-11",
            Name = "sdk",
            MajorVersion = 11,
            IsLts = false,
            Grade = Grade.Preview,
            EndOfLife = null
        };

        var eol = OutputFormat.Eol(component, installed: false);

        Assert.Contains("Preview", eol);
    }

    [Fact]
    public void Eol_WhenEndOfLifeIsPast_ShouldUseRedFormatting()
    {
        var component = new Component
        {
            Dependencies = [],
            Description = "SDK",
            Key = "sdk-6",
            Name = "sdk",
            MajorVersion = 6,
            IsLts = true,
            Grade = Grade.Rtm,
            EndOfLife = DateTime.Now.AddDays(-10)
        };

        var eol = OutputFormat.Eol(component, installed: false);

        Assert.Contains("red", eol);
    }
}
