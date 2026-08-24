using System.CommandLine;
using System.Text;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;
using Spectre.Console;

namespace Dotnet.Installer.Console.Commands;

public class InfoCommand : Command
{
    private readonly IManifestService _manifestService;
    private readonly ISnapService _snapService;
    private readonly ILogger _logger;

    public InfoCommand(
        IManifestService manifestService,
        ISnapService snapService,
        ILogger logger)
        : base("info", "Shows detailed information about a .NET component")
    {
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _snapService = snapService ?? throw new ArgumentNullException(nameof(snapService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var componentArgument = new Argument<string>(
            name: "component",
            description: "The .NET component name ('runtime', 'aspnetcore-runtime', 'sdk').")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var versionArgument = new Argument<string>(
            name: "version",
            description: "The .NET component version (e.g. '8' or '8.0'), lts, latest.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        AddArgument(componentArgument);
        AddArgument(versionArgument);

        this.SetHandler(Handle, componentArgument, versionArgument);
    }

    private async Task Handle(string? componentName, string? version)
    {
        if (string.IsNullOrWhiteSpace(componentName))
        {
            _logger.LogError("Missing component name. " +
                             $"Valid components are: {Constants.DotnetRuntimeComponentName}, " +
                             $"{Constants.AspnetCoreRuntimeComponentName}, {Constants.SdkComponentName}. " +
                             "Example: dotnet installer info sdk lts");
            Environment.Exit(-1);
            return;
        }

        if (!Constants.IsValidComponentName(componentName))
        {
            _logger.LogError($"Invalid component name '{componentName}'. " +
                             $"Valid components are: {Constants.DotnetRuntimeComponentName}, " +
                             $"{Constants.AspnetCoreRuntimeComponentName}, {Constants.SdkComponentName}. " +
                             "Example: dotnet installer info sdk lts");
            Environment.Exit(-1);
            return;
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            _logger.LogError($"Missing version for component '{componentName}'. " +
                             "Valid versions are: a major version (e.g. '8' or '8.0'), lts, latest. " +
                             $"Example: dotnet installer info {componentName} lts");
            Environment.Exit(-1);
            return;
        }

#if INCLUDE_PRERELEASE
        const bool includePrerelease = true;
#else
        const bool includePrerelease = false;
#endif
        try
        {
            await _manifestService.Initialize(includeUnsupported: true, includePrerelease);

            var component = _manifestService.MatchRemoteComponent(componentName, version);
            if (component is null)
            {
                var availableVersions = _manifestService.GetAvailableVersions(componentName);

                if (availableVersions.Count > 0)
                {
                    _logger.LogError($"The requested version '{version}' does not exist for component '{componentName}'. " +
                                     $"Available versions are: {string.Join(", ", availableVersions)}. " +
                                     $"Example: dotnet installer info {componentName} {availableVersions.First()}");
                }
                else
                {
                    _logger.LogError($"The requested component '{componentName} {version}' does not exist. " +
                                     $"Valid components are: {Constants.DotnetRuntimeComponentName}, " +
                                     $"{Constants.AspnetCoreRuntimeComponentName}, {Constants.SdkComponentName}. " +
                                     "Example: dotnet installer info sdk lts");
                }
                Environment.Exit(-1);
            }

            var installed = _manifestService.Local.Any(c => c.Key == component.Key);
            var (actualVersion, channel) = await GetSnapInfo(component.Key, installed);

            RenderInfo(component, installed, actualVersion, channel);
            RenderDependencies(component);
            if (installed) RenderReverseDependencies(component.Key);
        }
        catch (ApplicationException ex)
        {
            _logger.LogError(ex.Message);
            Environment.Exit(-1);
        }
    }

    private async Task<(DotnetVersion? Version, string Channel)> GetSnapInfo(string key, bool installed)
    {
        try
        {
            SnapInfo? snap;
            if (installed)
            {
                snap = await _snapService.GetInstalledSnap(key);
            }
            else
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                snap = await _snapService.FindSnap(key, cts.Token);
            }

            if (snap is null) return (null, "unknown");
            return (snap.ParseVersionAsDotnetVersion(), snap.Channel);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception.Message);
            return (null, "unknown");
        }
    }

    private void RenderInfo(Component component, bool installed, DotnetVersion? version, string channel)
    {
        var title = new StringBuilder(component.Description);
        if (version is not null) title.Append($" {version}");
        else title.Append($" {component.MajorVersion}");

        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow("Status:", installed ? "[green]Installed[/]" : "[blue]Available[/]");
        grid.AddRow("LTS:", component.IsLts ? "Yes" : "No");
        grid.AddRow("Grade:", component.Grade.ToString());
        grid.AddRow("Channel:", channel);
        grid.AddRow("EOL:", OutputFormat.Eol(component, installed));

        var panel = new Panel(grid)
        {
            Header = new PanelHeader(title.ToString()),
            Expand = true
        };

        AnsiConsole.Write(panel);
    }

    private void RenderDependencies(Component component)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Markup("[bold]Dependencies[/]"));
        AnsiConsole.WriteLine();

        if (!component.Dependencies.Any())
        {
            AnsiConsole.WriteLine("None");
            return;
        }

        var tree = new Tree(string.Empty);
        foreach (var dependencyKey in component.Dependencies)
        {
            var dependency = _manifestService.Merged.FirstOrDefault(c => c.Key == dependencyKey);
            var label = dependency is null ? dependencyKey : $"{dependency.Description} {dependency.MajorVersion}";
            tree.AddNode(label);
        }

        AnsiConsole.Write(tree);
    }

    private void RenderReverseDependencies(string key)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Markup("[bold]Required by[/]"));
        AnsiConsole.WriteLine();

        var reverseDependencies = new DependencyTree(_manifestService.Local).GetReverseDependencies(key);
        if (reverseDependencies.Count == 0)
        {
            AnsiConsole.WriteLine("Nothing installed depends on this component.");
            return;
        }

        var tree = new Tree(string.Empty);
        foreach (var dependency in reverseDependencies)
        {
            tree.AddNode($"{dependency.Description} {dependency.MajorVersion}");
        }

        AnsiConsole.Write(tree);
    }
}
