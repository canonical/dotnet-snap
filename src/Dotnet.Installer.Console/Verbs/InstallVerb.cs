﻿using System.CommandLine;
using Dotnet.Installer.Domain.Models;
using Dotnet.Installer.Domain.Types;

namespace Dotnet.Installer.Console.Verbs;

public class InstallVerb
{
    private readonly string? _dotnetRootPath = Environment.GetEnvironmentVariable("DOTNET_ROOT");
    private readonly RootCommand _rootCommand;

    public InstallVerb(RootCommand rootCommand)
    {
        _rootCommand = rootCommand ?? throw new ArgumentNullException(nameof(rootCommand));
    }

    public void Initialize()
    {
        var installVerb = new Command("install", "Installs a new .NET component in the system");
        var componentArgument = new Argument<string>(
            name: "component",
            description: "The .NET component name to be installed (dotnet-runtime, aspnetcore-runtime, runtime, sdk).",
            getDefaultValue: () => "sdk"
        );
        var versionArgument = new Argument<string>(
            name: "version",
            description: "The .NET component version to be installed (version or latest).",
            getDefaultValue: () => "latest"
        );
        installVerb.AddArgument(componentArgument);
        installVerb.AddArgument(versionArgument);

        installVerb.SetHandler(Handle, componentArgument, versionArgument);

        _rootCommand.Add(installVerb);
    }

    private async Task Handle(string component, string version)
    {
        if (_dotnetRootPath is null)
        {
            System.Console.Error.WriteLine("Install path is empty");
            return;
        }

        if (Directory.Exists(_dotnetRootPath))
        {
            var manifest = await Manifest.Load();

            if (manifest is null) return;
            
            var requestedVersion = DotnetVersion.Parse(version);
            var requestedComponent = manifest.FirstOrDefault(c => 
                c.Name.Equals(component, StringComparison.CurrentCultureIgnoreCase)
                && c.Version == requestedVersion);

            if (requestedComponent is null)
            {
                System.Console.Error.WriteLine("ERROR: The requested component {0} {1} does not exist.", 
                    component, version);
                return;
            }

            await requestedComponent.Install(_dotnetRootPath);

            return;
        }

        System.Console.Error.WriteLine("ERROR: The directory {0} does not exist", _dotnetRootPath);
    }
}
