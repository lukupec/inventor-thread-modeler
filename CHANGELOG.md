# Changelog

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
