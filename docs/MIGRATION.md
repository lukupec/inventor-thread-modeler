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
- `coolOrange.ThreadModeler.Inventor.addin`:
  `SupportedSoftwareVersionLessThan 24..` replaced by `SupportedSoftwareVersionGreaterThan 29..`
  (Inventor 2026 = 30, 2027 = 31). ClassId/ClientId unchanged.
- No `UseInventorAssemblyContext` element. The add-in loads into Inventor's shared assembly load context (the
  default), so the add-in and Inventor use the same `Autodesk.Inventor.Interop`. 30.0.0 used `0`, an isolated
  context, which would load its own copy of the interop. 30.0.1 used `1`, which is the default. The manifest
  is back in the original UTF-16 format without comments.

### Packaging, installer and updates
- The add-in is an Autodesk **ApplicationPlugins bundle** (`cODTthreadModeler.bundle`, the same folder
  name the original MSI used). The build writes it complete to `bin\<Configuration>\`.
- `PackageContents.xml`: `OS="Win64"`, `SeriesMin="R2026" SeriesMax="R2027"`, a single component entry
  for the `.addin`. The non-existent PostSharp / Log4PostSharp entries are removed. It is a template;
  the version is filled in from `Directory.Build.props`.
- The WiX v3 MSI project is replaced by a **WiX 5 SDK-style MSI** (`Installer\`):
  - per-machine, one click;
  - same UpgradeCode as the original, so older installs are upgraded;
  - `DISABLEUPDATECHECK=1` for IT deployments.
  WiX 5 is used because WiX 6+ requires accepting the Open Source Maintenance Fee EULA.
- New **update check** (`ThreadModeler\Update\`):
  - reads the latest GitHub release once a day and offers the MSI;
  - verifies the MSI's SHA-256 checksum;
  - installs it after Inventor exits.
  Releases are produced by `.github/workflows/release.yml` from `vX.Y.Z` tags.

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
  works on .NET 10.
- **Interop types are no longer embedded** (since 30.0.1). `Autodesk.Inventor.Interop` and `stdole` are
  normal references and are not copied to the output; Inventor provides them. Inventor's managed add-in
  loader casts the add-in object to its own `Inventor.ApplicationAddInServer` type. With embedded interop
  types (the .NET Framework-era setting, used in 30.0.0) that cast relies on NoPIA type equivalence, which
  .NET (Core) does not guarantee. The add-in could then stay *Unloaded* without any message. This
  matches the setup of Autodesk's Inventor 2025+ add-in templates.
- The compile-time interop reference is the old v16 interop in `References\`. It is the minimum version
  required at runtime, which Inventor 2026 (v30) and 2027 (v31) both satisfy.
- Removing the `dynamic` typing of embedded interop exposed an upstream bug, which is now fixed:
  `Worker.GetThreadMajorRadiusTapered` passed `Face.Geometry` instead of the `Face` to
  `Toolkit.GetFaceSideDirection`, which would have thrown for tapered threads.
- `Activate` failures are written to `%TEMP%\ThreadModeler-load-error.txt` and shown in a message box.
  Previously Inventor swallowed them silently.

### Logging
- **log4net removed** (30.0.2). It is replaced by `Addin\AddinLog.cs`, a small built-in logger that writes to
  `%LOCALAPPDATA%\coolOrange\ThreadModeler\threadModeler.log`.
  - 30.0.0 and 30.0.1 used the `log4net` 3.5.0 package, loaded through a static field of the add-in class.
  - If Inventor or another add-in (Vault, for example) had already loaded a different log4net version into the
    shared load context, .NET threw `FileLoadException` while Inventor created the add-in, and the add-in stayed
    *Unloaded* without any message.
  - The add-in now has no third-party dependencies.
- `Addin\LoadDiagnostics.cs` (a module initializer) logs how the assembly was loaded, and any assembly that
  cannot be resolved. `Activate` runs its work in non-inlined methods, so type-load errors are caught and reported.

### Versioning
- Single source: `<Version>` in `Directory.Build.props`. The assembly version attributes are generated
  by the SDK, and the release workflow overrides the version from the git tag.
- Major version = Inventor internal version of the first supported release (30 = Inventor 2026).

## Not changed
- Thread generation logic (`Worker.cs`, `Toolkit.cs`; apart from the tapered-thread fix above), dialogs
  and thread templates. The ribbon gains a *Check for Updates* button.
- Add-in ClassId `{90f6009a-a2ed-400f-9cbd-cee55ad30d23}` – Inventor keeps user settings for the add-in.

## Verification status
- Builds with the .NET 10 SDK without warnings, and the unit tests pass. The MSI is built on `windows-latest` in CI.
- **Runtime testing in Inventor 2026.4+/2027 must be done on Windows** (load in Add-In Manager,
  ribbon panel shows, ISO and BSW threads are modeled, log is written).
