# 双窗口采样消隐同步：复用比较

日期：2026-11-02。范围：源场景投影与独立视图共享采样消隐，保留棋盘和积木；恢复原可见状态。用户明确要求搭建 GameView 始终排除投影，保持此配置。保留现有采样模式、RGB 预测和三帧确认，不调整玩法或删除窗口。

## 检索与实际阅读

关键词：`Unity EditorWindow Repaint capture frame`、`Unity PlayerLoop DelayFrame cancellation`。检索 GitHub 后比较以下两个实际仓库：

| 候选 | 阅读文件 | 许可证 | 判断 |
| --- | --- | --- | --- |
| [UnityCsReference](https://github.com/Unity-Technologies/UnityCsReference) | README.md、LICENSE.md、Editor/Mono/EditorWindow.cs（Repaint / RepaintImmediately） | Unity Reference Only License；仅阅读参考，不复制 | Repaint 请求窗口刷新，与同步 Camera.Render 并非同一件事。采用公开 Repaint 通知，不复制引擎实现或调用内部强制重绘 API。EditorWindow 本身不可用于 Android，运行时消隐状态不得依赖 UnityEditor。 |
| [UniTask](https://github.com/Cysharp/UniTask) | README.md 的 PlayerLoop/取消/Editor 说明、LICENSE、Runtime/PlayerLoopHelper.cs | MIT | 可按 PlayerLoop 等待帧并管理取消，但本任务引入完整异步库成本过高。参考按帧推进、显式取消与释放状态的机制，使用现有 MonoBehaviour.Update 实现；不引入包、不改写外部代码。PlayerLoop 机制适合 Unity/C#，可离线，不要求线程或网络。 |

以上读取的是检索日 master，Unity 参考源当前为 7000 系列，只用于理解窗口刷新机制，最终通过本工程 6000.3.24f1 实测。UniTask README 特别区分末尾 PlayerLoop 与 WaitForEndOfFrame；本项目显式 Camera.Render/ReadPixels，不将普通 Update 当作屏幕帧已呈现。

## 本地方案

优先复用 ProjectionCaptureGate、SimulationCameraCapture 与现有预览。自动采样增加可取消的跨帧采集阶段：统一隐藏 → 保持可配置的实际帧/时间 → 采集 → finally 恢复 → 原识别/确认链。同步 CaptureObservationImage/AnalyzeOnce 保留用于现有测试及显式单次调用。关闭自动识别、禁用组件或失败时释放未完成采集，避免永远隐藏。

Gate 发布共享消隐状态和变化通知；视图用该状态控制投影副本，状态变化立即请求重绘。搭建相机按用户要求始终排除游戏与投影层；不因同步需求让搭建窗口显示投影。发现其他行为差异时先与用户讨论，不自行扩展修改范围。

不把整张窗口涂黑；不隐藏棋盘/积木；不更改 F8 或三帧确认规则；不假称硬件同步或保证操作系统两个窗口像素级同时刷新。

## 验证

新增自动采样回归：原工程真实场景、搭建相机和独立预览，观察投影正常/消隐两个阶段，检查两处一致及实际 RGB；覆盖恢复、原本关闭、禁用/取消、带投影模式和采集失败。复跑原自动布局更新检查，确认战斗不依赖重试。不采用外部代码，故无需新增第三方代码声明。

实测（Unity 6000.3.24f1，隔离工程）：新增 `ProjectionBlankingSyncChecks` 通过，本次 333 项断言、54 个隐藏观察帧及 74 个可见观察帧；计数随运行帧率变化。原双窗口 179 项通过，干净 RGB 对照差异为 0 像素；自动战斗刷新 108 项通过。搭建相机始终排除投影层的断言已保留。结果属于 Unity 仿真回归，不代表真实硬件时序或观看舒适度已验证。
