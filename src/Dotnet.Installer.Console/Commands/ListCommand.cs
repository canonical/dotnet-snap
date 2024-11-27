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
            description: "The timeout for requesting the version of a .NET component from the snap store in milliseconds (0 = infinite).),", 
            getDefaultValue: () => 800)
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

            foreach (var majorVersionGroup in _manifestService.Merged
                         .GroupBy(c => c.MajorVersion)
                         .OrderBy(c => c.Key))
            {
                var endOfLife = majorVersionGroup.First().EndOfLife;
                var isEndOfLife = endOfLife < DateTime.Now;
                var isLts = majorVersionGroup.First().IsLts;
                var dotnetVersionString = $".NET {majorVersionGroup.Key}{(isLts ? " LTS" : "")}";
                
                var dotNetRuntimeStatus = await ComponentStatus(
                    name: Constants.DotnetRuntimeComponentName,
                    majorVersion: majorVersionGroup.Key)
                    .ConfigureAwait(false);
                var aspNetCoreRuntimeStatus = await ComponentStatus(
                    name: Constants.AspnetCoreRuntimeComponentName,
                    majorVersion: majorVersionGroup.Key)
                    .ConfigureAwait(false);
                var sdkStatus = await ComponentStatus(
                    name: Constants.SdkComponentName,
                    majorVersion: majorVersionGroup.Key)
                    .ConfigureAwait(false);
                var eolString = $"[{(isEndOfLife ? "bold red" : "green")}]{endOfLife:d}[/]";

                table.AddRow(
                    dotnetVersionString,
                    dotNetRuntimeStatus,
                    aspNetCoreRuntimeStatus,
                    sdkStatus,
                    eolString);
                
                continue;
                
                async Task<string> ComponentStatus(string name, int majorVersion)
                {
                    var component = majorVersionGroup
                        .FirstOrDefault(c => c.Name == name && c.MajorVersion == majorVersion);
                    
                    bool isInstalled = component is { Installation: not null };

                    string status;
                    
                    if (component is null)
                    {
                        status = "[grey]-";
                    }
                    else if (isInstalled)
                    {
                        status = "[green][bold]Installed[/]";
                    }
                    else // is available for installation
                    {
                        status = "[blue][bold]Available[/]";
                    }
                    
                    DotnetVersion? version = await GetComponentVersion(component).ConfigureAwait(false);

                    status += version is null ? "[/]" : $" [[{version}]][/]";
                    return status;
                }

                async Task<DotnetVersion?> GetComponentVersion(Component? component)
                {
                    if (component is null)
                    {
                        return null;
                    }
                    
                    if (component.Installation is not null)
                    {
                        return component.GetLocalDotnetVersion(_manifestService, _fileService);
                    }

                    try
                    {
                        if (timeoutInMilliseconds == 0)
                        {
                            return await component.GetRemoteDotnetVersion(_snapService).ConfigureAwait(false);
                        }
                        
                        using var cancellationTokenSource = new CancellationTokenSource();
                        var queryVersionTask = component.GetRemoteDotnetVersion(
                            _snapService, 
                            cancellationTokenSource.Token);
                        var timeoutTask = Task.Delay(
                             TimeSpan.FromMilliseconds(timeoutInMilliseconds), 
                             cancellationTokenSource.Token);
                        
                        var completedTask = await Task.WhenAny(queryVersionTask, timeoutTask).ConfigureAwait(false);
                        _ = cancellationTokenSource.CancelAsync().ConfigureAwait(false);
                        
                        if (ReferenceEquals(completedTask, timeoutTask))
                        {
                            _logger.LogError($"Querying the version for {component.Key} timed out");
                            return null;
                        }

                        return queryVersionTask.Result;
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError($"Failed to query version for {component.Key}: {exception.Message}");
                        return null;
                    }
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
