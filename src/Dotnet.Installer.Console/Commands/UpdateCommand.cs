using System.CommandLine;
using Dotnet.Installer.Core.Exceptions;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Services.Contracts;
using Spectre.Console;

namespace Dotnet.Installer.Console.Commands;

public class UpdateCommand : Command
{
    private readonly IFileService _fileService;
    private readonly ILimitsService _limitsService;
    private readonly IManifestService _manifestService;

    public UpdateCommand(IFileService fileService, ILimitsService limitsService, IManifestService manifestService)
        : base("update", "Updates a .NET component in the system")
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _limitsService = limitsService ?? throw new ArgumentNullException(nameof(limitsService));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));

        this.SetHandler(Handle);
    }

    private async Task Handle()
    {
        try
        {
            if (Directory.Exists(_manifestService.DotnetInstallLocation))
            {
                await _manifestService.Initialize();

                // Components with updates have the same major version and occur more than once
                var componentsWithUpdates = new List<Component>();
                var majorVersionGroups = _manifestService.Merged
                    .GroupBy(c => c.Version.Major);

                foreach (var group in majorVersionGroups)
                {
                    // Components that appear duplicated in the merged component list
                    // indicate an available update, as this scenario is only possible
                    // when the local manifest contains a component that is also listed
                    // in the remote 'latest' manifest. Since their versions are not the
                    // same, they appear duplicated (each with their own versions).
                    var duplicateComponents = group
                        .Where(c1 => group
                            .Count(c2 => c2.Name.Equals(c1.Name)) > 1);
                    componentsWithUpdates.AddRange(duplicateComponents);
                }

                if (componentsWithUpdates.Count == 0)
                {
                    AnsiConsole.WriteLine("There are no updates available.");
                    return;
                }

                await AnsiConsole
                    .Status()
                    .Spinner(Spinner.Known.Dots12)
                    .StartAsync("Thinking...", async context =>
                    {
                        foreach (var versionGroup in componentsWithUpdates.GroupBy(c => c.Version.Major))
                        {
                            foreach (var componentGroup in versionGroup.GroupBy(c => c.Name))
                            {
                                var toUninstall = componentGroup
                                    .OrderBy(c => c.Version)
                                    .FirstOrDefault(c => c.Installation is not null);
                                var toInstall = componentGroup
                                    .OrderBy(c => c.Version)
                                    .LastOrDefault(c => c.Installation is null);

                                if (toUninstall is null || toInstall is null)
                                {
                                    System.Console.Error.WriteLine("ERROR: Could not update {0}", componentGroup.Key);
                                    continue;
                                }
                                
                                toInstall.InstallingPackageChanged += (sender, args) =>
                                    AnsiConsole.WriteLine($"Installing {args.Package.Name} {args.Component.Version}");

                                context.Status(
                                    $"Updating {toUninstall.Name} from {toUninstall.Version} to {toInstall.Version}...");

                                if (toInstall.CanInstall(_limitsService))
                                {
                                    await toUninstall.Uninstall(_fileService, _manifestService);
                                    await toInstall.Install(_fileService, _limitsService, _manifestService);
                                }
                                else
                                {
                                    throw new VersionTooHighException(toInstall,
                                        toInstall.Version.IsRuntime
                                            ? _limitsService.Runtime
                                            : _limitsService.Sdk.First(v => v.FeatureBand == toInstall.Version.FeatureBand));
                                }
                            }
                        }

                        AnsiConsole.Write(new Markup("[green]Update complete :check_mark_button:[/]\n"));
                    });

                return;
            }

            System.Console.Error.WriteLine("ERROR: The directory {0} does not exist", 
                _manifestService.DotnetInstallLocation);
            Environment.Exit(-1);
        }
        catch (ExceptionBase ex)
        {
            System.Console.Error.WriteLine("ERROR: " + ex.Message);
            Environment.Exit((int)ex.ErrorCode);
        }
    }
}
