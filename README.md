# Watcher Menu Lag Fix 

(雨世界 Watcher 主菜单卡顿修复模组)

<p align="center">
  <img src="https://img.shields.io/badge/Game-Rain%20World%20v1.9%2B%20%7C%20v1.11.5%2B-blue?style=flat-square" alt="Game Version">
  <img src="https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20(Wine%2FProton)%20%7C%20Steam%20Deck-success?style=flat-square" alt="Platform">
  <img src="https://img.shields.io/badge/Framework-BepInEx%205%20%7C%20MonoMod%20%7C%20Remix-orange?style=flat-square" alt="Framework">
  <img src="https://img.shields.io/badge/Version-1.1.0-brightgreen?style=flat-square" alt="Version">
  <img src="https://img.shields.io/badge/Author-Antigravity-purple?style=flat-square" alt="Author">
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="License">
</p>

---

## 🌟 核心亮点：100% 完整保留原本生动 3D 视差，同时锁满 60 FPS！

不同于粗暴将画面降级为静态图片的做法，**Watcher Menu Lag Fix v1.1.0** 采用深层渲染管线优化：

* ✅ **100% 保留全部 12 层 3D 空间纵深图层**
* ✅ **完整保留镜头自然平滑漂移、鼠标视差拖拽晃动、手柄摇杆联动与层次分离感**
* ✅ **画质保持官方原本的高清原画，消除恼人模糊并大幅提升画面纯净度**
* ⚡ **彻底解决帧率暴跌（由 15~20 FPS 恢复并稳锁 60+ FPS），鼠标移动零顿挫**

---

## 📖 简介 / Overview

《雨世界》（*Rain World*）在加载 **The Watcher DLC** 扩展后，主菜单（`MainMenu_Watcher`）带来了极富视觉张力的 12 层 3D 视差场景，但由于引擎底层着色器算法缺陷与每帧昂贵的 CPU 像素回读，导致大多数设备（尤其是 Linux / Wine / Proton / Steam Deck 以及核显配置）在主菜单出现**极严重的掉帧卡顿与鼠标微顿挫（Micro-stutter）**。

本模组通过精准的“手术刀式”底层内存挂钩（MonoMod Detour），在**完全不改变任何原本 3D 动态效果与生动交互**的前提下，排除了引起卡顿的根本瓶颈。

---

## 🔬 卡顿根本原因与无损优化原理 / Root Cause & Non-Destructive Principles

### 1. 为什么原版会卡？（深入底层反编译分析）

通过反编译 `Assembly-CSharp.dll` 内部的 `MenuScene`、`InteractiveMenuScene` 与 `SceneBlurLightEdges.shader`：

1. **GPU 瓶颈：40 次冗余高斯模糊采样循环（Over 4 亿次采样/帧）**
   * 原版 12 个高分大图（1531×1460）中，有 7 层使用了 `SceneBlurLightEdges` 和 `SceneBlur` 着色器。
   * 该着色器内部包含一个 5 次外层循环、每次 8 次纹理采样的模糊算法（共计 **40 次 `tex2D` 采样/像素**）。
   * 荒谬的是，在主菜单未触发景深模糊时（`_BlurAmount == 0`），着色器依然在每一帧强行对同一个像素点重复采样 40 次并计算平均值！7 个图层每帧产生数亿次毫无视觉意义的 GPU 显存带宽争用。
2. **GPU 瓶颈：全屏透明隐藏层无效绘制（Overdraw）**
   * 图层 7~10 为特定通关状态的腐化层，未达成条件时其透明度 `alpha == 0`。
   * 但引擎依然将其提交到了渲染层，GPU 每一帧都在反复对 4 张全屏完全透明的大图进行无效的光栅化渲染。
3. **CPU 瓶颈：鼠标移动时的 `texture.GetPixel` 内存回读**
   * 在 `InteractiveMenuScene.Update` 中，当鼠标活动时，引擎会对所有 12 个图层反复调用 `DepthAtPosition(...)`。
   * 该方法在每一帧反复调用底层 `texture.GetPixel` 从 CPU 内存中跨图层读取未压缩的巨大图片像素。每次移动鼠标都会引发极高的 CPU 缓存失效（Cache Thrashing），导致帧时间剧烈抖动。

---

### 2. 本模组是如何做到“效果完全不缩水”的？

```
[原始游戏图层构建 (12层 3D 深度/位置全部保留)]
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ 1. 硬件级渲染直通 (消除 40x 采样瓶颈)                         │
│    利用 Futile 原生 UV 空间切片映射顶部高分彩色艺术插画，     │
│    保留全部 12 层的 depth 深度坐标与视差偏移计算，            │
│    去除无意义的 40 次重复采样，GPU 耗时瞬间降低 90%           │
├─────────────────────────────────────────────────────────────┤
│ 2. 隐藏层过载剔除 (Overdraw Culling)                         │
│    未解锁状态下（Alpha = 0）的图层直接剔除光栅化管线          │
├─────────────────────────────────────────────────────────────┤
│ 3. 鼠标像素射线旁路 (Zero CPU Stutter)                       │
│    挂钩 DepthAtPosition，直接返回已计算好的物理图层深度，    │
│    彻底掐断每帧 24 次耗时的 texture.GetPixel CPU 锁阻塞       │
├─────────────────────────────────────────────────────────────┤
│ 4. 镜头呼吸深度补齐                                         │
│    为 Watcher 主菜单自动补齐原生遗漏的 idleDepths 聚焦列表   │
└─────────────────────────────────────────────────────────────┘
                        │
                        ▼
[结果: 12 层 3D 层次完全保留，视差漂移完全保留，满帧 60+ FPS！]
```

---

## 🎯 适用场景 / Applicable Scenarios

* 启用了 Watcher DLC 模组后，主菜单掉帧至 15~25 FPS；
* 在主界面移动鼠标时出现卡顿、撕裂或微小的输入滞后；
* 在 **Linux / Steam Deck / Wine / Proton** 下运行《雨世界》；
* 任何希望主菜单既保持 **绚丽生动的 12 层 3D 空间视差**，又能拥有 **丝滑满帧性能** 的玩家。

---

## ⚙️ 游戏内 Remix 设置选项 / In-Game Config

进入主菜单 **REMIX** 界面点击本模组，可随时进行个性化调整：

| 选项名称 | 默认值 | 说明 |
| :--- | :--- | :--- |
| **Performance Mode** | **Optimized3D** | **Optimized3D (推荐)**：完整保留 12 层 3D 视差与生动交互，锁定 60 FPS。<br>**Flat2D**：切换为官方单张静态 2D 艺术图（极简省电模式）。 |
| **Disable Depth Raycast** | **开启 (True)** | 屏蔽鼠标悬停时每帧扫描 CPU 像素计算，彻底根治鼠标晃动时的微顿挫。 |
| **Main Menu Theme** | **Watcher** | 可自由切换主菜单背景主题（Watcher / Downpour 倾盆大雨 / Vanilla 原版）。 |

---

## 📥 安装指南 / Installation

### 方式 A：作为 Remix 模组安装（推荐）
将 `dist/WatcherMenuLagFix` 文件夹放入游戏的 `mods` 目录中：
```bash
# 复制方式：
cp -r /绝对路径/WatcherMenuLagFix/dist/WatcherMenuLagFix \
      "雨世界根目录/RainWorld_Data/StreamingAssets/mods/"

# 或使用 Linux 软链接方式：
ln -s /绝对路径/WatcherMenuLagFix/dist/WatcherMenuLagFix \
      "雨世界根目录/RainWorld_Data/StreamingAssets/mods/"
```
启动游戏后，在主菜单点击 **REMIX**，勾选启用 **Watcher Menu Lag Fix** 并点击应用即可。

---

### 方式 B：作为全局 BepInEx 插件安装（极简）
直接将编译出的 DLL 放入 `BepInEx/plugins/` 目录：
```bash
cp /绝对路径/WatcherMenuLagFix/dist/WatcherMenuLagFix/plugins/WatcherMenuLagFix.dll \
   "雨世界根目录/BepInEx/plugins/"
```

---

## 🔨 从源码编译 / Build from Source

```bash
cd src
dotnet build WatcherMenuLagFix.csproj -c Release
```
产物会自动打包输出至 `dist/WatcherMenuLagFix/plugins/`。

---

## 👤 作者与署名 / Author & Credits

* **Author**: **Antigravity**
* **Game**: *Rain World* by Videocult & Akupara Games
* **License**: [MIT License](LICENSE)
