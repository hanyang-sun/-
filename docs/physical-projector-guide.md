# 固定投影仪仿真：第一阶段

适用：Unity 6000.3.24f1、Built-in 渲染管线、`Brick2LifeSimulation`，2026-11-06。

## 当前效果

现有道路、章鱼塔、敌人、攻击和角色纹样仍由原代码生成，但这些面片只供输出图像使用，不再直接显示在积木上。虚拟投影仪从固定位置把这一张图像投到棋盘和积木表面，按表面颜色、纹理、法线方向和距离计算附加反射光。从投影仪视角生成的深度图决定哪些表面被前方物体遮住。

因此，移动投影仪会使图案移动或变形；积木变高会改变图案相对位置；高积木或其他不透明挡光物会在后方形成投影阴影。黑色输出默认不增加光，不会像黑色贴纸一样盖黑积木。相同白光在红、绿积木上产生不同颜色的反射贡献。

搭建窗口继续保持无投影，作为编辑观察视图。顶置 RGB 与展示窗口接收表面投影光；展示窗口沿用已确认布局重建积木，不改变其版本与只读规则。确认前，预览积木与源场景真实几何可能短暂不同；真正用于 B 识别的图像始终来自源场景。仅投影模式保留为平面诊断显示，隐藏积木后不用于验证实物遮挡。

## 使用与调节

重新进入 Play 即自动启用，无需重新运行安装菜单。选择 Hierarchy 中的 `SimulationFeedback`，查看 `Simulation Projected Light` 组件：

| 参数 | 默认值 | 含义 |
| --- | --- | --- |
| Projector Position | `(5,14,5)` | 世界坐标中的镜头位置 |
| Projector Angles | `(90,0,0)` | 默认垂直向下；改变后产生斜向投影 |
| Field Of View | 约 `46.4°` | 输出覆盖范围，默认棋盘平面约 12×12 单位 |
| Output Resolution | `512` | 正方形输出及遮挡图分辨率 |
| Light Strength | `1.3` | 投影光相对强度，过强会产生饱和 |
| Black Level | `0` | 理想黑场；可调非零值模拟残余光，未按实机校准 |
| Blur Pixels | `0.5` | 图像的近似模糊半径，不是已标定镜头模型 |
| Shadow Bias | `0.025` | 避免表面自身误遮挡的世界距离容差 |

Play 中的参数调整用于实验，退出 Play 后 Unity 不自动保存这些运行时改动。若要保存默认参数，可在退出 Play 后为 `SimulationFeedback` 添加同名组件并设置，运行时会复用它；安装菜单也会添加该组件。

图像以棋盘平面为基准，没有使用真实积木高度做投影校正，因此高度导致的偏移会保留。当前单层识别合同没有改变；用于验证光路的高物体不表示已完成多层识别。

## RGB 与消隐边界

31 层现在是“投影内容源”，只在输出图像生成期间绘制。观察相机即使排除该层，也不能消除已经照在表面上的光。F8 的干净/带投影采样由统一门控制真实投影贡献，恢复后继续投射；搭建窗口始终保持干净视图。

稳定布局每 0.8 秒检查、可用待确认候选每 0.12 秒补足三帧的策略保留；一次消隐至少一帧、0.01 秒。它们仍是 Unity 等待参数，不是硬件同步结论。几何深度只用于生成相机画面，不向 `FixedGridRecognizer` 提供积木占用、颜色或 ID。

## 文件与调用链

```text
确认布局 / 战斗状态
  → ProjectionSimulator + SimulationBattleOverlay（内容源）
  → VirtualProjectorContent Camera → RenderTexture（输出图像）
  → SimulationProjectedLight（固定透视、接收物与遮挡深度）
  → SurfaceLight.shader（表面反射光贡献）
  → TopCamera RGB / SimulationProjectionPreview
  → 原有 B 识别与确认流程
```

- `Assets/Scripts/Feedback/SimulationProjectedLight.cs`：输出图像、投影参数、每个观察场景的遮挡与绘制、相机和纹理释放。
- `Assets/Resources/ProjectedLight/ProjectorDepth.shader`、`SurfaceLight.shader`：投影仪深度、表面反射、近似衰减和模糊；放在 Resources 以确保 Player 不因未引用而剔除。
- `ProjectionSimulator.cs`、`SimulationBattleOverlay.cs`：复用原图案与动画，源 Renderer 默认不直接绘制。
- `Assets/Editor/SimulationProjectionPreview.cs`：复用输出与镜头参数，在自己的积木场景中计算遮挡；不再复制投影面片。
- `SimulationFeedbackSetup.cs`：安装新组件，并移除原安装流程对不存在的 `renderGameObjects` 投影字段的引用。

旧额外三维角色组件和五个样式材质已删除，场景及安装菜单也已移除引用。预览不再保留战斗模型/路线复制分支或空投影节点；战斗门面只提供实际使用的计数、刷新和入口标记命令。

## 验证与限制

运行：

```powershell
pwsh -File Tests/RunSimulationDualWindowChecks.ps1 -CheckClass PhysicalProjectorChecks
pwsh -File Tests/RunSimulationDualWindowChecks.ps1 -CheckClass LiveProjectionRefreshChecks
pwsh -File Tests/RunSimulationDualWindowChecks.ps1 -CheckClass ProjectionBlankingSyncChecks
pwsh -File Tests/RunLogicChecks.ps1
```

图像检查覆盖黑场、残余光、颜色反射、投影位移、高度偏移、斜向遮挡、相机层过滤不能消除表面光、无投影搭建视图、预览不再贴面片、观察视角独立、消隐恢复、停用清理，以及真实 RGB 的干净识别和动态战斗输出。验证 PNG/JSON 生成在本机 `.scratch/dual-window/`，不提交大体积截图。

物理投影首次验证：图像 **26 项**、实时 RGB/战斗更新 **146 项**、双窗口消隐与恢复 **175 项**、纯逻辑 **1195 项**通过。清理后安装/图像检查 **28 项**、实时 RGB/战斗 **146 项**、双窗口消隐 **207 项**、纯逻辑 **1195 项**通过，包含安装菜单不会复生旧节点、无缺失组件。旧构造式对齐检查接口已删除，实际投影光斑的偏移从图像测量。

这是直接光照的可控近似：颜色尚未进行实机辐射校准，Gamma 管线的亮度叠加不能视为精确光度学结果；没有多次反射、镜头畸变、真实失焦、设备响应曲线或曝光积分。输出暂为固定方形、单投影仪。硬件时序和多层识别仍须后续分别实现。
