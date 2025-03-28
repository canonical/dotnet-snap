#!/bin/bash

# Check if the snap name argument is provided
if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <snap>"
    exit 1
fi

snap="$1"

echo "Updating mount unit files for $snap..."

# Function to run a command and check its result
run_command() {
    local cmd="$1"
    local description="$2"

    echo "Running: $cmd"
    eval "$cmd" > >(tee /tmp/script_output.log) 2>&1
    local return_code=${PIPESTATUS[0]}

    if [ "$return_code" -eq 0 ]; then
        echo "$description successful."
    else
        echo "$description failed."
        echo "Error message:"
        cat /tmp/script_output.log
        exit "$return_code"
    fi
}

# Remove dotnet-installer-update-mounts file
needs_update_file="/var/snap/$snap/common/dotnet-installer-update-mounts"
if [ -f "$needs_update_file" ]; then
    # Remove old mount unit files
    run_command "/snap/bin/dotnet.dotnet-installer environment remove-units --snap $snap --verbose" "Removing old mount unit files"

    # Place new mount unit files
    run_command "/snap/bin/dotnet.dotnet-installer environment place-units --snap $snap --verbose" "Placing new mount unit files"

    rm "$needs_update_file"
    echo "Removed dotnet-installer-update-mounts file for $snap"
else
    echo "No dotnet-installer-update-mounts file found for $snap"
fi
