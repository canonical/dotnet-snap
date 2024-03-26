using System.CommandLine;
using System.Text;
using Dotnet.Installer.Core.Exceptions;
using Dotnet.Installer.Core.Services.Contracts;
using Dotnet.Installer.Core.Types;
using Spectre.Console;

namespace Dotnet.Installer.Console.Commands;

public class RemoveCommand : Command
{
    private readonly IFileService _fileService;
    private readonly IManifestService _manifestService;

    public RemoveCommand(IFileService fileService, IManifestService manifestService) 
        : base("remove", "Removes an installed .NET component from the system")
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));

        var componentArgument = new Argument<string>(
            name: "component",
            description: "The .NET component name to be removed (dotnet-runtime, aspnetcore-runtime, runtime, sdk)."
        );
        var versionArgument = new Argument<string>(
            name: "version",
            description: "The .NET component version to be removed (version)."
        );
        var yesOption = new Option<bool>(
            name: "--yes",
            description: "Say yes to all prompts")
        {
            IsRequired = false
        };
        
        AddArgument(componentArgument);
        AddArgument(versionArgument);
        AddOption(yesOption);

        this.SetHandler(Handle, componentArgument, versionArgument, yesOption);
    }

    private async Task Handle(string component, string version, bool yesOption)
    {
        try
        {
            if (Directory.Exists(_manifestService.DotnetInstallLocation))
            {
                await _manifestService.Initialize();

                var requestedVersion = DotnetVersion.Parse(version);
                var requestedComponent = _manifestService.Local.FirstOrDefault(c => 
                    c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase)
                    && c.Version == requestedVersion);

                if (requestedComponent is null)
                {
                    System.Console.Error.WriteLine("ERROR: The requested component {0} {1} does not exist.", 
                        component, version);
                    Environment.Exit(-1);
                }

                var dependencyTree = new DependencyTree(_manifestService.Local);
                var reverseDependencies = 
                    dependencyTree.GetReverseDependencies(requestedComponent.Key);

                if (reverseDependencies.Count != 0 && !yesOption)
                {
                    var confirmationPrompt = new StringBuilder();
                    confirmationPrompt.AppendLine("This will also remove:");
                    foreach (var reverseDependency in reverseDependencies)
                    {
                        confirmationPrompt.AppendLine($"\t* {reverseDependency.Key}");
                    }

                    confirmationPrompt.AppendLine("Continue?");

                    if (!AnsiConsole.Confirm(confirmationPrompt.ToString(), defaultValue: false))
                    {
                        return;
                    }
                }

                await requestedComponent.Uninstall(_fileService, _manifestService);
                foreach (var reverseDependency in reverseDependencies)
                {
                    await reverseDependency.Uninstall(_fileService, _manifestService);
                }

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
