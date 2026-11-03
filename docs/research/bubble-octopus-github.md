# 泡泡章鱼塔的 GitHub 实现比较

检索日期 2026-11-03。关键词为 Unity tower defense projectile homing、Brackeys tower defense。验收以桌面投影中的腕足、表情、泡泡范围和命中同步扣血为准。参考本项目 README、架构文档与开发计划，战斗规则继续在无 Unity 依赖的固定步长模拟中结算。

| 候选 | 已读实现及授权 | 适用性 |
| --- | --- | --- |
| [Tower-Defense-Tutorial](https://github.com/Brackeys/Tower-Defense-Tutorial) | README、Turret.cs、Bullet.cs、LICENSE.md。Unlicense | Unity C#，发射后持续跟踪目标，在到达时扣血，目标消失时销毁。可离线运行，但使用逐帧 Unity 对象与组件。采用追踪与命中分离的思路。 |
| [Homing-Missile](https://github.com/Brackeys/Homing-Missile) | README、HomingMissile.cs。README 声明公有领域 | Unity C#，在固定物理步中转向并由碰撞回调触发销毁。离线可用，但引入刚体与触发器，且没有伤害同步。放弃物理实现。 |

两者均为教程示例。本项目自行实现按目标 ID 跟踪的泡泡，复用现有战斗步长、命中事件和投影面片，避免 Unity 物理和新增运行依赖。无外部代码或素材引入。验证目标为发射后保持血量、接触时扣血、暂停时冻结泡泡，以及目标消失后的清理。
