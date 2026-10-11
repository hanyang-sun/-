# BrickDefenseMVP

实体积木塔防项目：模拟布局或单张照片 → 10×10 布局 → 20×20 游戏格 → Unity 三维布局与寻路预览。照片有两种入口：带标记格纸恢复固定格位；无格纸模式依据积木尺寸生成相对布局。两种模式都由用户标定颜色角色，通用实拍准确率尚未验收。

## 运行演示

1. 用 **Unity 6000.3.24f1** 打开本目录。
2. 打开 `Assets/Scenes/BrickDefenseDemo.unity`，点击 Play。
3. 点击地面游戏格选择出生点；用底部按钮切换“需求示例布局”和“核心被包围布局”。

默认出生点是 `(1,18)`。需求示例应显示 9 块积木、36 个占用游戏格、到实体格 `(7,7)` 核心的 16 步路线。核心被包围布局应显示无可达路线的提示（结果代码 `NO_ROUTE`）。柔和蓝绿代表核心、暖石色代表墙、赭黄代表塔，深绿标出路线和有效出生点，砖红标出无效出生点。

## 柔和策略界面

地图采用暖色桌面、鼠尾草绿棋盘与圆角浮动卡片。底部 `Reference layout` / `Enclosed core` 切换示例，`Scan your bricks` 打开照片页；右上角显示路线状态、积木数量和出生点。相机根据窗口比例适应棋盘宽度。

### 视角、缩放与手机操作

默认采用 **55°斜俯视的正交桌游视角**，能看到积木顶面和侧面；远处和近处的同尺寸积木显示大小一致。俯仰限制为 45°—75°，旋转与缩放时始终保持正交投影。

| 操作 | 电脑 | 手机 |
| --- | --- | --- |
| 选择出生点 | 左键点击空格 | 单指轻点空格，抬起时确认 |
| 旋转与俯仰 | 按住右键拖动 | 单指拖动 |
| 放大、缩小 | 地图上滚动鼠标滚轮 | 双指张开、收拢 |
| 按钮控制 | 点击 | 轻点 |

右上角 `Left` / `Right` 每次转 45°，`Lower` / `Higher` 调整俯仰，`-` / `+` 缩放，标题显示当前倍率。缩放范围 **65%—250%**，围绕棋盘中心；放大用于查看局部，`Reset` 同时恢复默认角度和 **100% 全图**。旋转后或缩放后继续按实际地图位置选格。

拖动和捏合不会误选出生点；从 UI 开始的手势不操作地图，照片页暂停地图相机。捏合后需要全部抬起，再开始单指旋转或轻点。保留触摸模拟鼠标供现有按钮和照片标定使用，地图屏蔽重复输入。手机手势已接入当前 Legacy Input；编辑器检查不等于真机验收，本机仍缺少 Android 构建模块。

轨道计算局部改编自 MIT 授权的 JrsOrbitCamera，见 [检索比较](docs/research/orbit-camera-github.md) 及 [第三方声明](THIRD_PARTY_NOTICES.md)。

捏合距离计算继续有限改编同一 MIT 来源；新增手势生命周期与输入隔离见 [手机手势与缩放研究](docs/research/touch-zoom-github.md) 和 [.scratch/touch-zoom/spec.md](.scratch/touch-zoom/spec.md)。

照片页按 Your photograph → Role colors → Check the map 整理操作，宽屏双列，窄屏单列滚动。地图角色材质与实际积木颜色相互独立；原照片、标定色块、检测框和数字地图仍展示实际颜色。

配色与控件样式集中在 `Assets/Scripts/Presentation/SoftStrategyTheme.cs`，三维材质、光照和底座在 `LevelView.cs`，轨道与缩放在 `BoardOrbitCamera.cs`，手机手势在 `BoardTouchControls.cs`，地图绘制与触摸共用的 UI 区域在 `BoardUiLayout.cs`。GitHub 源码比较与有限借鉴见 [界面研究记录](docs/research/soft-strategy-ui-github.md)，实现与验证见 [.scratch/soft-strategy-ui/spec.md](.scratch/soft-strategy-ui/spec.md)。

## 使用真实照片

### 带标记格纸：固定格位

照片面板选择 `Board paper`。原有流程如下：

1. **制作格纸：**Unity 菜单 `Brick Defense/Export Scan Board`，填写实测积木边长（毫米），导出 SVG。使用足够大的纸张按 **100% 原尺寸**打印，关闭“适应纸张”；纸面边长是积木边长的 14 倍，核对每格尺寸等于积木边长。纸面需平整，外侧标记不能被积木遮挡。
2. **摆放和拍照：**一格一块、单层、边缘与格线平行，竖直俯拍。将全部格纸及四角黑白标记拍入，保留余量。四角 ID 为 125/248/511/742，125 所在角指定左上角。识别器使用这套原版 js-aruco 编码，不能混用其他 ArUco 字典或无标记格纸。
3. **导入照片：**Play 后点击 `Scan your bricks` → `Import photo`。编辑器弹出选图窗口；桌面独立程序可填本地文件路径。也可选择 `Camera` → `Take photo`。Android 拍摄路径已接入相机权限申请，但本机未安装 Android 构建模块，尚未真机验证；Android 相册选择器未接入。
4. **首次标定：**照片中应能看到空白纸面和三种积木颜色。点击 `Color setup`，分别选择 Paper/Core color/Wall color/Tower color，再点照片中对应的平坦颜色区域，最后 `Save colors`。配置保存供以后扫描使用，不需要每局重新分配角色；换一套积木或明显改变光照时重新标定。颜色不可区分时不会允许分析。
5. **识别与核对：**点击 `Analyze photo`，查看只读 10×10 数字地图：K 核心、W 墙、T 塔、`.` 空格、`?` 不确定。标记缺失、投影异常或颜色混杂时显示拒绝原因和相关格位。发现识别错误请重拍。
6. **生成关卡：**扫描成功且包含核心后，点击 `Use this map`，返回三维地图并选择出生点。失败扫描不会覆盖当前三维地图。

### 无格纸实拍：相对布局

适用于本次上传的灰黑背景照片：同尺寸立方体、单层、近似俯拍、背景与积木可区分，并至少有一个清晰独立方块用于估计尺寸。

1. 点击 `Scan your bricks`，选择 `Plain background`，再导入 PNG/JPG 或拍照。
2. 点击 `Color setup` → `Sample Background`，在干净背景处采样；再分别选择 Core、Wall、Tower 并从对应积木顶面采样。颜色与角色由你决定。
3. `Save colors` 后点击 `Analyze photo`。查看照片中的逐块检测框及 10×10 数字地图；同色连排、L 形或 2×2 紧贴方块按尺寸和实际颜色覆盖拆成独立积木。
4. 核对数量、角色和相对位置后确认生成三维关卡。未知颜色、裁切、形状无法可靠拆分、相对格冲突或超界时拒绝，不覆盖当前地图。

失败时，照片中以红框标出能定位的裁切、未知颜色、几何或拆分问题；报告中也保存原图像素坐标。照片面板使用不透明背景，三维地图不会透过面板遮住控件。

这里没有真实格纸原点。结果按照片左上方向，以检测到的方块边长估计间距，最上/最左的方块放在相对行/列 1，保留边缘空位；空白格和间距是离散近似，不是恢复了实体格纸。报告中的 `coordinateBasis=RelativeBrickScale` 和 `boardIsReliable=false` 明确这一点。需要固定格位或重复拍摄的固定世界坐标时使用 `Board paper`。

两种模式分别保存标定，切换模式不会覆盖原格纸标定。无格纸灰背景与白色积木通过明度区别，彩色积木仍复用既有颜色描述。严重斜拍、旋转、堆叠、遮挡、复杂背景或缺少可估尺寸的方块尚不属于该入口的支持范围。
原实拍回归样本为 `Tests/Fixtures/real-unmarked-bricks-v1.jpg`，保持上传原文件不变；本次回归检测到 7 橙、2 蓝、1 白，共 10 块、90 个相对空格。它们的游戏角色取决于标定顺序，测试中的角色安排不是程序预设。

2026-10-09 的拒绝修复使用扫描记录中用户实际保存的颜色，而非重新挑选较合适的样本：完整 `1791530226265.jpg` 在暗背景标定 `(65,61,63)` 下也识别到 10 块。低饱和背景的亮度变化结合已标定低饱和积木的亮度判断，避免把亮灰背景误报为白色积木或未知前景。`qq_pic_merged_1791536977046.jpg` 本身截断了边缘积木，继续拒绝；请导入完整原图，不用裁切版代替。

### 通用输入与记录

照片输入支持 PNG/JPG，文件不超过 32 MB、长边不超过 4096 像素。游戏识别过程在本地完成，不上传照片。扫描原图、归一方向后的照片、颜色样本、格位/积木/证据、错误和处理时间保存在 `Application.persistentDataPath/Scans/<scanId>/`，另记录输入模式、坐标依据和方向，便于后续人工标注与实拍评价。

开发阈值在 `Assets/Resources/photo-scan-settings.json`。格纸模式的颜色先归一化明暗，再用局部适配的 Colourful Lab 转换比较；白纸阴影不会仅因变暗而偏离纸面颜色。该模式中心采样要求 88% 同色，外围检查第二种颜色、未知颜色和覆盖不足；近黑区域不提供有效颜色证据，仅靠明暗区分的白与灰不能可靠区分。无格纸模式针对低饱和颜色额外保留明度，检测区域检查实际覆盖与几何拆分。
置信字段分别表示中心颜色一致比例或检测框颜色覆盖比例，都不是经过实测校准的成功概率；实物阴影、反光、模糊和顶面视差的通用容差仍需更多实拍验证。波次和战斗尚未实现。

### 暂无实物时检查接入

`Tests/Fixtures/synthetic-reference.png` 是明确标注的**合成测试图，不是实拍照片**。可导入该图，采样白纸、红色核心、蓝色墙和绿色塔，检查数字地图和三维生成流程。Unity 菜单 `Brick Defense/Verify Photo Integration` 可运行 PNG 解码到关卡的自动检查。

`Tests/Fixtures/generated-overhead-bricks-v1.png` 是带阴影和积木侧面的 **AI 生成俯拍图**。本次颜色回归固定纸面 `(720,170)`、红色 `(808,391)`、蓝色 `(487,385)`、绿色 `(650,636)` 的 7×7 中位数标定，修改后确认 6 蓝、2 红、1 绿及 91 空格。旧版本只识别出 5 块、9 格不确定。更新代码后导入原图即可重试；如原标定不合适，进入 `Color setup` 重新点选平坦区域。

## 逻辑检查

安装 .NET 10 SDK 后，在项目根目录运行：

```powershell
dotnet run --project Tests/BrickDefense.Checks.csproj
```

检查覆盖示例数量、2×2 格映射、四方向最短路线、等距核心的固定选择、封闭核心、被占用出生点、同色相邻积木的独立编号，以及不确定格位、未映射颜色、畸形网格和缺少核心的拒绝。另用独立 BFS 对照 200 个固定随机种子的布局，核对最短距离、目标选择和路径通行性。

照片检查从独立生成的 RGB 像素输入验证完整扫描与关卡连接，并覆盖 90/180/270 度旋转、透视、同色相邻、缺标记、未知颜色、混色和未标定。重建合成 PNG 可运行 `dotnet run --project Tests/BrickDefense.Checks.csproj -- --write-photo-fixture`。

颜色回归还读取原生成图的无损 RGB 数据，验证每块的颜色与位置、91 个空格及 16 步路线，并检查明暗渐变、纸面阴影、近黑格、仅在边缘出现的混色和未知色。Unity 菜单检查直接解码原 PNG 并使用基础设施模块的设置加载器。

流程检查覆盖照片与两个示例反复切换、非法输入保留当前关卡、输入数据快照、出生点变更，以及确认时使用分析当时的标定规则。
Unity Play 回归通过公开关卡接口验证实际 Renderer 和相机渲染，并检查程序集边界、原 PNG 导入、标定读写及扫描报告保存。

无格纸检查读取原实拍 JPG 的解码 RGB，核对 10 块颜色与人工标注位置，并覆盖缩放、用户互换角色、L 形/2×2 同色紧贴、未知色、裁切、空背景和模式切换失效。Unity 回归直接解码 JPG，验证确认、报告原文件与三维显示。

拒绝回归额外覆盖用户原标定、灰背景明暗渐变、真正裁切 JPG、拒绝区域原图坐标和当前关卡保留。`--scan-rejection-checks` 可单独运行。Unity 场景回归还验证拒绝区域 JSON 保存；隔离项目的 Game 视图截图验证面板遮挡，详见 `.scratch/scan-rejection/spec.md`。

## 代码位置

整体数据流程、模块职责与程序集依赖见 [架构说明](docs/architecture.md)。五个主要模块已建立独立 `.asmdef`；Domain、Vision、Application 以及公共数据和算法程序集不引用 Unity API。

- `Assets/Scripts/Contracts/`：共享布局、规则、格位、结果和照片证据数据。
- `Assets/Scripts/Domain/`：纯 C# 的模拟扫描、格位映射、关卡校验和 A* 寻路。
- `Assets/Scripts/Vision/`：纯 C# 的照片数据、格纸定位、投影映射、颜色识别与格纸模板。
- `Assets/Scripts/Vision/PlainBackgroundRecognizer.cs`：无标记背景分割、同色组件拆分与相对布局。
- `Assets/Scripts/Application/`：`LevelSession` 集中管理关卡上下文；`PhotoScanSession` 管理分析状态和确认规则。
- `Assets/Scripts/Presentation/`：场景接线、三维绘制、示例交互和照片界面。
- `Assets/Scripts/Infrastructure/`：图片读取、相机、Unity 像素转换、标定与扫描报告持久化。
- `Assets/ThirdParty/RoyTAStar/`：固定版本的 Roy-T.AStar 最小堆与完整许可证。
- `Assets/ThirdParty/JsAruco/`：局部改编的标记编码/解码与完整授权声明。
- `Assets/ThirdParty/Colourful/`：局部改编的 sRGB/D65 Lab 转换与完整 MIT 授权。
- `Assets/Editor/`：Editor 专用的场景生成、文件选择器和回归工具；重建场景菜单为 `Brick Defense/Create Demo Scene`。
- `docs/requirements-v1.md`：本项目首版功能和接口依据。

模拟扫描中的 `demo-core`、`demo-wall`、`demo-tower` 只是示例标识，不能当作实物颜色标定结果。照片识别报告独立的 `physical-color-*` 颜色编号，再由保存的配置映射为角色。

## GitHub 借鉴依据

本里程碑的检索、候选比较和有限整合方案见 [研究记录](docs/research/existing-milestone-github.md)。直接采用的源码范围与授权见 [第三方声明](THIRD_PARTY_NOTICES.md)。功能开发须先按 [项目流程](docs/agents/github-reuse.md) 完成检索记录。

照片识别的候选比较、源码选择和验证依据见 [照片识别研究记录](docs/research/photo-recognition-github.md)。

颜色识别改进的前置检索和实际适配见 [颜色识别研究记录](docs/research/color-recognition-github.md)。

示例在照片关卡之后只显示底板的问题已修复：切换到任一模拟布局时恢复示例的配套颜色规则。
Play Mode 回归覆盖初始示例、照片、两个示例按钮的加载入口、重复切换、实际 Renderer 与相机渲染。
普通示例显示 9 块和 16 步路线；包围示例仍显示 5 块布局，并以 `NO_ROUTE` 提示不可达。
前置比较及复现依据见 [示例切换研究记录](docs/research/demo-switching-github.md)。

五模块重构已完成。`BrickDefenseDemo` 负责场景接线，`LevelView` 负责绘制，`DemoUI` 负责操作；布局、颜色规则与计算结果经 `LevelSession` 整体替换。
照片面板的文件读取、相机和存储职责已移入 Infrastructure。标定保存键、报告格式与场景脚本 GUID 保留。
GitHub 候选比较和有限适配见 [架构重构研究记录](docs/research/architecture-refactor-github.md)。

无格纸实拍的前置检索与有限借鉴见 [无标记实拍研究](docs/research/markerless-photo-github.md)，输入与坐标扩展规范见 [v1.1 扩展](docs/requirements-markerless-v1.1.md)。

实拍拒绝与面板遮挡修复的源码比较、有限借鉴和验证见 [拒绝修复研究](docs/research/scan-rejection-github.md)。
