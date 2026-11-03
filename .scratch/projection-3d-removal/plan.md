# 投影化改造：去掉层 30 实体替身，并入层 31 Overlay

## 决策（2026-11-03 确认）
- 保留真实积木；敌人投影带血条与编号；开火带激光弹道线。
- 不新建层、不建第二门控：投影仍进 SimulationBattleOverlay（层 31），同一 MarksRoot，走 IProjectionGate / ProjectionCaptureGate。
- 层语义：29 目标引导 / 30 游戏实体 / 31 投影；顶置相机继续排除 29+30；不动层 29。
- 删除范围只限层 30：核心/墙/塔 Cube、敌人 Sphere、路线 LineRenderer、入口 Cylinder。
- 第一项批准：SimulationBattleView 一次性改为纯门面，控制器调用点一并改完，不做长期双写。
- 第二项暂缓：混合模式开关（实体+投影 / 仅投影）保留在方案中，本轮不实现。

## 里程碑
1. 兼容开关 + 探针/文档改口径（29/30/31；layer30Renderers、cameraDiffGameObjects、投影 on/off diff 分开记；按脚本列清单不写死断言总数）。
2. 关角色实体：SimulationLevelView.renderGameObjects=false（默认），保留类型/序列化，核心/塔/墙只留 ProjectionSimulator 图案。
3. 敌人上 31 + 删 Sphere + blanking/live 绿：billboard + 血条（用 WavePlan.EnemyHealth 存最大值，修 Health/Health 恒满）+ 编号；对象挂 Overlay 子树；RefreshLayout 按布局内容变化刷新环/路径/瞄准。
4. 路径光带 + 范围环美观 + 入口投影替代：柔边光带（UV 流光）、箭头、渐变呼吸环 + 扫描高光 + 入侵变色；删 30 入口柱后在 31 补手动入口标记。
5. 双窗口镜像适配 + 截图/文档：镜像 overlay.MarksRoot；projectionOnly 仍可见敌人/路径；三组截图（原始积木 / 投影窗口 / 混合模式留待后续）。

## 验收
layer30Renderers == 0；projectionEnemies == 敌人数；rings == 塔数；pathSegments > 0；
cameraDiffGameObjects == 0；projection on/off pixel diff 单独记录；monitor == Unchanged。