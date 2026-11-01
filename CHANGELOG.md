# Changelog

## 30.0.1 – 2026-11-01

- **Installer**: per-machine MSI (`ThreadModeler-<version>.msi`) from GitHub Releases. Double-click to install;
  it replaces older ThreadModeler installs. Silent install supported (`DISABLEUPDATECHECK=1` for IT-managed updates).
- **Automatic update check**: once a day Inventor checks GitHub for a newer release and offers to download it.
  The MSI is verified with SHA-256 and installed after Inventor is closed. *Check for Updates* button in the
  panel's slide-out.
- **Fix: add-in could stay "Automatic / Unloaded"**: the Inventor interop is no longer embedded, and the
  add-in loads into Inventor's shared assembly context.
- Load errors are shown and written to `%TEMP%\ThreadModeler-load-error.txt`, instead of failing silently.
- Fix: tapered-thread radius computation passed the face geometry instead of the face (upstream bug).
- Single version source (`Directory.Build.props`); release workflow publishes on `vX.Y.Z` tags.
- Unit tests for the update logic.

## 30.0.0 – 2026-11-01

Port to .NET 10 for Autodesk Inventor 2026 (Update 2026.4+) and Inventor 2027.

- Target `net10.0-windows` (SDK-style project, Visual Studio 2026 / .NET 10 SDK).
- Distributed as an ApplicationPlugins bundle (`cODTthreadModeler.bundle`) instead of a WiX MSI.
- `.addin` manifest: supports Inventor 2026+ and loads in an isolated assembly context on Inventor 2027.
- log4net 3.5.0.
- Removed registry-free COM manifest and RegAsm/registry install helpers.
- Fixed opening the Help / coolOrange links on .NET 10.
- Resources load without BinaryFormatter.
- Build scripts (`build.ps1`, `build.sh`) and GitHub Actions build.

See [docs/MIGRATION.md](docs/MIGRATION.md) for details.

## 19.0.x

Last upstream version ([coolOrangeApps/inventor-thread-modeler](https://github.com/coolOrangeApps/inventor-thread-modeler)),
.NET Framework 4.0, Inventor 2018–2019.
