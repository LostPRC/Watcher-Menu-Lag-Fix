# Watcher Menu Lag Fix (雨世界 Watcher 主菜单卡顿修复模组)

<p align="center">
  <img src="https://img.shields.io/badge/Game-Rain%20World%20v1.9%2B%20%7C%20v1.11.5%2B-blue?style=flat-square" alt="Game Version">
  <img src="https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20(Wine%2FProton)%20%7C%20Steam%20Deck-success?style=flat-square" alt="Platform">
  <img src="https://img.shields.io/badge/Framework-BepInEx%205%20%7C%20MonoMod%20%7C%20Remix-orange?style=flat-square" alt="Framework">
  <img src="https://img.shields.io/badge/Author-Antigravity-purple?style=flat-square" alt="Author">
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="License">
</p>

---

## 📖 简介 / Overview

**Watcher Menu Lag Fix** 是一个专门针对《雨世界》（*Rain World*）在启用 **The Watcher DLC** 扩展模组后，主菜单出现**严重掉帧（FPS 暴跌）、微卡顿（Micro-stutter）与鼠标输入延迟**问题的轻量级底层优化模组。

本模组采用非侵入式的动态内存挂钩（MonoMod RuntimeDetour），不修改任何游戏原文件，彻底根治主界面性能问题，让游戏主菜单无论在任何硬件配置或系统环境下均能保持 **60+ FPS 满帧丝滑运行**。

---

## 🎯 适用场景 / Applicable Scenarios

如果你在使用《雨世界》时遇到以下任一情况，本模组将完美解决你的问题：

1. **启用了 Watcher DLC**：在 Remix 模组列表中启用了 *The Watcher*，进入主菜单后帧率从平时的 60 FPS 骤降至 15~25 FPS。
2. **鼠标移动触发卡顿**：在主界面只要晃动或移动鼠标光标，画面就出现明显的顿挫、撕裂或微卡顿（Micro-stutter）。
3. **Linux / Steam Deck / Wine / Proton 用户**：在 Linux 系统（如 Steam Deck、Arch / EndeavourOS、Ubuntu）或通过 Wine/Proton 运行游戏时，DXVK 着色器多通道混合开销导致主菜单卡顿尤为明显。
4. **核显与轻薄本用户**：搭载 Intel 核显（Iris Xe / UHD）、AMD 集成显卡（Radeon 600M / 700M / 800M）或中低端独立显卡的设备。
5. **想要自定义主菜单风格**：想要自由在 **Watcher 官方 2D 平面图**、**Downpour 倾盆大雨背景** 或 **原版经典背景** 之间自由切换的玩家。

---

## 🔬 深入原理剖析 / Root Cause & Technical Principles

通过对游戏核心程序集 `Assembly-CSharp.dll` 与资产文件的逆向工程分析，发现启用 Watcher 后主菜单卡顿是由以下三重原因交织导致的：

### 1. 12 层超高分辨率堆叠着色器（GPU 填充率与带宽瓶颈）
* **原版 / Downpour**：主菜单仅有 4~5 个轻量图层。
* **Watcher 主菜单 (`MainMenu_Watcher`)**：堆叠了多达 **12 张全屏高分大图（分辨率高达 1531×1460）**。
* 更严重的是，这 12 个图层分别应用了 `SceneBlurLightEdges`、`SceneOverlay`、`SceneLighten`、`SceneBlur` 等多通道重型 Shader。显卡在每一帧都必须对 12 层巨大纹理进行多次模糊与边缘光照合成采样，导致 GPU 填充率（Fill-rate）与显存带宽瞬间被榨干。

### 2. 鼠标悬停时的每帧 CPU 纹理像素扫描（CPU 缓存失效与帧时间突刺）
* 在 `InteractiveMenuScene.Update` 中，当鼠标移动时（`mouseInActiveCounter > 20`），引擎会循环遍历所有图层并调用 `MenuDepthIllustration.DepthAtPosition(...)`。
* 在该方法内，游戏为了计算动态景深聚焦，在每一帧对每一层图层反复执行底层调用：
  ```csharp
  texture.GetPixel((int)ps.x, (int)ps.y)
  texture.GetPixel((int)ps.x, (int)(size.y + ps.y))
  ```
* 从 CPU 内存中对 12 张未压缩的巨大图片进行非连续内存地址寻址，导致频繁的 CPU L2/L3 缓存失效（Cache Thrashing）和线程锁阻塞。表现为**鼠标一动，画面帧时间剧烈跳动**。

### 3. 硬编码强制覆盖场景
* 只要检测到 `ModManager.Watcher` 启用，`MainMenu` 构造函数就会无条件将主场景硬编码重定向至高负载的 `MainMenu_Watcher`，使玩家无法通过普通设置规避。

---

## 🛠️ 模组修复方案 / How It Works

本模组通过精准的“手术刀式”内存注入，实施以下针对性修复：

```
[Rain World Engine]
       │
       ▼
[MenuScene.BuildWatcherMainMenuScene] ──► [Hook: 注入 flatMode = true] ──► 加载官方 2D 高清艺术图 (60 FPS 满帧)
       │
       ▼
[MenuDepthIllustration.DepthAtPosition] ──► [Hook: 拦截 CPU GetPixel] ──► 直接返回预设 depth (消除鼠标卡顿)
       │
       ▼
[Remix OptionInterface] ──► 游戏内可视化配置面板 (可自选 2D/3D 模式与背景主题)
```

1. **官方 2D 平面艺术背景注入（核心优化，默认开启）**：
   * 实际上，官方美术团队在制作 DLC 时已经打包了高清 2D 版本的单张艺术插画 `main menu watcher - flat.png`（1366×768）。
   * 模组在构建场景时临时激活 `flatMode = true`，直接加载官方 2D 平面艺术背景。**彻底免除 12 层多通道 Shader 的 GPU 负担，帧率直接锁满 60+ FPS，同时完整保留官方精美画风**。
2. **旁路拦截 CPU 像素深度射线检测（默认开启）**：
   * 挂钩 `DepthAtPosition`，针对主菜单图层直接返回当前层深度值，**阻断每一帧对底层 `texture.GetPixel` 的调用**，彻底消除了鼠标悬停或晃动时的微卡顿。
3. **原生 Remix 游戏内配置支持**：
   * 模组提供标准的 `OptionInterface`，在游戏内 **REMIX** 设置中支持：
     * 自由开关 **2D Flat 模式**
     * 自由开关 **CPU 像素扫描优化**
     * 自定义主菜单主题：**Watcher** / **Downpour (倾盆大雨)** / **Vanilla (原版)**

---

## 📂 项目与代码结构 / Project Structure

```text
WatcherMenuLagFix/
├── src/                                  # 源码工程
│   ├── WatcherMenuLagFix.csproj          # .NET Standard 2.0 项目配置文件
│   ├── Plugin.cs                         # BepInEx 插件入口与核心 Detour 钩子
│   └── WatcherMenuFixOptions.cs          # Remix 游戏内图形化选项界面
├── dist/                                 # 编译分发包 (Ready-to-use)
│   └── WatcherMenuLagFix/                # Remix 模组标准文件夹
│       ├── modinfo.json                  # 模组元数据信息
│       └── plugins/
│           ├── WatcherMenuLagFix.dll     # 编译就绪的二进制插件
│           ├── WatcherMenuLagFix.pdb     # 调试符号
│           └── WatcherMenuLagFix.deps.json
└── README.md                             # 说明文档
```

---

## 📥 安装指南 / Installation

你可以根据自己的习惯选择以下任一方式安装：

### 方式 A：作为 Remix 模组安装（推荐，支持在游戏内配置）
将 `dist/WatcherMenuLagFix` 文件夹放入游戏的 `StreamingAssets/mods` 目录中：
```bash
# Linux 软链接方式（不占用额外空间）：
ln -s /绝对路径/WatcherMenuLagFix/dist/WatcherMenuLagFix \
      "雨世界根目录/RainWorld_Data/StreamingAssets/mods/"

# 或直接复制文件夹：
cp -r /绝对路径/WatcherMenuLagFix/dist/WatcherMenuLagFix \
      "雨世界根目录/RainWorld_Data/StreamingAssets/mods/"
```
启动游戏后，在主菜单点击 **REMIX**，勾选启用 **Watcher Menu Lag Fix** 并点击应用即可。

---

### 方式 B：作为全局 BepInEx 插件安装（极简）
直接将编译出的 DLL 复制到 `BepInEx/plugins/` 目录：
```bash
cp /绝对路径/WatcherMenuLagFix/dist/WatcherMenuLagFix/plugins/WatcherMenuLagFix.dll \
   "雨世界根目录/BepInEx/plugins/"
```
游戏启动后将自动全局加载生效。

---

## 🔨 从源码编译 / Build from Source

本项目使用标准 .NET SDK 开发，支持跨平台编译（Windows / Linux / macOS）：

```bash
# 需已安装 .NET 6.0+ SDK
cd src
dotnet build WatcherMenuLagFix.csproj -c Release
```
编译成功后，产物将自动输出并打包至 `dist/WatcherMenuLagFix/plugins/`。

---

## 🤝 兼容性 / Compatibility

* **兼容所有其他模组**：本模组完全遵循 BepInEx 5 与 MonoMod MMHOOK 规范，不覆盖任何游戏原生文件，与其他模组（如 SBCameraScroll、Dress My Slugcat、Rotund World 等）完全兼容。
* **存档安全**：纯客户端视觉渲染优化，不影响游戏存档数据。

---

## 👤 作者与署名 / Author & Credits

* **Author**: **Antigravity**
* **Game**: *Rain World* by Videocult & Akupara Games
* **License**: [MIT License](LICENSE)
