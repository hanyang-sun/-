# 固定投影仪、表面反射与遮挡（第一阶段）

日期：2026-11-06。用户批准让仿真更接近真实投影。首阶段范围为：复用现有动画生成一张输出图像，按固定透视投影仪的光路投到真实场景表面，以表面颜色调制投影光，并加入投影方向的遮挡。保留无投影搭建视图、自动 RGB 预测、消隐节奏和玩法。硬件输出延迟、曝光积分及多层识别属于下一阶段，不在本轮伪造硬件参数。

## 编码前检索与比较

检索关键词：`Unity texture projector depth occlusion shader built in`、`ProjectorForLWRP`、`DepthInverseProjection`。通过 GitHub 实际读取：

| 候选 | 阅读文件与版本 | 比较与选择 |
| --- | --- | --- |
| [nyahoon-games/ProjectorForLWRP](https://github.com/nyahoon-games/ProjectorForLWRP) | README、MIT LICENSE、`SRP/Scripts/ProjectorForSRP.cs`、`Shaders/ProjectorLight.shader`、`Shaders/P4LWRP.cginc`；树 `97d226b297aeaaef7d1ad611a25575e36085ba03` | 采用“世界位置映射为投影纹理坐标、按相机过滤接收物、额外绘制投影贡献”的机制。该包验证 URP 7.1.7，本项目使用 Built-in，不引入旧 SRP 依赖。其普通投影不能直接替代本轮需要的遮挡判断。 |
| [keijiro/DepthInverseProjection](https://github.com/keijiro/DepthInverseProjection) | README（声明 Public domain，无独立 LICENSE 文件）、`Assets/InverseProjection/InverseProjection.cs`、`Assets/InverseProjection/Resources/InverseProjection.shader`；树 `6b6d72d8178dbecdc29b037a175b95ea364bdbaa` | 读取其深度、投影矩阵和正交/透视处理。采用坐标与深度必须一致的原则，不复制其反投影代码，不引入 PostProcessing 包。此例是视角反投影，不能直接提供投影仪输出与阴影。 |
| [mikebru/Unity-Projector](https://github.com/mikebru/Unity-Projector) | README、Unlicense LICENSE、完整目录树 `47b2244ed92425d1ae4cf7156f70994580e9581e` | README 明确遮挡与阴影仍待实现；实现仅以 12 MB 二进制 unitypackage 提供，没有读取包中源代码，因此不将其作为已审查可复用实现，不引入。 |

`nyahoon-games/DynamicShadowProjector` 的 GitHub 目录查询返回 404，未假定其代码可用。以上均只作机制参考，不复制、改写第三方源码或素材，不新增包。

## 接线与验收

- 现有 `ProjectionSimulator` 与 `SimulationBattleOverlay` 成为内容源，先由只拍投影层的正交相机汇成黑底 RenderTexture；场景相机不再直接显示这些图案面片。
- 新 `SimulationProjectedLight` 使用固定位置、方向、视场角，把图像透视映射到不透明表面。先以棋盘平面建立输出，积木高度引起的偏移真实保留，不用真实布局自动校正输出。
- 从同一投影仪视角绘制接收场景深度，光只照到沿光路最近的表面；表面自身法线、颜色、纹理调制附加光照。黑像素不盖黑场景。
- 预览复用相同输出和投影仪参数，但对预览场景自身几何重新计算遮挡；改变观察视角不移动投影仪。搭建相机不接收投影贡献。
- 消隐门同时关闭投影光贡献与源图案；采集异常恢复、F8 对照和自适应间隔继续使用原接口。场景几何仅供成像，B 仍只接收 RGB/公开标定。
- 必须验证：移动投影仪使图案移动、积木高度导致偏移、不同颜色表面呈现不同结果、投影方向遮挡、黑场不盖黑、搭建视角不变、真实 RGB 干扰与干净采样、预览一致、取消/禁用与资源释放。
- 这是直接光照近似，不包含多次反射、设备响应曲线、真实镜头失焦或硬件时序；不将其声称为完整物理光学模拟。

## 实现与验收结果

- 复用上述机制，自行实现 Built-in 额外表面绘制与投影仪深度图；未复制或改写候选源码，不增加第三方依赖。Direct3D 纹理纵轴差异已通过斜向遮挡图像回归修正。
- `PhysicalProjectorChecks`：**26 项通过**，驱动正式相机取样与预览，包含可调黑场残余光、未饱和白光下的颜色反射、投影位移、高度位移、斜向遮挡、观察相机排除内容层仍接收表面光、搭建视图不变、禁用后的命令缓冲释放、干净 RGB 100/100 及实际战斗输出。
- 白光非饱和测试：红积木红通道增量约 0.320、蓝通道约 0.090；绿积木绿通道约 0.331、红通道约 0.095。这里是归一化相机通道增量，不是实机反射率或精确光度学参数。
- 斜向蓝光测试：未遮挡目标通道增量约 0.094，放置高积木后同点增量 0；旁边仍受光的控制点增量约 0.183。高度改变时边界采样增量变化约 0.525。图像证据在本机 `.scratch/dual-window/run-20261106-physical-projector/`。
- `LiveProjectionRefreshChecks`：**146 项通过**，持续出怪的局部脱离与新路径、墙塔增删移动、暂停/波次/胜利后更新，以及实际投影道路继续接入。
- `ProjectionBlankingSyncChecks`：本次 **175 项通过**，双窗口消隐、F8、停止/禁用、相机异常与恢复继续正常。
- `RunLogicChecks.ps1`：纯 C# 全量 **1195 项通过**。
- 顺带修正新安装流程会遇到的无效序列化字段引用，以及内容材质清理误销毁 Unity 内置白纹理的问题。
- 原构造式对齐检查只测量内容放置，并非真实投影足迹；后续清理已删除该未被调用的接口，改用渲染图像验收。

## 替换后的旧代码清理

- 删除 `SimulationLevelView.cs` 及 `.meta`：额外三维角色生成、样式与对象缓存整套退役；同时删除适配器、安装菜单和场景中的接线，以及空的 `Level` 节点。
- 删除五个无引用的材质及对应 `.meta`：`Battle_Enemy`、`Battle_Route`、`Game_Core`、`Game_Wall`、`Game_Tower`。合计删除 **12 个文件**。
- 预览只保留确认积木、棋盘与系统目标提示；删除战斗模型与路线镜像、缓存字典和空 `Projection/Battle` 根节点。投影可见性直接表达为状态，不用空 GameObject 表示。
- 战斗门面删除旧材质/尺寸等序列化字段、空 `Render/ClearEnemies/ClearRoutes` 方法及调用；刷新入口改为无冗余参数的 `RefreshLayout`，只向所属 Overlay 转发。
- 删除只追加从不读取的敌人位置列表、未使用箭头池、投影门旧单对象入口、未调用的内容位置测量接口及旧相机警告接线。
- 内容源 Renderer 已只在输出烘焙期间启用，因此删除新投影器中重复的相机遮罩修改、恢复状态和 `onPostRender` 监听；不会重新启用旧贴面路径。
- 保留仍用于生成输出图像的角色纹样与动画。它们有实际调用，不属于旧投影旁路。
- 使用全新的隔离工程运行，避免旧验证目录残留已删除文件：安装及物理成像 **28 项**、自动 RGB/战斗更新 **146 项**、双窗口消隐/异常恢复 **207 项**、纯逻辑 **1195 项**通过。再次检索 `Assets/Tests`，退役符号和已删除材质的 GUID 无剩余引用。
