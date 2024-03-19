using System.CommandLine;
using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Services.Contracts;
using Spectre.Console;

namespace Dotnet.Installer.Console.Verbs;

public class UpdateCommand : Command
{
    private readonly IManifestService _manifestService;
    private readonly ILimitsService _limitsService;

    public UpdateCommand(IManifestService manifestService, ILimitsService limitsService)
        : base("update", "Updates a .NET component in the system")
    {
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _limitsService = limitsService ?? throw new ArgumentNullException(nameof(limitsService));

        var componentArgument = new Argument<string>(
            name: "component",
            description: "The .NET component name to be updated (dotnet-runtime, aspnetcore-runtime, runtime, sdk).")
            {
                Arity = ArgumentArity.ZeroOrOne
            };
        var allOption = new Option<bool>(
            name: "--all",
            description: "Updates all components with updates available."
        );
        AddArgument(componentArgument);
        AddOption(allOption);

        this.SetHandler(Handle, componentArgument, allOption);
    }

    private async Task Handle(string componentArgument, bool allOption)
    {
        if ((!string.IsNullOrWhiteSpace(componentArgument) && allOption) ||
            (string.IsNullOrWhiteSpace(componentArgument) && !allOption))
        {
            System.Console.Error.WriteLine("ERROR: Either name a component or update --all");
            return;
        }

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
                            
                            toInstall.InstallingPackageChanged += (sender, package) =>
                                AnsiConsole.WriteLine($"Installing {package}");

                            context.Status(
                                $"Updating {toUninstall.Name} from {toUninstall.Version} to {toInstall.Version}...");

                            await toUninstall.Uninstall(_manifestService);
                            await toInstall.Install(_manifestService, _limitsService);

                            context.Status("[green]Update complete :check_mark_button:[/]");
                        }
                    }
                });
        }
    }
}