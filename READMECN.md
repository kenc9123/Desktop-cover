# TopoShell

[English](./README.md) 丨 简体中文

TopoShell 是一个 Windows 11 桌面交互层实验项目。它探索一种工业机能风格的系统增强体验：第一眼简洁，近看有结构、有密度，也有明确功能。

它不是壁纸软件，也不是桌面小组件合集。默认目标是在不替换 `explorer.exe`、不修改系统 Shell 注册表、不 hook 任务栏的前提下，提供一个安全、可退出、可恢复的桌面层，包括可配置 Dock、命令面板、状态信息、视差场景和音频响应式系统视觉。

可选的 Shell 替换实验位于 `tools/shell-mode`。它只修改当前用户的 HKCU Shell 值，不会修改磁盘上的 `explorer.exe` 文件，并提供恢复脚本。

## 当前状态

> Phase 0 原型已经开始，当前 WPF 原型可以成功构建。

> 当前实现优先使用原生 WPF，减少网络包依赖，先保证可以运行、可以验证视觉方向。WebView2 / Three.js 仍保留为后续更复杂 3D 场景的选项。

## 原型功能

当前原型包含：

- 无边框桌面风格 WPF 窗口
- 工业机能风格深色界面
- 用于 Shell 替换测试的主屏全屏桌面层
- 为底部 Dock 留出空间的中央圆形系统核心
- 使用 WPF `Viewport3D` 生成真实 3D 机械核心，包含挤出圆环、机械板和刻度块
- 使用 `Viewport2DVisual3D` 将时间、硬件、音频和媒体封面面板贴附到同一个 3D 圆盘姿态上
- 使用紧贴圆盘表面的媒体、遥测和时间贴片，旧的中心 2D 信息层已移除
- 鼠标驱动的透视 3D 姿态旋转
- 通过 Windows 原生系统时间计数读取 CPU 占用
- 通过 Windows 原生内存状态 API 读取内存占用
- GPU 模块位预留，后续接入稳定的 GPU 遥测提供器
- Core Audio 系统输出峰值监听
- 使用圆盘实体径向条表现音频响应
- 播放器暴露媒体会话时读取标题、歌手、专辑信息
- 播放器暴露专辑封面时，在圆盘上显示稳定封面仓，并保留轻微音频电平脉冲
- 从 `dock-shortcuts.json` 读取的可配置底部 Dock
- Dock 按钮会尽量提取应用图标，提取失败时使用文字首字母
- 系统托盘入口，可切换运行状态并安全退出 TopoShell
- 运行时开关：鼠标动效、音频响应、窗口置顶
- 安全退出路径：Explorer 未运行时会先启动 Explorer 再退出
- 黑白单色工业界面语言
- 低亮度网格和类地形背景线场
- 命令槽可通过 `Ctrl+Space` 或 Dock 右侧按钮打开

## Dock 配置

底部 Dock 会从应用输出目录读取 `dock-shortcuts.json`。源配置文件位于：

```text
src/TopoShell.App/dock-shortcuts.json
```

示例：

```json
{
  "label": "Terminal",
  "target": "wt.exe",
  "arguments": "",
  "workingDirectory": ""
}
```

`target` 可以是 PATH 中的可执行文件名、完整 exe 路径、快捷方式、文档、文件夹，或 Windows 可以通过 Shell 打开的 URL。`arguments` 和 `workingDirectory` 可选。

## 构建和运行

下面的命令会把 .NET CLI home 和 NuGet 包缓存限制在项目目录内。

```powershell
$env:DOTNET_CLI_HOME='F:\Workspace\Personal\TopoShell\dotnet-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
$env:NUGET_PACKAGES='F:\Workspace\Personal\TopoShell\nuget-packages'
dotnet build F:\Workspace\Personal\TopoShell\src\TopoShell.App\TopoShell.App.csproj
dotnet run --project F:\Workspace\Personal\TopoShell\src\TopoShell.App\TopoShell.App.csproj --no-build
```

如果本地缓存已经刷新过，后续构建也可以使用 `--no-restore`。

## Shell 替换实验

- 普通运行模式仍然不替换 Explorer
- 可选实验脚本位于 `tools/shell-mode`
- 目标：`HKCU\Software\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell`
- 范围：当前用户
- 恢复脚本：`tools/shell-mode/restore-explorer-shell.ps1`
- 中文说明：[Shell 替换实验](docs/04-shell-replacement-experimentCN.md)

## 设计关键词

- 工业机能风格
- 简洁但不空
- 简单但保留结构复杂度
- Windows 原生感
- 低干扰桌面 HUD
- 细线网格和参数面板
- 鼠标驱动视差
- 可退出、可恢复、可回退

## 文档

- [sonic-topography 调研](docs/00-sonic-topography-research.md)
- [产品愿景](docs/01-product-vision.md)
- [技术架构草案](docs/02-technical-architecture.md)
- [MVP 路线图](docs/03-mvp-roadmap.md)
- [Shell 替换实验](docs/04-shell-replacement-experimentCN.md)

## 开发日记

- [2026-07-08 开发日记](dev-log/2026-07-08CN.md)
- [2026-07-08 English Development Log](dev-log/2026-07-08.md)
- [2026-07-04 开发日记](dev-log/2026-07-04CN.md)
- [2026-07-04 English Development Log](dev-log/2026-07-04.md)
- [2026-07-03 开发日记](dev-log/2026-07-03CN.md)
- [2026-07-03 English Development Log](dev-log/2026-07-03.md)
- [2026-06-30 开发日记](dev-log/2026-06-30CN.md)
- [2026-06-30 English Development Log](dev-log/2026-06-30.md)
- [2026-06-29 开发日记](dev-log/2026-06-29CN.md)
- [2026-06-29 English Development Log](dev-log/2026-06-29.md)
