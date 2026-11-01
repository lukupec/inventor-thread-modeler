# ThreadModeler

[![Windows](https://img.shields.io/badge/Platform-Windows%20x64-lightgray.svg)](https://www.microsoft.com/en-us/windows/)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![Inventor](https://img.shields.io/badge/Autodesk%20Inventor-2026.4%2B%20%7C%202027-yellow.svg)](https://www.autodesk.com/products/inventor/)

An add-in for **Autodesk Inventor 2026 (Update 2026.4 or later)** and **Autodesk Inventor 2027** (.NET 10) that turns Inventor thread features into realistic modeled threads.

## Disclaimer

THE SAMPLE CODE ON THIS REPOSITORY IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, OR NON-INFRINGEMENT.

THE USAGE OF THIS SAMPLE IS AT YOUR OWN RISK AND **THERE IS NO SUPPORT** RELATED TO IT.

## Description

ThreadModeler has been developed to allow Autodesk® Inventor® users to generate a realistic modeled equivalent thread based on an existing Inventor thread feature. Users are able to select an existing thread feature in the model, which the threadModeler then converts to a realistic 3D modeled thread based on the underlying thread feature properties.

Modeled threads are accurately generated saving a lot of time and effort for the user when compared with a manual approach, bringing a higher degree of realism to your models.

Please watch this short [video](https://coolorange.wistia.com/medias/9hf3s50g6m) of the original tool for an overview.

## Supported versions

| Inventor | .NET runtime of Inventor | Supported |
|---|---|---|
| 2027 | .NET 10 | ✅ |
| 2026 Update 2026.4 or later | .NET 10 | ✅ |
| 2026.0 – 2026.3 | .NET 8 | ❌ (update Inventor to 2026.4+) |
| 2025 and older | .NET 8 / .NET Framework | ❌ (use the upstream project) |

## Install

1. Download **`ThreadModeler-<version>.msi`** from the
   [latest release](https://github.com/lukupec/inventor-thread-modeler/releases/latest).
2. Double-click it and confirm the administrator prompt.
3. Start Inventor and open a part. The command is on the **ThreadModeler** ribbon tab.

New versions are offered automatically from inside Inventor (at most once a day), and installed after you close Inventor.
See [docs/INSTALL.md](docs/INSTALL.md) for silent/IT installs, turning the update check off, and troubleshooting.

## Build

```powershell
.\build.ps1        # Windows: add-in + tests + MSI in dist\
```
```bash
./build.sh         # any OS: add-in + tests + bundle (no MSI)
```

Releases are made by pushing a tag `vX.Y.Z`. See [docs/BUILDING.md](docs/BUILDING.md).

## Documentation

- [docs/BUILDING.md](docs/BUILDING.md) – requirements, building, debugging, installer, releasing
- [docs/INSTALL.md](docs/INSTALL.md) – installing (MSI, silent, manual), updates, usage, logs, troubleshooting
- [docs/MIGRATION.md](docs/MIGRATION.md) – what changed compared to the original .NET Framework 4.0 add-in and why
- [CHANGELOG.md](CHANGELOG.md)

## Repository layout

```
ThreadModeler.sln
Directory.Build.props               product version (single source)
ProductInfo.cs                      shared company / copyright info
build.ps1, build.sh                 build + test + package into dist/
References/                         Inventor interop + stdole used as compile-time references
ThreadModeler/
  ThreadModeler.csproj              SDK-style, net10.0-windows
  ThreadModeler.Inventor.addin      Inventor add-in manifest
  PackageContents.xml               ApplicationPlugins bundle manifest (template, @VERSION@)
  Addin/StandardAddInServer.cs      add-in entry point (ApplicationAddInServer)
  Commands/                         ribbon commands and the ThreadModeler dialog
  Update/                           update check (GitHub Releases), update dialog
  Worker.cs, Toolkit.cs             thread generation logic
  Utilities/                        ADN helper classes (ribbon, commands, interaction)
  Thread Templates/                 ISO / BSW profile templates (.ipt)
  resources/                        icons, ribbon definition
ThreadModeler.Tests/                unit tests (update logic)
Installer/                          WiX 5 MSI (per-machine)
.github/workflows/                  build.yml (CI), release.yml (tag -> GitHub release)
docs/                               documentation
```

## License

MIT – see [LICENSE](LICENSE) and [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).

## Credits

ThreadModeler is derived from **threadModeler** by coolOrange s.r.l. –
[coolOrangeApps/inventor-thread-modeler](https://github.com/coolOrangeApps/inventor-thread-modeler) (MIT License,
Copyright (c) 2020 coolOrange - LABS), which is in turn based on Autodesk ADN sample code by Philippe Leefsma and Jan Liska.
This port is independent and not affiliated with or endorsed by coolOrange s.r.l. or Autodesk, Inc.
Autodesk and Inventor are registered trademarks of Autodesk, Inc.
