# 任务 C：当前关卡反馈与投影

更新：2026-11-07。适用 Brick2Life-C 当前本地工作树、Unity 6000.3.24f1、Built-in。默认场景为 `Assets/Scenes/Brick2LifeSimulation.unity`。

## 状态来源与规则

C 消费 B 发布的 `BoardUpdate`，通过 `LevelFeedbackSession` 得到 `LevelState` 和 `ProjectionPlan`。默认场景共享 B 的唯一 `BoardSyncSession`，不读取真实积木布局或评价分数核验预测。人工目标接口只供明确标注的独立测试；自动识别接入时演示组件自动停用。

默认玩法由系统生成一个核心、1–4 个入口；目标是独立关卡配置，不是蓝色积木预测。玩家摆绿色墙、红色塔，无数量配额，但不能占用系统保留格。新确认在战斗、暂停、波次间隔及胜败后均刷新显示。局部敌人脱离与全局堵路回退规则见 [系统目标玩法](generated-defense-round-guide.md) 和 [实时编辑](live-battle-editing-guide.md)。旧手动颜色核心只作兼容 fixture。

## 当前调用链与文件

```text
B 的确认结果 → BoardUpdate → SimulationLevelAdapter / LevelFeedbackSession
  ├─ LevelState → SimulationBattleController → BattleSimulation
  └─ ProjectionPlan → ProjectionSimulator
战斗状态 → SimulationBattleOverlay
图案内容 → 投影仪视角的透视内容相机 → 输出纹理
  → SimulationProjectedLight / SurfaceLight.shader → 表面反射光和遮挡
  → TopCamera RGB / 独立展示窗口
```

- `Application/LevelSemantics.cs`、`ProjectionPlan.cs`、`LevelFeedbackSession.cs`：语义、标记和确认反馈合同。
- `Feedback/SimulationFeedbackBootstrap.cs`、`SimulationLevelAdapter.cs`：与 B 共享会话、接收更新并连接显示。
- `Feedback/ProjectionSimulator.cs`、`SimulationBattleOverlay.cs`：生成角色纹样、变更提示、路线和战斗动画的内容。
- `Feedback/SimulationProjectedLight.cs`、`Resources/ProjectedLight/`：透视生成与回投、反射和遮挡近似。
- `Editor/SimulationProjectionPreview.cs`：只读预览；积木由已确认布局重建，复用投影输出。
- `Editor/SimulationFeedbackSetup.cs`：安装、接线及分层检查。

上述路径均相对 `Assets/Scripts/`；`Editor/` 和 `Resources/` 路径相对 `Assets/`。

## 投影、分层与识别

源 Quad 只生成输出纹理，不再作为贴面投影直接进入观察图像。内容相机与投影仪共用透视参数和矩阵，章鱼脸按已确认单层顶面 y=1 放置，地面效果在 y=0；图案叠放使用绘制排序。`SimulationLevelView`、五个旧三维角色材质及构造式对齐接口已删除，安装菜单不会重建它们。

| 层/视图 | 当前用途 |
| --- | --- |
| 29 `Brick2LifeObjectives` | 搭建窗口中的系统目标提示，顶置相机排除 |
| 30 `Brick2LifeGame` | 保留的游戏层约定，顶置相机排除；无旧三维角色替身 |
| 31 `Brick2LifeProjection` | 投影内容源，专用内容相机生成输出 |
| 搭建视图 | 保持无投影，方便编辑 |
| 顶置 RGB / 展示窗口 | 接收表面投影光；排除 31 层不能消除已反射的光 |

F8/顶部按钮只切换连续识别与暂停识别。连续模式不消隐；暂停模式由统一门在暂停采集期间持续关投影，继续/开战的新确认完成后恢复。异常、取消和禁用释放门。已删除周期性消隐参数、旧跨帧采样 API 与 F5/F6/F7 快捷键；不要按旧文档重新添加。

## 安装与验证

正常使用只需打开默认场景进入 Play。需要重装接线时，退出 Play，使用 `Brick2Life/Simulation/Install feedback and save scene`；不需要重建旧 `Level` 节点或 `Game_Core/Game_Wall/Game_Tower` 材质。分层菜单 `Brick2Life/Simulation/Verify feedback layering` 检查相机掩码约定，不能替代实际 RGB 光斑和遮挡测量。

当前参数与两种模式操作见 [固定投影仪](physical-projector-guide.md)，几何测量见 [投影贴合](projection-alignment-guide.md)，命令和最后一次本地结果见 [验证摘要](verification/current-simulation.md)。当前文档清理没有重跑 Unity 或 Windows 构建。

当前投影仍是可控的直接反射近似，未标定真实设备的畸变、失焦、色彩响应或曝光积分；高度 fixture 不代表多层识别已完成。
