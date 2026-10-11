# 多出生点验证记录

日期：2026-10-11。Unity 6000.3.24f1，Windows，隔离项目 `.scratch/soft-strategy-ui/unity-check`；原工作项目未被验证器启动/关闭。所有图片为 Unity Play 截图。

## 已运行

- `dotnet run --project Tests/BrickDefense.Checks.csproj --no-restore`：exit 0；200 随机布局 BFS 对照、既有战斗/照片/颜色/流程/无格纸/扫描拒绝回归与 MULTIPLE_SPAWN_CHECKS_PASSED 全通过。
- Unity `-executeMethod BrickDefense.Editor.BattleChecks.VerifyMultipleSpawns`：exit 0，MULTIPLE_SPAWN_UNITY_CHECKS_PASSED。
- Unity `-executeMethod BrickDefense.Editor.OrbitCameraChecks.Verify`：exit 0，TOUCH_FRAME_CHECKS_PASSED、ORBIT_CAMERA_CHECKS_PASSED。

多点检查通过实际 Game View 鼠标事件验证添加、删除、清空、占用拒绝和零入口禁用；确认编号标记、原相机面板区域允许拖动/滚轮、三个入口锁定/出怪/手动下一波/重试、暂停、射程、胜负与返回扫描。窄屏经实际 DemoUI.HandleTouches 桥接验证轻点增删，双入口独立出怪。

相机回归继续验证鼠标拖动/滚轮、俯仰和缩放边界、正交远近大小、宽窄选格、单指拖动、双指捏合、指头切换、UI/照片隔离、焦点丢失、取消及模拟鼠标重复屏蔽。删除的按钮检查改为相机 API 检查，未将 API 调用记作按钮操作。

发现并修复结算返回后继续使用旧状态绘制的空引用；最终多点运行无该项目异常。日志中的 Unity Search 索引初始化异常属于已存在的编辑器启动问题，发生在 Play 前；未阻止编译或上述检查。

## 截图

- `multiple-starts-layout-wide.png`：1280×800，9 块参考布局、3 个入口、编号和所有路线；相机卡片已删除。
- `multiple-starts-layout-narrow.png`：540×800，单核心布局、2 个入口；演示取消/重选后入口保留。
- `multiple-starts-active-narrow.png`：540×800，双入口战斗、来自第二条路线的敌人、共享核心受伤。
- `battle-setup-wide.png`：总数量字段与入口数量说明。
- `battle-active-wide.png`：三入口战斗、射击反馈和暂停。
- `battle-next-wave-wide.png`：本波结束后等待手动开始下一波。
- `battle-victory-wide.png`：三入口默认试玩胜利。
- `battle-active-narrow.png`：战斗时拖动/捏合后的正交视图。
- `battle-defeat-narrow.png`：无塔布局失败及结算。

战斗检查用应用接口快速推进模拟时间以覆盖终局；不将其作为实际帧率测量。触摸由 Windows 编辑器注入当前手势桥接，不是 Android 真机测试；本机仍未安装 Android 构建模块。
