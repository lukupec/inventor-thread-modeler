# Migration notes: .NET Framework 4.0 → .NET 10

This document records what was changed to make the original ThreadModeler add-in
(built for Inventor 2018/2019 on .NET Framework 4.0) run in **Inventor 2026.4+ and Inventor 2027**.

## Why .NET 10

| Inventor | Add-in runtime |
|---|---|
| 2024 and older | .NET Framework 4.8 |
| 2025, 2026.0 – 2026.3 | .NET 8 |
| 2025.5, 2026.4 updates | .NET 10 |
| 2027 | .NET 10 |

.NET 8 reaches end of support in November 2026, so Autodesk moved Inventor 2027 to .NET 10 and
back-ported .NET 10 to Inventor 2025.5 and 2026.4. A single `net10.0-windows` build therefore
covers Inventor 2026 (updated) and Inventor 2027. A .NET 10 assembly cannot be loaded by the
.NET 8 runtime of Inventor 2026.0 – 2026.3.

Sources (Autodesk Developer Blog):
- [.NET Compatibility for Autodesk Inventor 2027 Add-ins](https://blog.autodesk.io/net-compatibility-for-autodesk-inventor-2027-add-ins-what-developers-need-to-know/)
- [My First Inventor Add-in — Overview](https://blog.autodesk.io/my-first-inventor-add-in-overview/) (.NET 10 in 2025.5 / 2026.4 / 2027)
- [Inventor 2027: Improved Dependency Isolation for .NET Add-ins](https://blog.autodesk.io/autodesk-inventor-2027-improved-dependency-isolation-for-net-add-ins/) (`UseInventorAssemblyContext`)
- [What's New in the Autodesk Inventor 2027 API](https://blog.autodesk.io/whats-new-in-the-autodesk-inventor-2027-api-feature-highlights-and-enhancements/)

## Changes

### Project system
- `ThreadModeler.csproj` rewritten as an SDK-style project: `net10.0-windows`, `UseWindowsForms`,
  `EnableDynamicLoading`, `PlatformTarget=x64`, `GenerateAssemblyInfo=false` (attributes stay in
  `ProductInfo.cs` / `Properties\AssemblyInfo.cs`).
- Old items removed: ClickOnce/bootstrapper packages, `System.Configuration.Install` reference,
  the VS2010/VS2012 `mt.exe` post-build step, `Inventor 2012` debug start program.
- `ThreadModeler.sln` upgraded to the Visual Studio 2026 format, x64 only; AnkhSVN binding and the
  WiX setup project removed.
- `EnableWindowsTargeting=true` allows building on non-Windows machines (CI).

### Loading / registration
- **Registry-free COM manifest removed** (`ThreadModeler.X.manifest`, embedded with `mt.exe`).
  .NET (Core) Inventor loads managed add-ins directly from the `<Assembly>` element of the `.addin`
  file and instantiates the class whose `[Guid]` matches `<ClassId>`; no COM registration or manifest is used.
- `InstallUtils` (RegAsm, `RegistrationServices`, `System.Configuration.Install`) and
  `AdnAddInRegistration` (HKCR CLSID registration) removed – unused, and not applicable to .NET 10 Inventor.
- `coolOrange.ThreadModeler.Inventor.addin` converted from UTF-16 to UTF-8;
  `SupportedSoftwareVersionLessThan 24..` replaced by `SupportedSoftwareVersionGreaterThan 29..`
  (Inventor 2026 = 30, 2027 = 31). ClassId/ClientId unchanged.
- `UseInventorAssemblyContext = 0` added: Inventor 2027 loads the add-in into its own
  `AssemblyLoadContext`, resolving dependencies (log4net) from the add-in folder first, then from
  Inventor's `Bin`. Earlier versions ignore the element.

### Packaging
- WiX v3 MSI replaced by an Autodesk **ApplicationPlugins bundle** (`cODTthreadModeler.bundle`,
  same folder name the MSI used) produced by `build.ps1` / `build.sh`.
- `PackageContents.xml`: version 30.0.0, `OS="Win64"`, `SeriesMin="R2026" SeriesMax="R2027"`,
  a single component entry for the `.addin`; non-existent PostSharp / Log4PostSharp entries removed.

### Runtime API changes
- **BinaryFormatter** is removed in .NET 9+. `.resx` files contain icons/bitmaps
  (`ResXFileRef` and `bytearray` entries), so the project sets
  `GenerateResourceUsePreserializedResources=true`. Resources are then read through
  `System.Resources.Extensions` (part of the Windows Desktop runtime) without BinaryFormatter.
  No `binary.base64` (BinaryFormatter) entries exist in the resx files.
- `Process.Start(url)` → `Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })`
  (`frmSplash.cs`, `HelpCtrlCmd.cs`). On .NET Core+ `UseShellExecute` defaults to `false`
  and starting a URL throws.
- `stdole.IPictureDisp` icons: `PictureDispConverter` (`OleCreatePictureIndirect`) is unchanged and
  works on .NET 10 with embedded `stdole` interop types.
- Inventor interop is referenced with embedded interop types (as before), so no specific
  `Autodesk.Inventor.Interop` version is required at runtime.

### Logging
- `References\log4net.dll` (1.2.x, .NET 2.0) replaced by the `log4net` 3.5.0 NuGet package.
- `XmlConfigurator.Configure` now targets the repository of the add-in assembly
  (`LogManager.GetRepository(assembly)`), as required by log4net 2+/3.
- Logger initialisation is wrapped in `try/catch` so a logging problem cannot prevent the add-in from loading.

### Versioning
- Assembly and bundle version 30.0.0 (major version = Inventor internal version of the first supported release).

## Not changed
- Thread generation logic (`Worker.cs`, `Toolkit.cs`), dialogs, ribbon definition and thread templates.
- Add-in ClassId `{90f6009a-a2ed-400f-9cbd-cee55ad30d23}` – Inventor keeps user settings for the add-in.

## Verification status
- Builds with the .NET 10 SDK (10.0.403) without warnings; bundle layout verified.
- **Runtime testing in Inventor 2026.4+/2027 must be done on Windows** (load in Add-In Manager,
  ribbon panel shows, ISO and BSW threads are modeled, log is written).
