#!/bin/bash

if [ "$1" = "installer" ]; then
    # Pass second argument onwards to .NET installer tool.
    "$SNAP"/Dotnet.Installer.Console "${@: 2}"
else
    # User is not invoking the .NET installer tool.
    
    # Check whether there is any .NET components installed.
    # If not, we install the latest SDK by default.
    if [ "$(jq length "${SNAP_COMMON}/snap/manifest.json")" -eq "0" ]; then
      "$SNAP"/Dotnet.Installer.Console install sdk latest
    fi 
    
    # Pass-through all arguments to .NET host.
    "$DOTNET_INSTALL_DIR"/dotnet "$@"
fi
