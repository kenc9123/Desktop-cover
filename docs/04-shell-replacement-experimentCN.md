# Shell 替换实验

这份文档说明 TopoShell 的可选 Shell 模式实验。

普通运行模式不会替换 Explorer。Shell 模式是一个主动选择的实验：把当前用户的 Windows Shell 值改为 TopoShell，让 Windows 在下次登录后启动 TopoShell，而不是启动 `explorer.exe`。

在这个模式下，TopoShell 会作为主屏全屏桌面层显示。从右上角 `X` 或命令面板退出时，如果 Explorer 没有运行，TopoShell 会先尝试启动 `explorer.exe`，再关闭自身。

## 安全模型

- 脚本不会替换或修改磁盘上的 `explorer.exe` 文件。
- 脚本不会写入 HKLM 全机器 Shell 设置。
- 脚本只修改 `HKCU\Software\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell`。
- 启用脚本会在修改前把原始值备份到项目目录。
- 恢复脚本可以恢复备份值；如果没有备份，则默认恢复为 `explorer.exe`。

## 准备

先构建 TopoShell：

```powershell
cd F:\Workspace\Personal\TopoShell

$env:DOTNET_CLI_HOME='F:\Workspace\Personal\TopoShell\dotnet-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
$env:NUGET_PACKAGES='F:\Workspace\Personal\TopoShell\nuget-packages'

dotnet build F:\Workspace\Personal\TopoShell\src\TopoShell.App\TopoShell.App.csproj
```

查看当前 Shell 值：

```powershell
powershell -ExecutionPolicy Bypass -File F:\Workspace\Personal\TopoShell\tools\shell-mode\check-shell-mode.ps1
```

## 启用 TopoShell 当前用户 Shell

```powershell
powershell -ExecutionPolicy Bypass -File F:\Workspace\Personal\TopoShell\tools\shell-mode\enable-toposhell-shell.ps1
```

注销并重新登录，或者重启。这个改动会在下一次登录时生效。

## 恢复 Explorer

在普通 PowerShell 中运行：

```powershell
powershell -ExecutionPolicy Bypass -File F:\Workspace\Personal\TopoShell\tools\shell-mode\restore-explorer-shell.ps1
```

然后注销并重新登录，或者重启。

## 紧急恢复

如果登录后只剩 TopoShell：

1. 按 `Ctrl+Shift+Esc` 打开任务管理器。
2. 选择“运行新任务”。
3. 运行：

```powershell
powershell -ExecutionPolicy Bypass -File F:\Workspace\Personal\TopoShell\tools\shell-mode\restore-explorer-shell.ps1
```

4. 在任务管理器里再运行：

```powershell
explorer.exe
```

这样可以立刻恢复当前会话的普通桌面，并把下一次登录的 Shell 值恢复回 Explorer。
