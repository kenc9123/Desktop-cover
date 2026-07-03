# Shell Replacement Experiment

This document covers the optional TopoShell shell-mode experiment.

Normal TopoShell does not replace Explorer. Shell mode is a deliberate experiment that changes the current user's Windows shell value so Windows starts TopoShell instead of `explorer.exe` after sign-in.

## Safety Model

- The scripts do not replace or modify the `explorer.exe` file.
- The scripts do not write machine-wide HKLM shell settings.
- The scripts only target `HKCU\Software\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell`.
- The enable script writes a project-local backup before changing the value.
- The restore script can restore the backed-up value or default back to `explorer.exe`.

## Prepare

Build TopoShell first:

```powershell
cd F:\Workspace\Personal\TopoShell

$env:DOTNET_CLI_HOME='F:\Workspace\Personal\TopoShell\dotnet-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
$env:NUGET_PACKAGES='F:\Workspace\Personal\TopoShell\nuget-packages'

dotnet build F:\Workspace\Personal\TopoShell\src\TopoShell.App\TopoShell.App.csproj
```

Check the current shell value:

```powershell
powershell -ExecutionPolicy Bypass -File F:\Workspace\Personal\TopoShell\tools\shell-mode\check-shell-mode.ps1
```

## Enable TopoShell As Current-User Shell

```powershell
powershell -ExecutionPolicy Bypass -File F:\Workspace\Personal\TopoShell\tools\shell-mode\enable-toposhell-shell.ps1
```

Sign out and sign back in, or reboot. The change applies on the next sign-in.

## Restore Explorer

From a normal PowerShell session:

```powershell
powershell -ExecutionPolicy Bypass -File F:\Workspace\Personal\TopoShell\tools\shell-mode\restore-explorer-shell.ps1
```

Then sign out and sign back in, or reboot.

## Emergency Recovery

If you sign in and only TopoShell appears:

1. Press `Ctrl+Shift+Esc` to open Task Manager.
2. Choose `Run new task`.
3. Run:

```powershell
powershell -ExecutionPolicy Bypass -File F:\Workspace\Personal\TopoShell\tools\shell-mode\restore-explorer-shell.ps1
```

4. In Task Manager, run:

```powershell
explorer.exe
```

This restores the normal desktop immediately for the current session and restores the shell value for the next sign-in.
