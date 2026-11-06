# 网格塔防的入口、路径与目标判定

调研日期 2026-11-03。商业案例依据官方手册和开发者说明，开源案例依据源码。公开材料不足以确认商业案例的亚格坐标。

| 案例 | 出生 | 路径 | 核心或生命结算 |
| --- | --- | --- | --- |
| Defense Grid 2 | 手册确认敌人按波进图。普通入口内的具体坐标和选择规则未公开。部分敌人会在地图内开传送门或释放其他敌人。 | 布塔布局可改变路线。可打开敌人路线指示器。未确认寻路算法或路线沿格心还是格线。 | 核心分安全、被盗但可回收、永久丢失三种状态。敌人到核心处取走核心，带出地图才永久丢失。 |
| Mindustry 地面波次 | 扫描地图的出生标记。波次组可筛选指定标记。普通地面单位的生成坐标是在标记世界坐标上加长度为两个地块的随机方向偏移。 | 地面 AI 跟随核心流场，向下一地块的世界坐标连续移动。 | 接近核心时，符合条件的武器将核心设为目标。核心是可受武器攻击的建筑，玩家核心全部被摧毁时判负。 |
| Catlike Coding Unity 塔防示例 | 教程先从配置的入口格中随机挑一个，生成位置是所选格中心。 | 从目的地反向计算最短路。移动从入口格中心出发，跨过相邻格共享边的中点，并在格内做转向。 | 敌人到目的地格的中心时回收，玩家减一生命。 |

- Defense Grid 2 官方 Xbox 手册和 Hidden Path 开发者文章。 [手册](https://dlassets-ssl.xboxlive.com/public/content/d61fbdfe-c184-4f9d-9c91-158a7b48a8d3/GameManual/326b47f0-1ce7-47ce-a16a-bdf4ac0ee0f5/en-SA/index.html) · [开发者说明](https://blog.playstation.com/?p=141818)
- Mindustry 实现。 [WaveSpawner](https://github.com/Anuken/Mindustry/blob/master/core/src/mindustry/ai/WaveSpawner.java) · [GroundAI](https://github.com/Anuken/Mindustry/blob/master/core/src/mindustry/ai/types/GroundAI.java) · [AIController](https://github.com/Anuken/Mindustry/blob/master/core/src/mindustry/entities/units/AIController.java) · [Logic](https://github.com/Anuken/Mindustry/blob/master/core/src/mindustry/core/Logic.java)
- Catlike Coding 教程源码。 [The Board](https://catlikecoding.com/unity/tutorials/tower-defense/the-board/) · [Enemies](https://catlikecoding.com/unity/tutorials/tower-defense/enemies/) · [Scenarios](https://catlikecoding.com/unity/tutorials/tower-defense/scenarios/)

## 与 Brick2Life 的关系

入口标记、寻路拓扑和敌人画面坐标可以分别定义。Catlike 的敌人跨越共享边中点，实际轨迹仍经过格内部。Brick2Life 当前的逻辑以每个实体格的四个导航格为单位，入口固定选低 X、低 Y 子格。现有 Overlay 将导航坐标乘 0.5，未加 0.25 的格心偏移，因此当前画面沿导航网格边界移动。核心的四个导航格都是可选终点，敌人走完路线时结算。 [BattleLevel](../../Assets/Scripts/Battle/BattleLevel.cs) · [RouteSearch](../../Assets/Scripts/Battle/RouteSearch.cs) · [BattleSimulation](../../Assets/Scripts/Battle/BattleSimulation.cs) · [SimulationBattleOverlay](../../Assets/Scripts/Feedback/SimulationBattleOverlay.cs)

如果后续要让路线和小焰的受击反馈位置一致，可分别评估入口中心、导航格中心以及可见的核心接触点。单独移动投影图标只会改变视觉位置，改变判定点需要同时确认路线、实际敌人位置和扣血时机。本文未改动工程代码。
