<br/>
<p align="center">
  <a href="https://dot.net">
    <img src="images/dotnet-logo.png" alt="Logo" width="80" height="80">
  </a>

  <h3 align="center">.NET Snap</h3>

  <p align="center">
    A snap package for .NET
    <br/>
    <br/>
    <a href="https://github.com/canonical/dotnet-snap/actions/workflows/build-on-main.yaml"><img src="https://github.com/canonical/dotnet-snap/actions/workflows/build-on-main.yaml/badge.svg?event=push"/></a>
    <br/>
    <br/>
    <a href="https://github.com/canonical/dotnet-snap/wiki"><strong>Explore the docs »</strong></a>
    <br/>
    <br/>
    <a href="https://github.com/canonical/dotnet-snap/issues">Report Bug</a>
    .
    <a href="https://github.com/canonical/dotnet-snap/issues">Request Feature</a>
  </p>
</p>

![Downloads](https://img.shields.io/github/downloads/canonical/dotnet-snap/total) ![Contributors](https://img.shields.io/github/contributors/canonical/dotnet-snap?color=dark-green) ![Issues](https://img.shields.io/github/issues/canonical/dotnet-snap) ![License](https://img.shields.io/github/license/canonical/dotnet-snap)

## Table Of Contents

* [About the Project](#about-the-project)
* [Built With](#built-with)
* [Getting Started](#getting-started)
  * [Prerequisites](#prerequisites)
  * [Installation](#installation)
* [Usage](#usage)
* [Roadmap](#roadmap)
* [Contributing](#contributing)
* [License](#license)
* [Authors](#authors)
* [Acknowledgements](#acknowledgements)

## About The Project

![Screen Shot](images/demo.gif)

This is the source code for the .NET snap package. It includes shared .NET components, such as the latest .NET host, as well as an installer tool that will allow you to install several versions of .NET side-by-side, all within your snap environment.

## Built With

This project is built with:

- Snapcraft
- C# (.NET 8)
- Some (little) bash scripting :)

## Getting Started

### Prerequisites

None really. Just make sure you're running a Linux distro that supports snaps.

### Installation

1. Install the .NET snap from the Snapcraft store
```
$ sudo snap install --classic dotnet
```

## Usage

When you first install the .NET snap, you will already have a fully working SDK installation of the latest .NET LTS (currently, .NET 8). In order to manage the versions you have installed, you have to use the .NET installer tool, which is embedded within the snap package.

### List

To see which versions you currently have installed in your system, simply run `dotnet installer list`.

## Roadmap

See the [open issues](https://github.com/canonical/dotnet-snap/issues) for a list of proposed features (and known issues).

## Contributing

Contributions are what make the open source community such an amazing place to be learn, inspire, and create. Any contributions you make are **greatly appreciated**.
* If you have suggestions for adding or removing features, feel free to [open an issue](https://github.com/canonical/dotnet-snap/issues/new) to discuss it before creating a pull request with your changes.
* Please make sure you check your spelling and grammar.
* Create individual PR for each suggestion.
* Please also read through the [Code Of Conduct](https://github.com/canonical/dotnet-snap/blob/main/CODE_OF_CONDUCT.md) before posting your first idea as well.

### Creating A Pull Request

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/my-feature`)
3. Commit your Changes (`git commit -m 'here is my amazing feature'`)
4. Push to the Branch (`git push origin feature/my-feature`)
5. Open a Pull Request

## License

See [LICENSE](https://github.com/canonical/dotnet-snap/blob/main/LICENSE) for more information.

## Authors

* **Mateus Rodrigues de Morais** - *Canonical* - [GitHub](https://github.com/mateusrodrigues/)

## Acknowledgements

