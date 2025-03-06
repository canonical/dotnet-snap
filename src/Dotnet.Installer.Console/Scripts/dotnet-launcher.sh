#!/usr/bin/env bash

set -euo pipefail

# Check for installed .NET components
manifest_path="$SNAP_COMMON/snap/manifest.json"

if [[ $("$SNAP"/usr/bin/jq 'length' "$manifest_path") -eq 0 ]]; then
    echo "Looks like you don't yet have a .NET SDK or Runtime installed."
    echo "Downloading and installing the latest LTS SDK for you to use. It should only take a few moments."

    if ! "$SNAP/Scripts/dotnet-installer-launcher.sh" install sdk lts; then
        echo "Could not install the latest LTS .NET SDK. Please check your credentials or run this command with sudo."
        exit 255
    fi
fi

# Pass-through all arguments to .NET host
dotnet_executable="$DOTNET_INSTALL_DIR/dotnet"
"$dotnet_executable" "$@"
