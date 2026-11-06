# 研究与复用记录

整理：2026-11-07。这些文件保留编码前比较、授权范围、取舍及阶段结果，不是当前运行手册；文中的“当前”以该记录日期为准。实际行为见 [文档导航](../README.md)。

当前连续/暂停识别整合见 [连续识别与 C 成像](continuous-recognition-physical-projector-github.md)，固定投影仪选型见 [投影研究](physical-projector-github.md)，局部敌人脱离见 [重规划记录](live-enemy-overlap-rerouting-github.md)，空格中心路线见 [中心与间距](centered-clearance-routing-github.md)。

以下记录描述的实现已被替代：

| 记录 | 退役部分 | 当前说明 |
| --- | --- | --- |
| [战斗编辑锁](battle-editing-lock-github.md) | 战斗期间禁止编辑 | [实时编辑](../live-battle-editing-guide.md) |
| [同步消隐](synchronized-projection-blanking-github.md)、[自适应采样](adaptive-projection-sampling-github.md) | 周期性消隐与旧跨帧采集 API；自适应采样调度仍保留 | [两种识别模式](../task-b-recognition-guide.md) |
| [初期投影](task-c-projection-github.md) | 直接贴面片、旧三维替身和 F5/F6/F7 | [固定投影仪](../physical-projector-guide.md) |
| [早期双窗口](editor-dual-window-github.md)、[A/C 整合](a-c-integration-github.md) | 预览复制面片/角色模型、早期路线和采样行为 | [当前双窗口](../editor-dual-window-guide.md) |

模型候选、旧资产与多层论文只是研究或历史选型，不表示已经导入资产、启用旧工程或完成多层识别。不得按历史记录重新引入已删除代码。
