# TopoShell

TopoShell is an experimental Windows 11 desktop interaction layer. It explores an industrial functional interface style: minimal at first glance, but structured, dense, and useful up close.

It is not a wallpaper app and not a desktop widget collection. The default goal is to provide a safe, reversible desktop layer with a configurable Dock, command panel, status surface, parallax scene, and audio-reactive system visuals without replacing `explorer.exe`, changing the Windows shell registry value, or hooking the taskbar.

An opt-in shell replacement experiment now exists under `tools/shell-mode`. It targets the current-user HKCU shell value only, does not modify the `explorer.exe` file, and includes restore scripts.

Chinese README: [READMECN.md](READMECN.md)

## Status

> Phase 0 prototype work has started and the first WPF prototype builds successfully.

> The current implementation is a native WPF prototype first, so the project can run without depending on network package restore. WebView2 / Three.js remains a later option for richer 3D scenes.

## Prototype

Current prototype features:

- Borderless desktop-style WPF window
- Industrial functional dark interface
- Full-screen primary-display desktop surface for shell replacement testing
- Central circular system core tuned to leave room for the bottom Dock
- Real WPF `Viewport3D` mechanical core with extruded rings, plates, and tick blocks
- `Viewport2DVisual3D` information panels attached to the same 3D disk transform
- Compact surface decals for media, telemetry, and time instead of a hidden center overlay
- Mouse-driven 3D attitude rotation in perspective
- Live CPU usage from native Windows system time counters
- Live memory usage from native Windows memory status APIs
- GPU module slot reserved for a future stable telemetry provider
- Core Audio system output peak listener
- Audio-reactive radial bars built from the physical disk geometry
- Windows media session title / artist / album metadata when exposed by the active player
- Stable album-art bay with subtle audio-level pulse when artwork is available
- Configurable bottom Dock loaded from `dock-shortcuts.json`
- Dock shortcut buttons with extracted app icons when possible and text-initial fallback
- System tray entry for runtime toggles and safe shell exit
- Runtime toggles for motion response, audio response, and always-on-top mode
- Safe close path that starts Explorer when Explorer is not running
- Monochrome black/white industrial interface language
- Low-brightness grid and terrain-like background line field
- Command slot toggled by `Ctrl+Space` or the Dock command button, with compact runtime control actions

## Dock Configuration

The bottom Dock reads `dock-shortcuts.json` from the application output folder. The source profile lives at:

```text
src/TopoShell.App/dock-shortcuts.json
```

Example entry:

```json
{
  "label": "Terminal",
  "target": "wt.exe",
  "arguments": "",
  "workingDirectory": ""
}
```

`target` can be an executable name available on `PATH`, a full executable path, a shortcut, document, folder, or URL that Windows can open through shell execution. `arguments` and `workingDirectory` are optional.

## Build And Run

The commands below keep .NET CLI home and NuGet package cache inside the project folder.

```powershell
$env:DOTNET_CLI_HOME='F:\Workspace\Personal\TopoShell\dotnet-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
$env:NUGET_PACKAGES='F:\Workspace\Personal\TopoShell\nuget-packages'
dotnet build F:\Workspace\Personal\TopoShell\src\TopoShell.App\TopoShell.App.csproj --no-restore
dotnet run --project F:\Workspace\Personal\TopoShell\src\TopoShell.App\TopoShell.App.csproj --no-build
```

If this is a clean checkout after the Windows media-session target framework change, run `dotnet build` once without `--no-restore` so the local project cache can refresh.

## Design Keywords

- Industrial functional style
- Minimal, but not empty
- Simple, but structurally complex
- Native Windows feel
- Low-distraction desktop HUD
- Fine grid lines and parameter panels
- Mouse-driven parallax
- Reversible and easy to exit

## Optional shell replacement experiment:

- Scripts live in `tools/shell-mode`
- Target: `HKCU\Software\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell`
- Scope: current user only
- Recovery: `tools/shell-mode/restore-explorer-shell.ps1`
- Documentation: [Shell replacement experiment](docs/04-shell-replacement-experiment.md)

## Documents

- [sonic-topography research](docs/00-sonic-topography-research.md)
- [Product vision](docs/01-product-vision.md)
- [Technical architecture draft](docs/02-technical-architecture.md)
- [MVP roadmap](docs/03-mvp-roadmap.md)
- [Shell replacement experiment](docs/04-shell-replacement-experiment.md)
- [Shell replacement experiment CN](docs/04-shell-replacement-experimentCN.md)

## Development Logs

- [2026-07-08 Development Log](dev-log/2026-07-08.md)
- [2026-07-08 Chinese Development Log](dev-log/2026-07-08CN.md)
- [2026-07-04 Development Log](dev-log/2026-07-04.md)
- [2026-07-04 Chinese Development Log](dev-log/2026-07-04CN.md)
- [2026-06-30 Development Log](dev-log/2026-06-30.md)
- [2026-06-30 Chinese Development Log](dev-log/2026-06-30CN.md)
- [2026-07-03 Development Log](dev-log/2026-07-03.md)
- [2026-07-03 Chinese Development Log](dev-log/2026-07-03CN.md)
- [2026-06-29 Development Log](dev-log/2026-06-29.md)
