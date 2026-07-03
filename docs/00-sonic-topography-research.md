# sonic-topography 调研

## 项目定位

- 仓库：<https://github.com/yin-yizhen/sonic-topography>
- 作者：`yin-yizhen`
- 当前公开信息显示版本为 `1.1.0`。
- 项目描述：随音乐产生交互的棋盘式海浪，支持网易云搜索。
- 桌面端定位：桌面音乐播放器 + 3D 音频可视化应用。

## 技术栈

从仓库结构和 `package.json` 可确认：

- 桌面壳：Electron。
- 前端：React、TypeScript、Vite。
- 3D 渲染：Three.js、@react-three/fiber、@react-three/drei。
- 动效：framer-motion / motion。
- 样式：Tailwind CSS。
- 构建：electron-builder。

## 视觉可借鉴点

`sonic-topography` 最值得借鉴的是视觉语言，而不是系统集成方式：

- 深色背景里铺一层低亮度 3D 地形或网格，作为“空间感”的底。
- 面板使用半透明深色玻璃、细线描边、少量青色高亮。
- 信息密度较高，但通过弱层级、弱发光、窄间距和模块边界保持秩序。
- 控件像设备参数面板，而不是普通网页表单。
- 3D 场景、歌词、控制面板和设置面板之间有清晰的前后层级。
- 音频不是简单频谱条，而是驱动地形、流动、闪光和节奏起伏。

## 不适合直接作为底座的原因

`sonic-topography` 是一个 Electron 应用，不是 Windows Shell 或桌面环境增强器：

- 不负责 Dock。
- 不负责任务栏替换。
- 不负责窗口管理。
- 不负责系统托盘、全局启动器、虚拟桌面、工作区布局。
- 主要视觉区域仍在单个应用窗口内，而不是贴合 Windows 桌面合成层。

因此它适合作为 `TopoShell` 的视觉和交互参考，但不适合作为系统级桌面项目的直接 fork 底座。

## 对 TopoShell 的启发

TopoShell 应吸收它的“工业机能风”：

- 简约，但不能空。
- 克制，但不能平。
- 信息密度高，但需要有秩序。
- 视觉复杂度来自层级、网格、参数、微动效和材质，而不是装饰。
- 每个面板都像有用途的操作模块，不做无意义的酷炫元素。

## 后续观察点

如果后续要继续看源码，可以重点分析：

- `src/components/AudioVisualizer`
- `src/components/UI`
- `src/lib/AudioEngine.ts`
- `src/lib/groundEqSettings.ts`
- `src/lib/themes.ts`
- `src/lib/terrainResponse.ts`

这些模块能帮助我们理解它如何把音频数据映射到 3D 地形、主题、控制面板和动效参数。
