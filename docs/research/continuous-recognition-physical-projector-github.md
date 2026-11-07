# 连续投影识别与 C 成像整合

## 当前两种模式（2026-11-06 已实现）

用户明确仅保留：连续模式直接识别 C 的带投影 RGB；暂停识别模式按 Brick2Life 的暂停/继续操作关闭与恢复投影。现有周期性消隐模式、其定时/跨帧采样 API 和旧消隐频率测试退役，合成图只作纯数学测试 fixture。

编码前复核同仓库 `9ef432bf240760aac79b6d6f714f061612a1e497` 的 `SimulationVisionMonitor.Update/RefreshOnceWithoutProjection/ShowPreparedProjection`（GitHub blob `2a27c69862a0e46570a2288b8f6dca87bcfdee92`）及本地同提交的 `SimulationBattleController.SetPaused`，比较 C 当前的自动周期性消隐管线。原实现与需求最直接匹配，按用户授权复用同仓库操作机制；不引入其他包/第三方实现。下方历史比较中的两个外部候选仍仅作光度/映射机制参考，不复制其代码。

适配：保持 C 真实 RGB、光度补偿与投影光路。暂停期间统一门持续关闭展示光，使用各自新采集的 RGB 确认；继续/开战前沿用控制器现有的新帧等待机制，确认后恢复，不复制原分支同图反复确认的循环。准备阶段也允许通过暂停按钮采集。切换、取消、禁用、重试和采集异常须释放门；继续等待失败时保留战斗暂停并恢复显示。下方“历史整合阶段”之后保留旧方案和周期性消隐验证，不再作为运行配置。当前结果见 [验证摘要](../verification/current-simulation.md)。

已完成：删除周期性消隐字段/四个跨帧采样 API、场景旧参数、两份旧消隐测试及 runner 接线。连续识别检查合并为 `LiveProjectionRefreshChecks`，新增 `PauseRecognitionChecks` 替代原对照。最终实跑：连续 462 项（307 帧不消隐）、暂停模式 103 项、物理成像 75 项、纯逻辑 1216 项通过；45 点最大误差 0.01348 格。本机证据 `.scratch/dual-window/run-20261106-two-modes/`。

## 历史整合阶段（两种模式收敛前）

日期：2026-11-06。用户要求保留 Brick2Life 的识别算法，投影采用 Brick2Life-C。目标目录为 Brick2Life-C，保留真实投影及几何校正、寻路、自适应采样；原 Brick2Life 不修改。

## 编码前比较

关键词：`projector photometric compensation camera`、`Unity projector projection matrix`。已读取 README/架构/开发计划，复核原分支的调用链；通过 GitHub 实际读取以下代码与资料：

| 候选 | 阅读范围及授权 | 采用方式 |
| --- | --- | --- |
| [本项目连续识别分支](https://github.com/hanyang-sun/Brick2life/tree/9ef432bf240760aac79b6d6f714f061612a1e497) | GitHub `ProjectedLightRecognizer.cs` blob `77685440abb52ca3ac9a65cda36240287423d847`，本地同提交的 PhotometricProfile/CellProjection/Srgb/PhotometricComposer/测试及接线 | 用户授权整合同仓库实现。复用光度逆运算、归一化 RGB 分类、饱和/曝光/不稳定拒绝。删除旧的离屏干净图再合成输入旁路；合成器仅迁入纯测试 fixture。 |
| [nyahoon-games/ProjectorForLWRP](https://github.com/nyahoon-games/ProjectorForLWRP) | 再读 `Shaders/P4LWRP.cginc`，MIT；前期研究已读 README/LICENSE | 沿用公开世界点映射输出纹理的机制。不引入 SRP 依赖或复制第三方源码。 |
| [BingyaoHuang/CompenNet](https://github.com/BingyaoHuang/CompenNet) | 再读 README 与 LICENSE：学术/非营利非商业研究限制；已有研究比较其 PyTorch/GPU 管线 | 只参考观测图像必须与投影输出对齐、需要光度标定的原则；不复制代码、不引入依赖。其目标是投影外观补偿，不直接替换颜色识别。 |

## 接线与边界

- C 的实际顶置 RGB → 已曝光输出纹理/公开投影位姿与单层采样平面 → 原光度恢复与分类 → 原连续独立帧确认 → C 关卡和投影。预测不读取 JSON、占用、真实颜色、ID 或遮挡深度。
- 投影命令从输出图像逐采样点获取，覆盖动态纹样；不再从 ProjectionPlan 按格估计，也不读取接收物材质。恢复模型只是近似，源深度图不交给识别器。
- C 当前 Gamma 管线直接叠加颜色，不改其 shader 外观。光度模型增加响应指数，默认保留原 2.2，C 适配用 1。环境响应由独立已知灰参考面标定，不是实际玩家布局的干净图；缓存按相机/光照配置失效。实机保留原 Fit/曝光合同。
- F8/界面按钮切换“连续投影补偿 / 消隐采样”，默认连续；消隐对照继续按独立 RGB 帧确认，不迁入原分支同图重复确认、暂停手动刷新的旁路，不新增长期双写或旧三维角色代码。
- 测试必须运行真实 C 渲染图，覆盖连续模式不消隐、加删移动、错误投影指令影响预测、饱和未知、干净采样回归、几何对齐和遮挡。合成图只验证数学算法，不作为实际投影准确率证据。

## 整合适配

- 原有固定中心 3×3 样本保留，增加同一 RGB 的桌面假设来判断空格；只比较图像证据的置信度，不读取真实高度或占用。
- 增加每个样本的照明系数，取自实际输出纹理、公开投影几何、已知灰参考面的环境响应。C 的 `CellProjection` 信号是按 profile gain=8 归一化的总照明，另保留环境分量；实机原逐格指令 API 兼容。
- 阴影下中性棋盘的环境光衰减从 RGB 验证，避免用未遮挡灰参考直接除法导致旧塔格位持续未知。不把实际场景深度传入识别。
- 为避免在 8 位目标中先过曝后无法恢复，识别相机运行时使用 ARGBHalf，并在最终图像统一施加锁定曝光 0.3。展示相机光强/材质不变；固定相机预览与识别使用同一曝光图。导出记录 exposureScale，退出恢复原纹理格式/HDR。
- 不迁入旧 `CaptureAlbedoForMixing`、运行时合成器、`ICellProjectionSource`/ProjectionPlan 照明估计、暂停重复同图确认或旧 UI 排版。合成器与已有数学检查只放 Tests；已删除迁入测试中不再使用的关卡照明 fixture。

## 结果

纯逻辑 1216 项、连续实际成像/玩法 578 项（423 个观察帧未消隐）、消隐对照玩法 146 项、双窗口消隐恢复 50 项、物理成像 75 项通过。连续场景八个确认边界均为 100/100；不声称任意实际照片都达到此精度。几何 45 点最大误差 0.01348 格。证据在 `.scratch/dual-window/run-20261106-integrated-recognition/`。

迁入的是同仓库算法，自行适配采样与响应；没有复制外部源码或添加外部包。原工程保持原样。实机曝光、光度标定、时序和复杂遮挡准确率仍需后续验证。
