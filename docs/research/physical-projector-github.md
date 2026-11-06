# 固定投影仪、表面反射与遮挡（第一阶段）

> 阶段研究记录：以下选择、参数和结果保留原日期条件，部分实现已替代。当前运行行为以 [现行文档](../README.md) 为准；版本关系见 [研究索引](README.md)。

## 2026-11-06：几何校正的编码前增补

用户批准采用投影仪视角烘焙，保留光学近似、干净搭建视图、识别与消隐节奏。验收以实际 RGB 光斑中心为准，目标误差不超过 0.02 格，覆盖中心、四角、多个显式指定高度及斜投；高物体仍遮挡，校正不能读取真值来定位内容。

沿用下表已审查的两个候选，再次通过 GitHub 读取 `nyahoon-games/ProjectorForLWRP/Shaders/P4LWRP.cginc`（blob `bcd5e168eaeb1b46b97eeef34338cd0ab9d04208`）以及 `keijiro/DepthInverseProjection/Assets/InverseProjection/InverseProjection.cs`（`ef45aa1ce331ffefa15984df2a883de193b8f424`）和对应 shader（`439dbeb57072c0c541842cb7df2b758d4f5fbe0a`）。关键词沿用 `Unity projector perspective projection matrix`。前者 MIT，世界坐标经投影矩阵采样纹理；后者 README 声明 Public domain、无独立 LICENSE，明确区分正交/透视和相机矩阵。二者均不是本项目的成套几何校正模块，且分别有 SRP/PostProcessing 依赖，不直接集成。

仅采用“生成和回投必须共用投影坐标”的机制，自行修改现有内容相机，并从其实际 view/projection 矩阵构造回投和遮挡矩阵，避免两套参数漂移。删除旧正交烘焙配置、面片高度补偿字段与接线；内容按已确认布局的单层表面放置，透明图层用排序代替悬空。不新增长期旧模式分支、包或第三方源码。高度测试仅为显式光学 fixture，不代表识别能感知多层。固定校准图片仍是未随场景预畸变的幻灯片，应保留其高度偏移和移动投影仪时的图像位移测试。

本轮结果：原地修改相机、内容高度、排序及其安装/场景字段，未新增产品文件或依赖。真实 RGB 光斑 45 点最大中心误差 0.01246 格，物理成像 75 项、实时 RGB/战斗 146 项、双窗口消隐 35 项（按观察帧数计数）、纯逻辑 1195 项通过。测试定位并修复停用根 Renderer 仍进入接收物/遮挡列表的幽灵阴影，增加停用后光照恢复回归。具体 fixture 与实机边界见 [贴合说明](../projection-alignment-guide.md)。

以下为校正前的第一阶段历史记录。

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
