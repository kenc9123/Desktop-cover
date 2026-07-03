# TopoShell

TopoShell is an experimental Windows 11 desktop interaction layer. It explores an industrial functional interface style: minimal at first glance, but structured, dense, and useful up close.

It is not a wallpaper app and not a desktop widget collection. The default goal is to provide a safe, reversible desktop layer with a Dock, command panel, status surface, parallax scene, and later audio-reactive system visuals without replacing `explorer.exe`, changing the Windows shell registry value, or hooking the taskbar.

An opt-in shell replacement experiment now exists under `tools/shell-mode`. It targets the current-user HKCU shell value only, does not modify the `explorer.exe` file, and includes restore scripts.

Chinese README: [READMECN.md](READMECN.md)

## Status

> Phase 0 prototype work has started and the first WPF prototype builds successfully.

> The current implementation is a native WPF prototype first, so the project can run without depending on network package restore. WebView2 / Three.js remains a later option for richer 3D scenes.

## Prototype

Current prototype features:

- Borderless desktop-style WPF window
- Industrial functional dark interface
- Central circular system core
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
- System tray entry for hiding, restoring, and exiting the shell
- Runtime toggles for motion response, audio response, and always-on-top mode
- Monochrome black/white industrial interface language
- Low-brightness grid and terrain-like background line field
- Shortcut/application rails intentionally deferred for future profile-specific desktop variants
- Command slot toggled by `Ctrl+Space` or the footer slot, with compact runtime control actions

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

- [2026-06-30 Development Log](dev-log/2026-06-30.md)
- [2026-06-30 Chinese Development Log](dev-log/2026-06-30CN.md)
- [2026-07-03 Development Log](dev-log/2026-07-03.md)
- [2026-07-03 Chinese Development Log](dev-log/2026-07-03CN.md)
- [2026-06-29 Development Log](dev-log/2026-06-29.md)
