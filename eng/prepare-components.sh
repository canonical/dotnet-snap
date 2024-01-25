#!/usr/bin/env bash

trap on_exit TERM
trap on_exit EXIT

set -euo pipefail

SCRATCH_DIR=$(mktemp -d)
FINAL_DIR=$(pwd)
RELEASE=""
RUNTIME_VERSION=""
SDK_VERSION=""
ARCH="amd64"

DOTNET_RUNTIME_TARBALL_NAME=""
ASPNETCORE_RUNTIME_TARBALL_NAME=""
DOTNET_SDK_TARBALL_NAME=""

function print_usage
{
    bold_style='\033[1m'
    underline_style='\033[4m'
    reset_style='\033[0m'

    echo    ""
    echo -e "${bold_style}Usage:${reset_style} $0 --release ${underline_style}release${reset_style} --runtime-version ${underline_style}version${reset_style} --sdk-version ${underline_style}version${reset_style}"
    echo -e "               [--arch ${underline_style}arch${reset_style}]"
    echo -e "               [--output ${underline_style}path${reset_style}]"
    echo -e "               [--dotnet-runtime-tarball-name ${underline_style}name${reset_style}]"
    echo -e "               [--aspnetcore-runtime-tarball-name ${underline_style}name${reset_style}]"
    echo -e "               [--dotnet-sdk-tarball-name ${underline_style}name${reset_style}]"
    echo    ""
    echo    "Creates the tarballs necessary to the consumption of the .NET snap installer tool."
    echo    ""
    echo    "Required parameters:"
    echo -e "  --release ${underline_style}release${reset_style}"
    echo -e "        ${underline_style}release${reset_style} is the ubuntu release to download packages from (jammy, kinetic, mantic...)."
    echo -e "  --runtime-version ${underline_style}version${reset_style}"
    echo    "        The .NET Runtime Ubuntu package version to be packaged up, e.g. 8.0.0-0ubuntu1"
    echo -e "  --sdk-version ${underline_style}version${reset_style}"
    echo    "        The .NET SDK Ubuntu package version to be packaged up, e.g. 8.0.101-0ubuntu1~22.04"
    echo    ""
    echo    "Optional parameters:"
    echo -e "  --arch ${underline_style}arch${reset_style}"
    echo    "        The architecture of the downloaded packages. (Defaults to amd64)"
    echo -e "  --output ${underline_style}path${reset_style}"
    echo    "        The output location of the tarballs. (Defaults to working directory)"
    echo -e "  --dotnet-runtime-tarball-name ${underline_style}name${reset_style}"
    echo    "        Override the name of the .NET runtime tarball. (Defaults to dotnet-runtime-<upstream_runtime_version>.tar.gz)"
    echo -e "  --aspnetcore-runtime-tarball-name ${underline_style}name${reset_style}"
    echo    "        Override the name of the ASP.NET Core runtime tarball. (Defaults to aspnetcore-runtime-<upstream_runtime_version>.tar.gz)"
    echo -e "  --dotnet-sdk-tarball-name ${underline_style}name${reset_style}"
    echo    "        Override the name of the .NET SDK tarball. (Defaults to dotnet-sdk-<upstream_sdk_version>.tar.gz)"
    echo    ""
    echo    "Example:"
    echo    "  $0 --release jammy --runtime-version 8.0.1-0ubuntu1~22.04.1 --sdk-version 8.0.101-0ubuntu1~22.04.1"
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
        --arch)
            if [ "$#" -lt "2" ]; then
                print_error "parameter --arch is specified, but no value was provided"
                print_usage
                exit 1
            fi
            ARCH=$2
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
        --dotnet-runtime-tarball-name)
            if [ "$#" -lt "2" ]; then
                print_error "parameter --dotnet-runtime-tarball-name is specified, but no value was provided"
                print_usage
                exit 1
            fi
            DOTNET_RUNTIME_TARBALL_NAME=$2
            shift 2
            ;;
        --aspnetcore-runtime-tarball-name)
            if [ "$#" -lt "2" ]; then
                print_error "parameter --aspnetcore-runtime-tarball-name is specified, but no value was provided"
                print_usage
                exit 1
            fi
            ASPNETCORE_RUNTIME_TARBALL_NAME=$2
            shift 2
            ;;
        --dotnet-sdk-tarball-name)
            if [ "$#" -lt "2" ]; then
                print_error "parameter --dotnet-sdk-tarball-name is specified, but no value was provided"
                print_usage
                exit 1
            fi
            DOTNET_SDK_TARBALL_NAME=$2
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

echo "Packaging .NET runtime..."
if [ -z "$DOTNET_RUNTIME_TARBALL_NAME" ]; then
    DOTNET_RUNTIME_TARBALL_NAME=dotnet-runtime-"$(echo "$RUNTIME_VERSION" | cut -d- -f1)"-"$ARCH".tar.gz
fi
RuntimePackages=(
    dotnet-runtime-"$DOTNET_MAJOR_VERSION".0
)
echo "Packages: "
for item in "${RuntimePackages[@]}"; do
    echo "- ${item}"
    # pull-lp-debs "$item" "$RUNTIME_VERSION" "$RELEASE"
    wget -nv https://launchpad.net/ubuntu/+archive/primary/+files/"$item"_"$RUNTIME_VERSION"_"$ARCH".deb
    dpkg --extract "$item"_* DotnetRuntime
done
tar czf "$DOTNET_RUNTIME_TARBALL_NAME" -C ./DotnetRuntime/usr/lib/dotnet .
sha256sum "$DOTNET_RUNTIME_TARBALL_NAME"
cp "$DOTNET_RUNTIME_TARBALL_NAME" "$FINAL_DIR"

echo "Packaging ASP.NET Core runtime..."
if [ -z "$ASPNETCORE_RUNTIME_TARBALL_NAME" ]; then
    ASPNETCORE_RUNTIME_TARBALL_NAME=aspnetcore-runtime-"$(echo "$RUNTIME_VERSION" | cut -d- -f1)"-"$ARCH".tar.gz
fi
AspNetCoreRuntimePackages=(
    aspnetcore-runtime-"$DOTNET_MAJOR_VERSION".0
)
echo "Packages: "
for item in "${AspNetCoreRuntimePackages[@]}"; do
    echo "- ${item}"
    # pull-lp-debs "$item" "$RUNTIME_VERSION" "$RELEASE"
    wget -nv https://launchpad.net/ubuntu/+archive/primary/+files/"$item"_"$RUNTIME_VERSION"_"$ARCH".deb
    dpkg --extract "$item"_* AspNetCoreRuntime
done
tar czf "$ASPNETCORE_RUNTIME_TARBALL_NAME" -C ./AspNetCoreRuntime/usr/lib/dotnet .
sha256sum "$ASPNETCORE_RUNTIME_TARBALL_NAME"
cp "$ASPNETCORE_RUNTIME_TARBALL_NAME" "$FINAL_DIR"

echo "Packaging SDK..."
if [ -z "$DOTNET_SDK_TARBALL_NAME" ]; then
    DOTNET_SDK_TARBALL_NAME=dotnet-sdk-"$(echo "$SDK_VERSION" | cut -d- -f1)"-"$ARCH".tar.gz
fi
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
    # pull-lp-debs "$item" "$SDK_VERSION" "$RELEASE"
    wget -nv https://launchpad.net/ubuntu/+archive/primary/+files/"$item"_"$SDK_VERSION"_"$ARCH".deb
    dpkg --extract "$item"_* DotnetSdk
done
tar czf "$DOTNET_SDK_TARBALL_NAME" -C ./DotnetSdk/usr/lib/dotnet .
sha256sum "$DOTNET_SDK_TARBALL_NAME"
cp "$DOTNET_SDK_TARBALL_NAME" "$FINAL_DIR"
