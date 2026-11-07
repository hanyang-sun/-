# 减少稳定布局的投影消隐频率

> 阶段研究记录：以下选择、参数和结果保留原日期条件，部分实现已替代。当前运行行为以 [现行文档](../README.md) 为准；版本关系见 [研究索引](README.md)。

> 历史记录：用户随后决定只保留连续实际 RGB 识别和暂停识别，周期性消隐及其计时参数已退役。RecognitionSamplingCadence 仍用于连续/暂停采集期间的检查间隔，旧消隐次数不再代表当前产品行为。

日期：2026-11-06。用户希望进一步减少频闪，目标是减少无变化时的关闭次数，同时保留三张独立 RGB 稳定确认、两个窗口同步消隐，以及战斗中实时更新。

## 编码前检索与比较

关键词：`TanStack query refetchInterval adaptive polling`、`dotnet reactive Throttle scheduler`。实际读取两个仓库的 README、LICENSE 和以下源文件。

| 候选 | 文件与文件 SHA | 采用或放弃 |
| --- | --- | --- |
| [TanStack/query](https://github.com/TanStack/query) | [`packages/query-core/src/queryObserver.ts`](https://github.com/TanStack/query/blob/main/packages/query-core/src/queryObserver.ts)，`d46c910b15a85f114a76076e6bcd10d41226ed85`；MIT | 源码在更新查询状态后重新计算轮询间隔并重设计时器。借鉴“根据观测结果改变下一次检查间隔”的机制；不引入 TypeScript、网络请求或界面依赖。 |
| [dotnet/reactive](https://github.com/dotnet/reactive) | [`Rx.NET/Source/src/System.Reactive/Linq/Observable/Throttle.cs`](https://github.com/dotnet/reactive/blob/main/Rx.NET/Source/src/System.Reactive/Linq/Observable/Throttle.cs)，`5b579e0c1d5c1b358e7296ea6ae0e84aba7c906b`；MIT | 其 Throttle 在连续输入时取消旧计时并等待安静期，可帮助减少事件噪声；本项目还必须主动发现未知变化，不能只等编辑事件或不断推迟取样。因此不直接采用 Throttle 或增加 Rx.NET 依赖。 |

均可离线使用。只参考机制，继续复用现有 Unity 时间循环、`LayoutChangeTracker`、`ProjectionCaptureGate` 和相机接口，不复制第三方代码、不新增包。

## 具体方案与验收

- 稳定布局采用较长的检查间隔；RGB 提示未确认变化时短时间加速采样，确认后立即回到低频。
- 比较稳定间隔 0.6 / 0.8 / 1.0 秒与原固定 0.25 秒，快速确认间隔先设 0.12 秒。以实际 Unity 消隐次数和积木变动到确认的延迟选取默认值。
- 持续遮挡、未知、拒绝或相机异常时，快速期最多持续一秒，随后降低检查频率；恢复为可用候选时允许新的短确认期。
- 开战要求新 RGB 证据时主动进入有限快速期，仍然采足请求之后的独立帧。禁止以编辑动作、真实布局版本、JSON 或评价分数判断该不该消隐。
- 一次消隐仍为至少一帧、至少 0.01 秒；保持双窗口同步、取消/禁用/异常后恢复。F8 保留投影取样时不消隐。
- 验证纯调度规则、实际自动 RGB 更新、连续战斗与投影道路，以及自动停止和相机异常恢复。不会将仿真度量称为真实设备实验或主观最佳观感。

## 仿真对照与默认选择

通过 `ProjectionCadenceChecks` 驱动正式自动 RGB 采集、`LayoutChangeTracker` 和 `ProjectionCaptureGate`，每组观察四秒稳定期，再实际添加墙并请求三张新 RGB。Unity 6000.3.24f1，本地隔离项目，最短消隐仍为 0.01 秒 / 一帧。

| 稳定间隔 / 确认间隔（秒） | 4 秒稳定期消隐次数 | 本次增墙到确认（秒） | 请求新三帧到确认（秒） |
| --- | ---: | ---: | ---: |
| 0.25 / 0.25（原固定频率对照） | 16 | 0.692 | 0.740 |
| 0.6 / 0.12 | 6 | 0.466 | 0.341 |
| **0.8 / 0.12（默认）** | **5** | **1.041** | **0.359** |
| 1.0 / 0.12 | 4 | 1.242 | 0.355 |

选择 0.8 / 0.12：相对原稳定频率，关闭次数减少 68.75%；本次增墙确认仍约一秒，1.0 秒间隔的单次样例延迟更长。各次修改相对采样周期的相位不同，表中的单次延迟不能视为平均值或严谨的性能排名；理想情况下默认新增布局等待上限约为 0.8 + 2×0.12 = 1.04 秒，实际另受帧率、渲染与分类时间影响。若用户更重视即时响应，可将稳定间隔设为 0.6 秒，需重建识别会话或重新进入 Play 使配置生效。

频率减少主要作用于无变化的稳定期；出现待确认变化时仍有短促的快速采样。并未消除可见消隐，也未以永不关闭展示窗口掩盖它。

## 文件与验证

- `Assets/Scripts/Vision/RecognitionSamplingCadence.cs`：纯调度规则，稳定低频、有限快速期、未知/异常降频、可用候选恢复。
- `Assets/Scripts/Simulation/SimulationVisionMonitor.cs`：使用 RGB 确认状态选择间隔；开战的新证据请求维持快速期，直到满足请求的帧数。重复设置同样的自动模式不会反复重置快速期。
- 未修改相机的一帧 / 0.01 秒消隐参数、窗口同步门或 RGB 分类器；此前的空格中心寻路与局部脱离规则保留。
- `RunLogicChecks.ps1`：全量 **1195 项**，新增调度检查 **17 项**通过。
- `ProjectionCadenceChecks`：**29 项**通过，包含实测频率减少、自动 RGB 确认延迟、三张新帧要求。
- `ProjectionBlankingSyncChecks`：本次 **208 项**通过，覆盖双窗口一致、F8 带投影采集不消隐、停用/禁用、原本已关闭的投影不被误打开、相机异常恢复。
- `LiveProjectionRefreshChecks`：**146 项**通过，覆盖真实自动 RGB、战斗增删移动、暂停/波次/胜利后更新及实际投影道路位置。
