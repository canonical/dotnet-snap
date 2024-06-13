#!/bin/bash
set -euo pipefail

run_elevated() {
    # Pass second argument onwards to .NET installer tool.
    CommandToExecute=$1
    DotnetInstallDir=$DOTNET_INSTALL_DIR
    ServerUrl=$SERVER_URL
    
    # If the third argument is either install or remove, we need to elevate the process.
    ProcessEuid=$("$SNAP"/Dotnet.Installer.Console environment | grep "Process+EffectiveUserId" | cut -d= -f2)
    
    if [ "$ProcessEuid" -eq "0" ]; then
        $CommandToExecute
    else
        if [ -e "/usr/bin/pkexec" ]; then
            # shellcheck disable=SC2086
            pkexec DOTNET_INSTALL_DIR=$DotnetInstallDir SERVER_URL=$ServerUrl $CommandToExecute
        else
            # shellcheck disable=SC2086
            sudo DOTNET_INSTALL_DIR=$DotnetInstallDir SERVER_URL=$ServerUrl $CommandToExecute
        fi
    fi
}

if [ -n "${1:-}" ] && [ "$1" = "installer" ]; then
    # Pass second argument onwards to .NET installer tool.
    CommandToExecute="$SNAP/Dotnet.Installer.Console ${*: 2}"
    
    need_elevation_commands=("install" "remove")
        
    # shellcheck disable=SC2076
    if [[ "${need_elevation_commands[*]}" =~ "$2" ]]; then
        run_elevated "$CommandToExecute"
    else
        $CommandToExecute
    fi
else
    # User is not invoking the .NET installer tool.
    
    # Check whether there is any .NET components installed.
    # If not, we install the latest SDK by default.
    if [ "$(jq length "${SNAP_COMMON}/snap/manifest.json")" -eq "0" ]; then
        CommandToExecute="$SNAP/Dotnet.Installer.Console install sdk latest"
        run_elevated "$CommandToExecute"
    fi 
    
    # Pass-through all arguments to .NET host.
    "$DOTNET_INSTALL_DIR"/dotnet "$@"
fi