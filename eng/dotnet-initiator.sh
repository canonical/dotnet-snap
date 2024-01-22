#!/bin/bash

if [ "$1" = "installer" ]; then
    # Pass second argument onwards to .NET installer tool.
    "$SNAP"/Dotnet.Installer.Console "${@: 2}"
else
    # User is not invoking the .NET installer tool.
    # Pass-through all arguments to .NET host.
    "$DOTNET_ROOT"/dotnet "$@"
fi