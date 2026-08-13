using System.CommandLine;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Services.Contracts;
using Spectre.Console;

namespace Dotnet.Installer.Console.Commands;

public class SearchCommand : Command
{
    private readonly IManifestService _manifestService;
    private readonly ILogger _logger;

    public SearchCommand(
        IManifestService manifestService,
        ILogger logger)
        : base("search", "Search for .NET components")
    {
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var installedOption = new Option<bool>(
            name: "--installed",
            description: "Show only installed components.")
        {
            IsRequired = false
        };
        var allOption = new Option<bool>(
            name: "--all",
            description: "Show installed and available components.")
        {
            IsRequired = false
        };
        var ltsOption = new Option<bool>(
            name: "--lts",
            description: "Show only LTS components.")
        {
            IsRequired = false
        };

        AddOption(installedOption);
        AddOption(allOption);
        AddOption(ltsOption);

        this.SetHandler(Handle, installedOption, allOption, ltsOption);
    }

    private async Task Handle(bool installedOnly, bool all, bool ltsOnly)
    {
#if INCLUDE_PRERELEASE
        const bool includePrerelease = true;
#else
        const bool includePrerelease = false;
#endif
        try
        {
            await _manifestService.Initialize(includeUnsupported: true, includePrerelease);

            IEnumerable<Component> components;
            if (installedOnly) components = _manifestService.Local;
            else if (all) components = _manifestService.Merged;
            else components = _manifestService.Remote;

            if (ltsOnly) components = components.Where(c => c.IsLts);

            var table = new Table();
            table.AddColumn("Component");
            table.AddColumn("Version");
            table.AddColumn("Status");
            table.AddColumn("EOL");

            var results = components
                .OrderByDescending(c => c.MajorVersion)
                .ThenBy(c => c.Name)
                .ToList();

            if (results.Count == 0)
            {
                var message = installedOnly
                    ? "You don't have any .NET components installed. Run 'dotnet installer install sdk lts' to install the latest LTS SDK."
                    : "No .NET components found.";
                AnsiConsole.WriteLine(message);
                return;
            }

            foreach (var component in results)
            {
                table.AddRow(
                    component.Name,
                    component.MajorVersion.ToString(),
                    OutputFormat.Status(component),
                    OutputFormat.Eol(component, component.IsInstalled));
            }

            AnsiConsole.Write(table);
        }
        catch (ApplicationException ex)
        {
            _logger.LogError(ex.Message);
            Environment.Exit(-1);
        }
    }
}
