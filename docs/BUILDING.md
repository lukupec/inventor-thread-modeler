# Building ThreadModeler

## Requirements

| | |
|---|---|
| OS | Windows 10/11 x64 (building also works on Linux/macOS, see below) |
| SDK | [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) |
| IDE (optional) | Visual Studio 2026 with the **.NET desktop development** workload |
| Inventor (optional for building, required for running) | Inventor 2027 or Inventor 2026 Update 2026.4+ |

Visual Studio 2022 cannot target .NET 10; use Visual Studio 2026 or the `dotnet` CLI.

## Inventor interop reference

The project references `Autodesk.Inventor.Interop.dll` with **Embed Interop Types = true**,
so the add-in does not depend on a specific interop version at runtime. The reference is resolved in this order:

1. `-p:InventorInteropPath=...` if passed on the command line
2. `C:\Program Files\Autodesk\Inventor 2027\Bin\Public Assemblies\Autodesk.Inventor.Interop.dll`
3. `C:\Program Files\Autodesk\Inventor 2026\Bin\Public Assemblies\Autodesk.Inventor.Interop.dll`
4. `References\Autodesk.Inventor.Interop.dll` (old interop kept in the repo; sufficient because the add-in uses no newer API)

`stdole` is referenced from `References\stdole.dll`, also with embedded interop types.

## Build the bundle

PowerShell (Windows):

```powershell
.\build.ps1                         # Release build + dist\cODTthreadModeler.bundle + .zip
.\build.ps1 -Configuration Debug    # Debug build (keeps .pdb)
.\build.ps1 -Install                # also copies the bundle to %ProgramData%\Autodesk\ApplicationPlugins (elevated prompt)
.\build.ps1 -InventorInteropPath "D:\Inventor 2027\Bin\Public Assemblies\Autodesk.Inventor.Interop.dll"
```

Bash (Git Bash, Linux, macOS – the project sets `EnableWindowsTargeting`):

```bash
./build.sh            # Release
./build.sh Debug
```

Both scripts produce:

```
dist/
  cODTthreadModeler.bundle/
    PackageContents.xml
    Contents/
      coolOrange.ThreadModeler.Inventor.addin
      ThreadModeler.dll
      ThreadModeler.deps.json
      ThreadModeler.runtimeconfig.json
      ThreadModeler.dll.log4net
      log4net.dll
      Thread Templates/BSW Template.ipt
      Thread Templates/ISO Template.ipt
  cODTthreadModeler.bundle.zip
```

A plain `dotnet build ThreadModeler.sln -c Release` puts the same `Contents` files into `bin\Release\Contents\`.

## Debugging in Visual Studio

1. Build `Debug|x64` once with `.\build.ps1 -Configuration Debug -Install` (elevated), **or** create a
   development bundle that points at your build output, e.g. copy `ThreadModeler\PackageContents.xml`
   into `%ProgramData%\Autodesk\ApplicationPlugins\cODTthreadModeler.bundle\` and create a directory junction
   `Contents` → `<repo>\bin\Debug\Contents`.
2. The Debug configuration starts `C:\Program Files\Autodesk\Inventor 2027\Bin\Inventor.exe`
   (change `StartProgram` in `ThreadModeler.csproj` for Inventor 2026).
3. Press F5. Breakpoints in `StandardAddInServer.Activate` are hit while Inventor starts.

Close Inventor before rebuilding – it locks `ThreadModeler.dll`.

## Versioning

The assembly version lives in `ProductInfo.cs`; the bundle version in `ThreadModeler\PackageContents.xml`
(`AppVersion`, `FriendlyVersion`, `ComponentEntry/@Version`). Keep them in sync.
The major version follows the Inventor internal version (30 = Inventor 2026).

## Continuous integration

`.github/workflows/build.yml` builds the bundle on `windows-latest` for every push / pull request
and uploads `cODTthreadModeler.bundle.zip` as a workflow artifact.
