using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Console;

public static class OutputFormat
{
    public static string Status(Component component, DotnetVersion? version = null)
    {
        var status = component.IsInstalled ? "[green][bold]Installed[/]" : "[blue][bold]Available[/]";
        status += version is null ? "[/]" : $" [[{version}]][/]";
        return status;
    }

    public static string Eol(Component component, bool installed)
    {
        if (component.Grade == Grade.Preview) return "[yellow]Preview[/]";
        if (component.Grade == Grade.Rc) return "[yellow]RC[/]";

        var endOfLife = component.EndOfLife;
        if (endOfLife is null) return "[grey]-[/]";

        var daysUntilEndOfLife = (endOfLife.Value - DateTime.Now).TotalDays;
        var eolString = $"[{(daysUntilEndOfLife <= 0d ? "bold red" : "green")}]{endOfLife:d}[/]";

        if (installed && daysUntilEndOfLife is < 30d and > 0d)
        {
            eolString += $" [bold yellow]({daysUntilEndOfLife:N0} days left)[/]";
        }
        else if (daysUntilEndOfLife is < 90d and > 0d)
        {
            eolString += $" ({daysUntilEndOfLife:N0} days left)";
        }

        return eolString;
    }
}
