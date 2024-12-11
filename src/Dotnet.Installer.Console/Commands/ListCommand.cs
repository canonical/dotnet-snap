using System.Collections.Immutable;
using System.CommandLine;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;
using Spectre.Console;

namespace Dotnet.Installer.Console.Commands;

public class ListCommand : Command
{
    private readonly IManifestService _manifestService;
    private readonly ISnapService _snapService;
    private readonly ILogger _logger;

    public ListCommand(
        IManifestService manifestService,
        ISnapService snapService,
        ILogger logger) : base("list", "List installed and available .NET versions")
    {
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _snapService = snapService ?? throw new ArgumentNullException(nameof(snapService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var includeUnsupportedOption = new Option<bool>(
            name: "--all",
            description: "Include unsupported .NET components in the list output.")
            {
                IsRequired = false
            };
        
        var timeoutOption = new Option<uint>(
            name: "--timeout",
            description: "The timeout for requesting the version of a .NET component " +
                         "from the snap store in milliseconds (0 = infinite).",
            getDefaultValue: () => 5000)
        {
            IsRequired = false,
        };

        AddOption(includeUnsupportedOption);
        AddOption(timeoutOption);

        this.SetHandler(Handle, includeUnsupportedOption, timeoutOption);
    }

    private async Task Handle(bool includeUnsupported, uint timeoutInMilliseconds)
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

            var components = _manifestService.Merged.ToList();
            Dictionary<string, DotnetVersion?> componentVersions;

            try
            {
                componentVersions = await GetComponentVersions(components, timeoutInMilliseconds).ConfigureAwait(false);

                if (componentVersions.Any(x => x.Value == null
                        && components.First(c => c.Key == x.Key) is not { Installation.IsRootComponent: false }))
                {
                    _logger.LogError("The version numbers of some components can not be displayed due to unexpected " +
                                     "failures. Run this command with --verbose to show more detailed error messages");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Requesting the version numbers for the components from snapd timed out");
                componentVersions = components.ToDictionary(c => c.Key, _ => (DotnetVersion?)null);
            }

            foreach (var majorVersionGroup in components
                         .GroupBy(c => c.MajorVersion)
                         .OrderByDescending(c => c.Key))
            {
                table.AddRow(
                    VersionGroupDisplayName(),
                    ComponentStatus(Constants.DotnetRuntimeComponentName),
                    ComponentStatus(Constants.AspnetCoreRuntimeComponentName),
                    ComponentStatus(Constants.SdkComponentName),
                    EndOfLifeStatus());

                string VersionGroupDisplayName()
                {
                    var isLts = majorVersionGroup.First().IsLts;
                    return $".NET {majorVersionGroup.Key}{(isLts ? " LTS" : "")}";
                }

                string ComponentStatus(string name)
                {
                    var component = majorVersionGroup.FirstOrDefault(
                        c => c.Name == name && c.MajorVersion == majorVersionGroup.Key);

                    if (component is null)
                    {
                        return "[grey]-[/]";
                    }

                    string status = component.IsInstalled ? "[green][bold]Installed[/]" : "[blue][bold]Available[/]";

                    if (!component.IsInstalled || component.IsRoot)
                    {
                        var version = componentVersions[component.Key];
                        status += version is null ? "[/]" : $" [[{version}]][/]";
                    }
                    else
                    {
                        var rootComponent = majorVersionGroup.Single(c => c is { Installation.IsRootComponent: true });
                        status += $" (with {rootComponent.Name})[/]";
                    }

                    return status;
                }

                string EndOfLifeStatus()
                {
                    var endOfLife = majorVersionGroup.First().EndOfLife;
                    var daysUntilEndOfLife = (endOfLife - DateTime.Now).TotalDays;

                    var eolString = $"[{(daysUntilEndOfLife <= 0d ? "bold red" : "green")}]{endOfLife:d}[/]";

                    if (majorVersionGroup.Any(c => c.IsInstalled) && daysUntilEndOfLife is < 30d and > 0d)
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

            AnsiConsole.Write(table);
        }
        catch (ApplicationException ex)
        {
            Log.Error(ex.Message);
            Environment.Exit(-1);
        }
    }

    private async Task<Dictionary<string, DotnetVersion?>> GetComponentVersions(
        List<Component> components,
        uint timeoutInMilliseconds)
    {
        if (timeoutInMilliseconds == 0u)
        {
            return await GetComponentVersions(components).ConfigureAwait(false);
        }

        // The following madness (letting two tasks race condition against each other) is more precise than
        // using cancellationTokenSource.CancelAfter(...). For some reason (that is not worth investigating)
        // the snapd HttpRequest does not immediately abort reliably if the token is canceled.
        using var cancellationTokenSource = new CancellationTokenSource();

        var queryVersionsTask = GetComponentVersions(
            components,
            cancellationTokenSource.Token);
        var timeoutTask = Task.Delay(
            delay: TimeSpan.FromMilliseconds(timeoutInMilliseconds),
            cancellationTokenSource.Token);

        var completedTask = await Task.WhenAny(queryVersionsTask, timeoutTask).ConfigureAwait(false);
        _ = cancellationTokenSource.CancelAsync().ConfigureAwait(false);

        if (ReferenceEquals(completedTask, timeoutTask))
        {
            throw new OperationCanceledException("The component version query timed out");
        }

        return queryVersionsTask.Result;
    }

    private async Task<Dictionary<string, DotnetVersion?>> GetComponentVersions(
        List<Component> components,
        CancellationToken cancellationToken = default)
    {
        var componentVersions = new Dictionary<string, DotnetVersion?>();

        var installedComponents = new List<string>();
        var uninstalledComponents = new List<string>();

        foreach (var component in components)
        {
            if (!component.IsInstalled)
            {
                uninstalledComponents.Add(component.Key);
            }
            else if (component.IsRoot)
            {
                installedComponents.Add(component.Key);
            }
            else
            {
                //non root-components are installed as part of a root component, therefore we just ignore them here
                componentVersions.Add(component.Key, null);
            }
        }

        var getInstalledComponentsVersionsTask = GetInstalledComponentsVersions();
        var getUninstalledComponentsVersionsTask = GetUninstalledComponentsVersions();

        foreach (var (name, version) in await getInstalledComponentsVersionsTask)
        {
            componentVersions.Add(name, version);
        }

        foreach (var (name, version) in await getUninstalledComponentsVersionsTask)
        {
            componentVersions.Add(name, version);
        }

        return componentVersions;

        async Task<IEnumerable<(string Name, DotnetVersion? Version)>> GetInstalledComponentsVersions()
        {
            if (installedComponents.Count == 0) return [];

            IImmutableList<SnapInfo> installedSnaps;

            try
            {
                installedSnaps = await _snapService.GetInstalledSnaps(cancellationToken).ConfigureAwait(false);
            }
            catch (ApplicationException exception)
            {
                _logger.LogDebug(exception.Message);
                return installedComponents.Select(c => (Name: c, Version: (DotnetVersion?)null));
            }

            var result = new List<(string Name, DotnetVersion? Version)>();

            foreach (var componentName in installedComponents)
            {
                var installedSnap = installedSnaps.SingleOrDefault(snap => snap.Name == componentName);

                if (installedSnap is null)
                {
                    _logger.LogDebug($"Could not find installed component {componentName}");
                    result.Add((componentName, Version: null));
                    continue;
                }

                try
                {
                    var version = installedSnap.ParseVersionAsDotnetVersion();
                    result.Add((componentName, version));
                }
                catch (ApplicationException exception)
                {
                    _logger.LogDebug(exception.Message);
                    result.Add((componentName, Version: null));
                }
            }

            return result;
        }

        async Task<IEnumerable<(string Name, DotnetVersion? Version)>> GetUninstalledComponentsVersions()
        {
            if (uninstalledComponents.Count == 0) return [];

            var tasks = new List<(string SnapName, Task<SnapInfo?> GetSnapInfoTask)>();

            foreach (var componentName in uninstalledComponents)
            {
                tasks.Add((SnapName: componentName, GetSnapInfoTask: _snapService.Find(componentName, cancellationToken)));
            }

            var result = new List<(string Name, DotnetVersion? Version)>();

            foreach ((string snapName, Task<SnapInfo?> getSnapInfoTask) in tasks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                SnapInfo? snap;

                try
                {
                    snap = await getSnapInfoTask.ConfigureAwait(false);
                }
                catch (ApplicationException exception)
                {
                    _logger.LogDebug(exception.Message);
                    result.Add((snapName, Version: null));
                    continue;
                }

                if (snap is null)
                {
                    _logger.LogDebug($"Could not find a snap with the name {snapName}");
                    result.Add((snapName, Version: null));
                    continue;
                }

                try
                {
                    var version = snap.ParseVersionAsDotnetVersion();
                    result.Add((snapName, version));
                }
                catch (ApplicationException exception)
                {
                    _logger.LogDebug(exception.Message);
                    result.Add((snapName, Version: null));
                }
            }

            return result;
        }
    }
}
