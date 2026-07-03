# TopoShell

## 2026-07-03 更新

- 增加系统托盘入口，可以隐藏、恢复和退出 TopoShell。
- 增加运行时开关：鼠标动效、音频响应、窗口置顶。
- `Ctrl+Space` 命令槽已从占位内容改为可点击的运行控制台。
- 快捷方式/应用启动栏继续后置，等待后续按不同桌面 profile 拆分。

## Shell 替换实验

- 普通运行模式仍然不替换 Explorer。
- 可选实验脚本位于 `tools/shell-mode`。
- 脚本只修改当前用户的 `HKCU\Software\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell`。
- 脚本不会替换磁盘上的 `explorer.exe` 文件。
- 恢复脚本：`tools/shell-mode/restore-explorer-shell.ps1`。
- 中文说明：[04-shell-replacement-experimentCN.md](docs/04-shell-replacement-experimentCN.md)。

TopoShell 是一个 Windows 11 桌面交互层实验项目，目标是探索一种工业机能风的系统增强体验：第一眼简约，近看有结构、有密度、有明确功能。

它不是壁纸软件，也不是 Rainmeter 式小组件合集。TopoShell 的方向是在不替换 `explorer.exe`、不修改系统 Shell 注册表、不 hook 任务栏的前提下，提供一个安全可退出、可恢复的桌面层，包括 Dock、命令面板、状态信息、视差场景，以及后续的音频响应式系统视觉。

英文 README：[README.md](README.md)

## 当前状态

> 阶段 0 原型已经开始，第一版 WPF 原型已经可以成功构建。

> 当前实现优先做原生 WPF 原型，减少网络包依赖，先保证可以运行、可以验证视觉方向。WebView2 / Three.js 仍然保留为后续更复杂 3D 场景的选择。

## 原型功能

当前原型包含：

- 无边框桌面风格 WPF 窗口
- 工业机能风深色界面
- 中央圆形系统核心
- 使用 WPF `Viewport3D` 生成真实 3D 机械核心，包含挤出圆环、机械板和刻度块
- 使用 `Viewport2DVisual3D` 将时间、硬件、音频、媒体封面面板贴附到同一个 3D 圆盘姿态上
- 使用紧贴圆盘表面的媒体、遥测和时间贴片，旧的中心 2D 信息层已移除
- 鼠标驱动的透视 3D 姿态旋转
- 通过 Windows 原生系统时间计数读取 CPU 占用
- 通过 Windows 原生内存状态 API 读取内存占用
- GPU 模块位已预留，后续接入稳定的 GPU 遥测提供器
- Core Audio 系统输出峰值监听
- 使用圆盘实体径向条表现音频监听，而不是独立浮动音频面板
- Windows 媒体会话标题、歌手、专辑信息读取
- 播放器暴露专辑封面时，在圆盘上显示稳定封面仓，并保留轻微音频电平脉冲
- 黑白单色工业界面语言
- 低亮度网格和类地形背景线场
- 快捷方式/应用启动栏暂时后置，后续用于不同需求的桌面版本
- 命令槽占位，可通过 `Ctrl+Space` 唤出

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

如果本地缓存已经刷新过，也可以在后续构建时使用 `--no-restore`。

## 设计关键词

- 工业机能风
- 简约但不空
- 简单但保留结构复杂度
- Windows 原生感
- 低干扰桌面 HUD
- 细线网格和参数面板
- 鼠标驱动视差
- 可退出、可恢复、可逆

## 文档

- [sonic-topography 调研](docs/00-sonic-topography-research.md)
- [产品愿景](docs/01-product-vision.md)
- [技术架构草案](docs/02-technical-architecture.md)
- [MVP 路线图](docs/03-mvp-roadmap.md)

## 开发日志

- [2026-06-30 开发日志](dev-log/2026-06-30CN.md)
- [2026-06-30 英文开发日志](dev-log/2026-06-30.md)
- [2026-06-29 开发日志](dev-log/2026-06-29.md)
