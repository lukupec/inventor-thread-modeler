# inventor-thread-modeler

[![Windows](https://img.shields.io/badge/Platform-Windows%20x64-lightgray.svg)](https://www.microsoft.com/en-us/windows/)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![Inventor](https://img.shields.io/badge/Autodesk%20Inventor-2026.4%2B%20%7C%202027-yellow.svg)](https://www.autodesk.com/products/inventor/)

Port of [coolOrangeApps/inventor-thread-modeler](https://github.com/coolOrangeApps/inventor-thread-modeler)
to **.NET 10** for **Autodesk Inventor 2026 (Update 2026.4 or later)** and **Autodesk Inventor 2027**.

## Disclaimer

THE SAMPLE CODE ON THIS REPOSITORY IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, OR NON-INFRINGEMENT.

THE USAGE OF THIS SAMPLE IS AT YOUR OWN RISK AND **THERE IS NO SUPPORT** RELATED TO IT.

## Description

The threadModeler tool has been developed to allow Autodesk® Inventor® users to generate a realistic modeled equivalent thread based on an existing Inventor thread feature. Users are able to select an existing thread feature in the model, which the threadModeler then converts to a realistic 3D modeled thread based on the underlying thread feature properties.

Modeled threads are accurately generated saving a lot of time and effort for the user when compared with a manual approach, bringing a higher degree of realism to your models.

Please watch this short [video](https://coolorange.wistia.com/medias/9hf3s50g6m) for an overview of the threadModeler.

## Supported versions

| Inventor | .NET runtime of Inventor | Supported |
|---|---|---|
| 2027 | .NET 10 | ✅ |
| 2026 Update 2026.4 or later | .NET 10 | ✅ |
| 2026.0 – 2026.3 | .NET 8 | ❌ (update Inventor to 2026.4+) |
| 2025 and older | .NET 8 / .NET Framework | ❌ (use the upstream project) |

## Quick start

1. Build the bundle (Windows, .NET 10 SDK):
   ```powershell
   .\build.ps1
   ```
2. Copy `dist\cODTthreadModeler.bundle` to `%ProgramData%\Autodesk\ApplicationPlugins\`
   (or run `.\build.ps1 -Install` from an elevated prompt).
3. Start Inventor, open a part. The command is on the **coolOrange** ribbon tab → **threadModeler** panel.

## Documentation

- [docs/BUILDING.md](docs/BUILDING.md) – requirements, building in Visual Studio / CLI, debugging
- [docs/INSTALL.md](docs/INSTALL.md) – installing, uninstalling, usage, logs, troubleshooting
- [docs/MIGRATION.md](docs/MIGRATION.md) – what changed compared to the original .NET Framework 4.0 add-in and why
- [CHANGELOG.md](CHANGELOG.md)

## Repository layout

```
ThreadModeler.sln
ProductInfo.cs                      shared assembly version / company info
build.ps1, build.sh                 build + package the ApplicationPlugins bundle into dist/
References/                         fallback Inventor interop + stdole (compile-time only)
ThreadModeler/
  ThreadModeler.csproj              SDK-style, net10.0-windows
  coolOrange.ThreadModeler.Inventor.addin   Inventor add-in manifest
  PackageContents.xml               ApplicationPlugins bundle manifest
  Addin/StandardAddInServer.cs      add-in entry point (ApplicationAddInServer)
  Commands/                         ribbon commands and the ThreadModeler dialog
  Worker.cs, Toolkit.cs             thread generation logic
  Utilities/                        ADN helper classes (ribbon, commands, interaction)
  Thread Templates/                 ISO / BSW profile templates (.ipt)
  resources/                        icons, ribbon definition
docs/                               documentation
```

## License

MIT – see [LICENSE](LICENSE). Original work © coolOrange s.r.l / Autodesk ADN.
