Status: ready-for-agent
Completion: complete (2026-10-11)

# 多点选择、各点寻路与轮流出怪

按 spec.md 实现合同、选择流程、战斗快照、逐敌人路线与总数分配；覆盖无效选择、跨波、快照、锁定和重试。

## Comments

- 2026-10-11：GitHub 机制比较完成，开始实施。
- 2026-10-11：多点合同/独立 A*/轮转/快照/锁定完成；MultipleSpawnChecks 与全部 .NET 回归通过，Unity 三入口波次、重试和双入口窄屏战斗通过。
