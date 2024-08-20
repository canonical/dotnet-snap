import os
import subprocess
import sys
import json


def run_elevated(command_to_execute, dotnet_install_dir) -> subprocess.CompletedProcess[str]:
    process_euid = int(subprocess.check_output(["id", "-u"]).strip())

    if process_euid == 0:
        process_result = subprocess.run(command_to_execute, check=True)
    else:
        if os.path.exists("/usr/bin/pkexec"):
            process_result = subprocess.run(["pkexec"] + command_to_execute,
                                            env={"DOTNET_INSTALL_DIR": dotnet_install_dir},
                                            check=False)
        else:
            process_result = subprocess.run(["sudo", "--preserve-env=DOTNET_INSTALL_DIR"]
                                            + command_to_execute,
                                            env={"DOTNET_INSTALL_DIR": dotnet_install_dir},
                                            check=False)

    return process_result


if __name__ == "__main__":
    try:
        # We need to install the dotnet-manifest content snap before anything
        if not os.path.exists("/snap/dotnet-manifest/current/supported.json"):
            result = run_elevated(["snap", "install", "dotnet-manifest"],
                                  os.environ.get("DOTNET_INSTALL_DIR"))

            if result.returncode != 0:
                sys.exit(result.returncode)

        if len(sys.argv) > 1 and sys.argv[1] == "installer":
            # Pass second argument onwards to .NET installer tool.
            command_to_execute = [os.environ.get("SNAP") + "/Dotnet.Installer.Console"] + sys.argv[2:]

            need_elevation_commands = ["install", "remove"]

            if len(sys.argv) > 2 and sys.argv[2] in need_elevation_commands:
                result = run_elevated(command_to_execute,
                                      os.environ.get("DOTNET_INSTALL_DIR"))
            else:
                result = subprocess.run(command_to_execute, check=False)
        else:
            # User is not invoking the .NET installer tool.

            # Check whether there are any .NET components installed.
            # If not, install the latest SDK by default.
            manifest_path = os.path.join(os.environ.get("SNAP_COMMON"), "snap", "manifest.json")
            with open(manifest_path, 'r', encoding='utf-8-sig') as f:
                manifest_data = json.load(f)

            if len(manifest_data) == 0:
                print("Welcome to .NET on Snap!")
                print("We are downloading and installing the latest SDK for you to use. It should only be a few moments.")

                command_to_execute = [os.environ.get("SNAP") + "/Dotnet.Installer.Console", "install", "sdk", "latest"]
                result = run_elevated(command_to_execute,
                                      os.environ.get("DOTNET_INSTALL_DIR"))

                if result.returncode != 0:
                    print("Could not install the latest .NET SDK.")
                    sys.exit(result.returncode)

            # Pass-through all arguments to .NET host.
            dotnet_executable = os.path.join(os.environ.get("DOTNET_INSTALL_DIR"), "dotnet")
            result = subprocess.run([dotnet_executable] + sys.argv[1:], check=False)

        sys.exit(result.returncode)
    except KeyboardInterrupt:
        # Gracefully exit
        sys.exit(-1)
