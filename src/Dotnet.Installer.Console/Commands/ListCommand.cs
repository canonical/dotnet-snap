using System.Collections.Immutable;
using System.CommandLine;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;
using Spectre.Console;

namespace Dotnet.Installer.Console.Commands;

public class ListCommand : Command
{
    private readonly IFileService _fileService;
    private readonly IManifestService _manifestService;
    private readonly ISnapService _snapService;
    private readonly ILogger _logger;

    public ListCommand(
        IFileService fileService,
        IManifestService manifestService,
        ISnapService snapService,
        ILogger logger) : base("list", "List installed and available .NET versions")
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
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
            var componentVersions = await GetComponentVersions(components, timeoutInMilliseconds).ConfigureAwait(false);

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

                    var status = component.IsInstalled ? "[green][bold]Installed[/]" : "[blue][bold]Available[/]";

                    var version = componentVersions[component.Key];
                    status += version is null ? "[/]" : $" [[{version}]][/]";

                    return status;
                }

                string EndOfLifeStatus()
                {
                    var endOfLife = majorVersionGroup.First().EndOfLife;

                    if (endOfLife is null)
                    {
                        return "[grey]-[/]";
                    }

                    var daysUntilEndOfLife = (endOfLife.Value - DateTime.Now).TotalDays;

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
        var componentVersions = new Dictionary<string, DotnetVersion?>();
        if (components.Count == 0) return componentVersions;

        // thread safety of adding a key to a dictionary is questionable, therefore we pre-populate all keys
        foreach (var component in components)
        {
            componentVersions.Add(component.Key, null);
        }

        Task? timeoutTask = null;
        var cancellationTokenSource = new CancellationTokenSource();
        var timeout = false;
        var anyFailure = false;

        var tasks = new List<Task>
        {
            GetInstalledComponentsVersions(cancellationTokenSource.Token),
            GetUninstalledComponentsVersions(cancellationTokenSource.Token)
        };

        var timeoutTaskCount = 0;

        if (timeoutInMilliseconds > 0)
        {
            timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(timeoutInMilliseconds), cancellationTokenSource.Token);
            tasks.Add(timeoutTask);
            timeoutTaskCount = 1;
        }

        while (tasks.Count > timeoutTaskCount) // ensures that we do not wait for the timeout task
        {
            var finishedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
            tasks.Remove(finishedTask);

            if (ReferenceEquals(finishedTask, timeoutTask))
            {
                timeout = true;
                break;
            }
        }

        await cancellationTokenSource.CancelAsync().ConfigureAwait(false);

        if (timeout)
        {
            _logger.LogError("Requesting the version numbers for some components timed out. You can increase the " +
                             "timeout time with the --timeout flag.");
            _logger.LogDebug($"Unfinished tasks: {tasks.Count}");
        }
        else if (anyFailure)
        {
            _logger.LogError("The version numbers of some components can not be displayed due to unexpected " +
                             "failures. Run this command with --verbose to show more detailed error messages.");
        }

        return componentVersions;

        async Task GetInstalledComponentsVersions(CancellationToken cancellationToken = default)
        {
            var installedComponents = components.Where(c => c.IsInstalled);
            // ReSharper disable once PossibleMultipleEnumeration
            // The underlying list is not long. Therefore, the re-enumeration is cheaper than creating a new list/array.
            if (!installedComponents.Any()) return;

            IImmutableList<SnapInfo> installedSnaps;

            try
            {
                installedSnaps = await _snapService.GetInstalledSnaps(cancellationToken).ConfigureAwait(false);
            }
            catch (ApplicationException exception)
            {
                anyFailure = true;
                _logger.LogDebug(exception.Message);
                return;
            }

            // ReSharper disable once PossibleMultipleEnumeration (see reasoning above)
            foreach (var component in installedComponents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                SnapInfo installedSnap;

                try
                {
                    installedSnap = installedSnaps.Single(snap => snap.Name == component.Key);
                }
                catch (InvalidOperationException)
                {
                    anyFailure = true;
                    _logger.LogDebug($"Could not find installed component {component.Key}");
                    continue;
                }

                try
                {
                    componentVersions[component.Key] = installedSnap.ParseVersionAsDotnetVersion();
                }
                catch (ApplicationException exception)
                {
                    anyFailure = true;
                    _logger.LogDebug(exception.Message);
                }
            }
        }

        Task GetUninstalledComponentsVersions(CancellationToken cancellationToken = default)
        {
            return Parallel.ForEachAsync(
                components.Where(c => !c.IsInstalled),
                cancellationToken,
                GetUninstalledComponentVersion);
        }

        async ValueTask GetUninstalledComponentVersion(Component component, CancellationToken cancellationToken = default)
        {
            SnapInfo? snap;

            try
            {
                snap = await _snapService.FindSnap(component.Key, cancellationToken).ConfigureAwait(false);
            }
            catch (ApplicationException exception)
            {
                anyFailure = true;
                _logger.LogDebug(exception.Message);
                return;
            }

            if (snap is null)
            {
                anyFailure = true;
                _logger.LogDebug($"Could not find a snap with the name {component.Key}");
                return;
            }

            try
            {
                var version = snap.ParseVersionAsDotnetVersion();
                componentVersions[component.Key] = version;
            }
            catch (ApplicationException exception)
            {
                anyFailure = true;
                _logger.LogDebug(exception.Message);
            }
        }
    }
}
