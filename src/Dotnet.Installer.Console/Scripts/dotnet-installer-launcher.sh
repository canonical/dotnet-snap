#!/usr/bin/env bash

set -euo pipefail

# Run a command in elevated mode (sudo or pkexec, whichever is available)
run_elevated() {
    shift  # Remove the first argument to keep only the command

    local command_to_execute=("$@")

    # Check if the script is running as root
    if [[ "$EUID" -eq 0 ]]; then
        "${command_to_execute[@]}"
    else
        sudo --preserve-env=DOTNET_INSTALL_DIR,DOTNET_INSTALLER_DEBUG "${command_to_execute[@]}"
    fi

    return $?
}

debug="${DOTNET_INSTALLER_DEBUG:-0}"

# Pass first argument onwards to .NET installer tool
command_to_execute=("$SNAP/Dotnet.Installer.Console" "${@:1}")

need_elevation_commands=("install" "remove")

# shellcheck disable=SC2076
# shellcheck disable=SC2199
if [[ $# -gt 1 && " ${need_elevation_commands[@]} " =~ " ${1} " ]]; then
    run_elevated "$debug" "${command_to_execute[@]}"
else
    "${command_to_execute[@]}"
fi
