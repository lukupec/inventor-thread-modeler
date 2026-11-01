# Installing and using ThreadModeler

## Requirements

- Windows x64
- Autodesk Inventor **2027**, or Inventor **2026 with Update 2026.4 or later**

  Inventor 2026.0 – 2026.3 runs add-ins on .NET 8 and **cannot** load this .NET 10 add-in. It then just
  shows *Automatic / Unloaded* in the Add-In Manager. Check *Help → About Autodesk Inventor* and install
  the latest Inventor 2026 update.

No separate .NET installation is needed. The add-in runs on the .NET 10 runtime that Inventor uses.

## Install (recommended: MSI)

1. Download `ThreadModeler-<version>.msi` from the
   [latest release](https://github.com/lukupec/inventor-thread-modeler/releases/latest).
2. Double-click it, accept the license, and confirm the Windows administrator prompt.
   If Inventor is running, the installer asks you to close it.
3. Start Inventor and open a part. The commands are on the **ThreadModeler** ribbon tab.

The MSI installs the add-in for all users to

```
C:\ProgramData\Autodesk\ApplicationPlugins\ThreadModeler.bundle
```

It also upgrades older installations made with an earlier ThreadModeler MSI (up to 30.0.2 they used the folder
`cODTthreadModeler.bundle`, which the upgrade removes).

> The MSI is not code-signed. If Microsoft Defender SmartScreen shows *Windows protected your PC*,
> click **More info → Run anyway**.

### Silent / IT deployment (Intune, GPO, SCCM)

```powershell
msiexec /i ThreadModeler-30.0.1.msi /qn
msiexec /i ThreadModeler-30.0.1.msi /qn DISABLEUPDATECHECK=1   # IT manages updates; users are not prompted
```

Uninstall silently with `msiexec /x ThreadModeler-30.0.1.msi /qn`, or by product name through your deployment tool.

## Updates

The add-in checks for new versions by itself:

- About 20 seconds after Inventor starts, at most **once a day**, it asks GitHub for the latest release
  of this repository. It sends no personal data. The request is a plain HTTPS GET to `api.github.com`.
- If a newer version exists, a dialog offers:
  - **Download and install**: downloads the new MSI and verifies its SHA-256 checksum. The update is
    **installed after you close Inventor**, with a progress window and the administrator prompt.
    Inventor is never closed for you.
  - **Remind me later**: asks again at the next daily check.
  - **Skip this version**: is not offered again. A later version is.
- **Check for Updates** in the ThreadModeler panel's slide-out checks immediately.

Turn the automatic check off:

| Scope | How |
|---|---|
| All users | Install with `DISABLEUPDATECHECK=1`, or set `HKLM\SOFTWARE\ThreadModeler` → `DisableUpdateCheck` (DWORD) = `1` |
| Current user | Set `HKCU\SOFTWARE\ThreadModeler` → `DisableUpdateCheck` (DWORD) = `1` |

The update state (last check, skipped version) is kept in `%LOCALAPPDATA%\ThreadModeler\update.json`.

## Use

1. Open a part (`.ipt`) that contains one or more **Thread** features.
2. On the **ThreadModeler** ribbon tab, click **ThreadModeler**.
3. The *Thread Modeler Control* dialog opens. Pick thread features in the model; they appear under
   *Selected Threads*. Optionally adjust *Pitch Offset* (thread pitch + %), then click **Ok**.
   The *Thread Sketch Template* field is filled automatically from `Contents\Thread Templates`.
   Use **...** to choose another template.

**Help**, **Check for Updates** and **About** are in the panel's slide-out.

## Uninstall

*Settings → Apps → Installed apps → ThreadModeler for Autodesk Inventor → Uninstall.*

To keep it installed but not loaded, use *Tools → Add-Ins* in Inventor. Clear **Loaded** / **Load Automatically**
for *ThreadModeler*.

## Manual install (without the MSI)

1. Close Inventor.
2. From the release, download `ThreadModeler.bundle.zip` and unzip it. You get the folder `ThreadModeler.bundle`.
3. Copy that folder to `C:\ProgramData\Autodesk\ApplicationPlugins\` (administrator rights needed).
4. Unblock the files: in PowerShell inside the bundle folder, run `Get-ChildItem -Recurse | Unblock-File`.

Uninstall by deleting the folder. The automatic update check also works for manual installs, but it installs the MSI.

## Logs

The add-in writes a short log per user to

```
%LOCALAPPDATA%\ThreadModeler\ThreadModeler.log
```

It contains how it was loaded (runtime, load context, Inventor interop version), activation, update checks
and errors. It is also written to the debugger output, which you can view with Sysinternals DebugView.

If the add-in fails while loading, the error is shown in a message box and appended to
`%TEMP%\ThreadModeler-load-error.txt`. That file includes the Inventor and .NET versions.

## Troubleshooting

| Symptom | Check |
|---|---|
| *Automatic / Unloaded* in the Add-In Manager, no message | 1. Inventor 2026 below **Update 2026.4**: update Inventor. To see the runtime Inventor uses, run this while Inventor is open: `(Get-Process Inventor).Modules \| ? ModuleName -eq coreclr.dll \| select FileName`. The path must contain `Microsoft.NETCore.App\10.` 2. Check the end of `%LOCALAPPDATA%\ThreadModeler\ThreadModeler.log`. If there is no entry from this Inventor session, Inventor never loaded `ThreadModeler.dll`. If there is a *loaded* line but no *Activated* line, the log shows why. |
| Message "ThreadModeler failed to load" | Send `%TEMP%\ThreadModeler-load-error.txt` and `ThreadModeler.log` along with the issue |
| Add-in not listed in *Tools → Add-Ins* | `C:\ProgramData\Autodesk\ApplicationPlugins\ThreadModeler.bundle\PackageContents.xml` must exist |
| Add-in listed twice, or the old name *coolOrange - ThreadModeler* still shows | A manually copied old bundle is still there: delete `C:\ProgramData\Autodesk\ApplicationPlugins\cODTthreadModeler.bundle` |
| Update check says it could not connect | The PC needs HTTPS access to `api.github.com`, `github.com` and `objects.githubusercontent.com` (proxy/firewall) |
| *Thread Sketch Template* field is empty | `Contents\Thread Templates\*.ipt` must sit next to `ThreadModeler.dll`, or pick a template with **...** |
