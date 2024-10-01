#!/usr/bin/env bash

set -euo pipefail

# Run a command in elevated mode (sudo or pkexec, whichever is available)
run_elevated() {
    # shellcheck disable=SC2153
    local dotnet_install_dir="$DOTNET_INSTALL_DIR"
    local should_debug="$1"  # Get the debug value as the first argument
    shift  # Remove the first argument to keep only the command

    local command_to_execute=("$@")

    # Check if the script is running as root
    if [[ "$EUID" -eq 0 ]]; then
        "${command_to_execute[@]}"
    else
        if [[ -e /usr/bin/pkexec ]]; then
            pkexec "${command_to_execute[@]}" env DOTNET_INSTALL_DIR="$dotnet_install_dir" DOTNET_INSTALLER_DEBUG="$should_debug"
        else
            sudo --preserve-env=DOTNET_INSTALL_DIR,DOTNET_INSTALLER_DEBUG "${command_to_execute[@]}"
        fi
    fi
}

# Main script execution
if [[ ! -e "/snap/dotnet-manifest/current/supported.json" ]]; then
    if [[ $(run_elevated "0" snap install dotnet-manifest) -ne 0 ]]; then
        exit 255
    fi
fi

if [[ $# -gt 0 && $1 = "installer" ]]; then
    debug="${DOTNET_INSTALLER_DEBUG:-0}"

    # Pass second argument onwards to .NET installer tool
    command_to_execute=("$SNAP/Dotnet.Installer.Console" "${@:2}")

    need_elevation_commands=("install" "remove")

    # shellcheck disable=SC2076
    # shellcheck disable=SC2199
    if [[ $# -gt 2 && " ${need_elevation_commands[@]} " =~ " ${2} " ]]; then
        run_elevated "$debug" "${command_to_execute[@]}"
    else
        "${command_to_execute[@]}"
    fi
else
    # Check for installed .NET components
    manifest_path="$SNAP_COMMON/snap/manifest.json"
    manifest_data=$(< "$manifest_path")

    if [[ $(echo "$manifest_data" | "$SNAP"/usr/bin/jq 'length') -eq 0 ]]; then
        echo "Welcome to .NET on Snap!"
        echo "Looks like you don't yet have a .NET SDK or Runtime installed."
        echo "We are downloading and installing the latest SDK for you to use. It should only be a few moments."

        command_to_execute=("$SNAP/Dotnet.Installer.Console" "install" "sdk" "latest")

        if [[ $(run_elevated "0" "${command_to_execute[@]}") -ne 0 ]]; then
            echo "Could not install the latest .NET SDK."
            exit 255
        fi
    fi

    # Pass-through all arguments to .NET host
    dotnet_executable="$DOTNET_INSTALL_DIR/dotnet"
    "$dotnet_executable" "$@"
fi
