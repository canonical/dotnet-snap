using System.CommandLine;
using System.Text;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;
using Spectre.Console;

namespace Dotnet.Installer.Console.Commands;

public class ListCommand : Command
{
    private readonly IFileService _fileService;
    private readonly IManifestService _manifestService;

    public ListCommand(
        IFileService fileService,
        IManifestService manifestService) : base("list", "List installed and available .NET versions")
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));

        var includeUnsupportedOption = new Option<bool>(
            name: "--all",
            description: "Include unsupported .NET components in the list output.")
            {
                IsRequired = false
            };

        AddOption(includeUnsupportedOption);

        this.SetHandler(Handle, includeUnsupportedOption);
    }

    private async Task Handle(bool includeUnsupported)
    {
        try
        {
            await _manifestService.Initialize(includeUnsupported);

            var table = new Table();

            table.AddColumn(new TableColumn("Version"));
            table.AddColumn(new TableColumn(".NET Runtime"));
            table.AddColumn(new TableColumn("ASP.NET Core Runtime"));
            table.AddColumn(new TableColumn("SDK"));
            table.AddColumn(new TableColumn("End of Life"));

            foreach (var majorVersionGroup in _manifestService.Merged
                         .GroupBy(c => c.MajorVersion)
                         .OrderBy(c => c.Key))
            {
                var components = new Dictionary<string, (bool Installed, DotnetVersion? Version)>();

                var installedComponents = majorVersionGroup.Where(c => c.Installation is not null);
                foreach (var installedComponent in installedComponents)
                {
                    var version = installedComponent.GetDotnetVersion(_manifestService, _fileService);
                    components[installedComponent.Name] = (Installed: true, Version: version);
                }

                var endOfLife = majorVersionGroup.First().EndOfLife;
                var isEndOfLife = endOfLife < DateTime.Now;

                var dotnetVersionStringBuilder = new StringBuilder();
                dotnetVersionStringBuilder.Append($".NET {majorVersionGroup.Key}");
                if (majorVersionGroup.First().IsLts)
                    dotnetVersionStringBuilder.Append(" LTS");

                var dotNetRuntimeStatus = ComponentStatus(
                    Constants.DotnetRuntimeComponentName,
                    majorVersionGroup.Key,
                    isEndOfLife);
                var aspNetCoreRuntimeStatus = ComponentStatus(
                    Constants.AspnetCoreRuntimeComponentName,
                    majorVersionGroup.Key,
                    isEndOfLife);
                var sdkStatus = ComponentStatus(Constants.SdkComponentName,
                    majorVersionGroup.Key,
                    isEndOfLife);

                var eolStringBuilder = new StringBuilder();
                eolStringBuilder.Append(isEndOfLife ? "[bold red]" : "[bold green]");
                eolStringBuilder.Append(endOfLife.ToShortDateString());
                eolStringBuilder.Append("[/]");

                table.AddRow(
                    dotnetVersionStringBuilder.ToString(),
                    dotNetRuntimeStatus,
                    aspNetCoreRuntimeStatus,
                    sdkStatus,
                    eolStringBuilder.ToString());

                continue;

                string ComponentStatus(string key, int majorVersion, bool isEol)
                {
                    if (components.TryGetValue(key, out var component))
                    {
                        return $"[bold green]Installed [[{component.Version}]][/]";
                    }

                    var available = _manifestService.Remote.Any(c => c.Name == key && c.MajorVersion == majorVersion);

                    if (!available)
                    {
                        return "[grey]-[/]";
                    }

                    return isEol ? "[grey]Available[/]" : "[bold blue]Available[/]";
                }
            }

            AnsiConsole.Write(table);
        }
        catch (ApplicationException ex)
        {
            Log.Error(ex.Message);
            Environment.Exit(-1);
        }
    }
}
