#!/usr/bin/env bash

trap on_exit TERM
trap on_exit EXIT

set -euo pipefail

SCRATCH_DIR=$(mktemp -d)
FINAL_DIR=$(pwd)
RELEASE=""
RUNTIME_VERSION=""
SDK_VERSION=""

function print_usage
{
    bold_style='\033[1m'
    underline_style='\033[4m'
    reset_style='\033[0m'

    echo    ""
    echo -e "${bold_style}Usage:${reset_style} $0 --release ${underline_style}release${reset_style} --runtime-version ${underline_style}version${reset_style} --sdk-version ${underline_style}version${reset_style} [--output ${underline_style}path${reset_style}]"
    echo    ""
    echo    "Creates the tarballs necessary to the consumption of the .NET snap installer tool."
    echo    ""
    echo    "Parameters:"
    echo -e "  --release ${underline_style}release${reset_style}"
    echo -e "        ${underline_style}release${reset_style} is the ubuntu release to download packages from (jammy, kinetic, mantic...)."
    echo -e "  --runtime-version ${underline_style}version${reset_style}"
    echo    "        The .NET Runtime version to be packaged up."
    echo -e "  --sdk-version ${underline_style}version${reset_style}"
    echo    "        The .NET SDK version to be packaged up."
    echo -e "  --output ${underline_style}path${reset_style}"
    echo    "        The output location of the tarballs. (Defaults to working directory)"
    echo    ""
    echo    "Example:"
    echo    "  $0 --release jammy --runtime-version 8.0.1 --sdk-version 8.0.101"
}

function on_exit {
    if [ -d "$SCRATCH_DIR" ]; then
        rm -rf "$SCRATCH_DIR"
    fi
}

function print_error() {
    echo "ERROR:" "$@" 1>&2;
}

# parse parameters
while [ "$#" -gt 0 ]; do
    case $1 in
        --help)
            print_usage
            exit 0
            ;;
        --release)
            if [ "$#" -lt "2" ]; then
                print_error "parameter --release is specified, but no value was provided"
                print_usage
                exit 1
            fi

            RELEASE=$2
            shift 2
            ;;
        --runtime-version)
            if [ "$#" -lt "2" ]; then
                print_error "parameter --runtime-version is specified, but no value was provided"
                print_usage
                exit 1
            fi

            RUNTIME_VERSION=$2
            shift 2
            ;;
        --sdk-version)
            if [ "$#" -lt "2" ]; then
                print_error "parameter --sdk-version is specified, but no value was provided"
                print_usage
                exit 1
            fi

            SDK_VERSION=$2
            shift 2
            ;;
        --output)
            if [ "$#" -lt "2" ]; then
                print_error "parameter --output is specified, but no value was provided"
                print_usage
                exit 1
            fi

            FINAL_DIR=$2
            shift 2
            ;;
        *)
            print_error "unexpected argument '$1'"
            print_usage
            exit 1
            ;;
    esac
done

if [ -z "$RELEASE" ]; then
    print_error "Release was not specified."
    print_usage
    exit 1
fi
if [ -z "$RUNTIME_VERSION" ]; then
    print_error ".NET Runtime version was not specified."
    print_usage
    exit 1
fi
if [ -z "$SDK_VERSION" ]; then
    print_error ".NET SDK version was not specified."
    print_usage
    exit 1
fi

DOTNET_MAJOR_VERSION=$(echo "$RUNTIME_VERSION" | cut -d . -f 1)

pushd "$SCRATCH_DIR"

echo "Pulling components for .NET Runtime ${RUNTIME_VERSION} and .NET SDK ${SDK_VERSION} on ${RELEASE}."
echo "Scratch dir: ${SCRATCH_DIR}"

echo "Packaging runtime..."
RuntimePackages=(
    dotnet-runtime-"$DOTNET_MAJOR_VERSION".0
)
echo "Packages: "
for item in "${RuntimePackages[@]}"; do
    echo "- ${item}"

    pull-lp-debs "$item" "$RUNTIME_VERSION" "$RELEASE"
    dpkg --extract "$item"* DotnetRuntime
done
tar czf dotnet-runtime-"$RUNTIME_VERSION".tar.gz -C ./DotnetRuntime/usr/lib/dotnet .
sha256sum dotnet-runtime-"$RUNTIME_VERSION".tar.gz
cp dotnet-runtime-"$RUNTIME_VERSION".tar.gz "$FINAL_DIR"

echo "Packaging SDK..."
SdkPackages=(
    dotnet-sdk-"$DOTNET_MAJOR_VERSION".0
    aspnetcore-targeting-pack-"$DOTNET_MAJOR_VERSION".0
    dotnet-apphost-pack-"$DOTNET_MAJOR_VERSION".0
    dotnet-targeting-pack-"$DOTNET_MAJOR_VERSION".0
    dotnet-templates-"$DOTNET_MAJOR_VERSION".0
)
echo "Packages: "
for item in "${SdkPackages[@]}"; do
    echo "- ${item}"

    pull-lp-debs "$item" "$SDK_VERSION" "$RELEASE"
    dpkg --extract "$item"* DotnetSdk
done
tar czf dotnet-sdk-"$SDK_VERSION".tar.gz -C ./DotnetSdk/usr/lib/dotnet .
sha256sum dotnet-sdk-"$SDK_VERSION".tar.gz
cp dotnet-sdk-"$SDK_VERSION".tar.gz "$FINAL_DIR"
