﻿using System.CommandLine;
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

        this.SetHandler(Handle);
    }

    private async Task Handle()
    {
        try
        {
            await _manifestService.Initialize();

            var table = new Table();

            table.AddColumn(new TableColumn("Version"));
            table.AddColumn(new TableColumn(".NET Runtime"));
            table.AddColumn(new TableColumn("ASP.NET Core Runtime"));
            table.AddColumn(new TableColumn("SDK"));

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

                var dotNetRuntimeStatus = ComponentStatus(Constants.DotnetRuntimeComponentName, majorVersionGroup.Key);
                var aspNetCoreRuntimeStatus = ComponentStatus(Constants.AspnetCoreRuntimeComponentName, majorVersionGroup.Key);
                var sdkStatus = ComponentStatus(Constants.SdkComponentName, majorVersionGroup.Key);

                table.AddRow($".NET {majorVersionGroup.Key.ToString()}", dotNetRuntimeStatus, aspNetCoreRuntimeStatus, sdkStatus);
                continue;

                string ComponentStatus(string key, int majorVersion)
                {
                    string status;
                    var available = _manifestService.Remote.Any(c => c.Name == key && c.MajorVersion == majorVersion);

                    if (components.TryGetValue(key, out var component))
                    {
                        status = $"[bold green]Installed [[{component.Version}]][/]";
                    }
                    else
                    {
                        status = available
                            ? "[bold blue]Available[/]"
                            : "[bold grey]Unavailable[/]";
                    }

                    return status;
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
