using System.CommandLine;
using Dotnet.Installer.Core.Models;
using Spectre.Console;

namespace Dotnet.Installer.Console.Verbs;

public class UpdateVerb(RootCommand rootCommand)
{
    private readonly RootCommand _rootCommand = rootCommand ?? throw new ArgumentNullException(nameof(rootCommand));

    public void Initialize()
    {
        var updateVerb = new Command("update", "Updates a .NET component in the system");
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
        updateVerb.AddArgument(componentArgument);
        updateVerb.AddOption(allOption);

        updateVerb.SetHandler(Handle, componentArgument, allOption);

        _rootCommand.Add(updateVerb);
    }

    private async Task Handle(string componentArgument, bool allOption)
    {
        if ((!string.IsNullOrWhiteSpace(componentArgument) && allOption) ||
            (string.IsNullOrWhiteSpace(componentArgument) && !allOption))
        {
            System.Console.Error.WriteLine("ERROR: Either name a component or update --all");
            return;
        }

        if (Directory.Exists(Manifest.DotnetInstallLocation))
        {
            var manifest = await Manifest.Initialize();

            // Components with updates have the same major version and occur more than once
            var componentsWithUpdates = new List<Component>();
            var majorVersionGroups = manifest.Merged
                .GroupBy(c => c.Version.Major);

            foreach (var group in majorVersionGroups)
            {
                var duplicateComponents = group.Where(c1 =>
                    group.Count(c2 => c2.Name.Equals(c1.Name)) > 1);
                componentsWithUpdates.AddRange(duplicateComponents);
            }

            if (componentsWithUpdates.Count == 0)
            {
                AnsiConsole.WriteLine("There are no updates available.");
            }

            await AnsiConsole.Status()
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

                            await toUninstall.Uninstall(manifest);
                            await toInstall.Install(manifest);

                            context.Status("[green]Update complete :check_mark_button:[/]");
                        }
                    }
                });
        }
    }
}