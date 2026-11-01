# Installing and using ThreadModeler

## Requirements

- Windows x64
- Autodesk Inventor **2027**, or Inventor **2026 with Update 2026.4 or later**
  (earlier 2026 builds run add-ins on .NET 8 and cannot load this .NET 10 add-in)

No separate .NET installation is needed – the add-in runs on the .NET 10 runtime that ships with Inventor.

## Install

1. Close Inventor.
2. If an older ThreadModeler was installed with the MSI installer, uninstall it via
   *Settings → Apps* ("coolOrange Inventor ThreadModeler").
3. Unzip `cODTthreadModeler.bundle.zip` (or take `dist\cODTthreadModeler.bundle` from the build).
4. Copy the `cODTthreadModeler.bundle` folder to

   ```
   %ProgramData%\Autodesk\ApplicationPlugins\
   ```

   (typically `C:\ProgramData\Autodesk\ApplicationPlugins\cODTthreadModeler.bundle`). Administrator rights are required.
5. If the zip was downloaded from the internet, unblock the files first
   (`Get-ChildItem -Recurse | Unblock-File` in PowerShell inside the bundle folder), otherwise Windows may refuse to load the DLL.
6. Start Inventor.

## Use

1. Open a part (`.ipt`) that contains one or more **Thread** features.
2. Go to the **coolOrange** ribbon tab → **threadModeler** panel → **threadModeler**.
3. The *Thread Modeler Control* dialog opens. Pick thread features in the model; they are listed under
   *Selected Threads*. Optionally adjust *Pitch Offset* (thread pitch + %), then click **Ok**.
   The *Thread Sketch Template* field is filled automatically from `Contents\Thread Templates`;
   use **...** to choose another template.

**Help** and **About** are in the panel's slide-out.

## Uninstall

Close Inventor and delete `%ProgramData%\Autodesk\ApplicationPlugins\cODTthreadModeler.bundle`.

To keep it installed but not loaded, use *Tools → Add-Ins* in Inventor and clear **Loaded** / **Load Automatically**
for *coolOrange - ThreadModeler*.

## Logs

Errors are written with log4net to

```
%ProgramData%\Autodesk\ApplicationPlugins\cODTthreadModeler.bundle\Contents\Log\threadModeler.log
```

and to the debugger output (viewable with Sysinternals DebugView). The log level is set in
`Contents\ThreadModeler.dll.log4net` (`ERROR` by default; use `DEBUG` for more detail).

## Troubleshooting

| Symptom | Check |
|---|---|
| Add-in not listed in *Tools → Add-Ins* | Bundle folder location; `PackageContents.xml` must be directly inside `cODTthreadModeler.bundle`; Inventor version is 2026 (30) or later |
| Listed, but fails to load in Inventor 2026 | Inventor must be on **Update 2026.4+** (.NET 10). *Help → About* shows the build. |
| Fails to load after downloading the zip | Unblock the files (see Install step 5) |
| No log file is written | Standard users may lack write access to `ProgramData`; run DebugView, or grant write access to `Contents\Log` |
| *Thread Sketch Template* field is empty | `Contents\Thread Templates\*.ipt` must sit next to `ThreadModeler.dll`, or pick a template with **...** |
