# 布局挑战 A：验证画面与范围

2026-10-11，Windows / Unity 6000.3.24f1 / 内置渲染管线。图片来自隔离工程的实际 Play 画面与 ScreenCapture，没有生成或编辑战斗截图。

| 文件 | 内容 | 尺寸 |
| --- | --- | --- |
| battle-setup-wide.png | 六项开局参数与默认塔/核心配置 | 1280×800 |
| battle-active-wide.png | 暂停的第一波、敌人血条和射击效果 | 1280×800 |
| battle-next-wave-wide.png | 第一波清场，等待点击下一波 | 1280×800 |
| battle-victory-wide.png | 三波胜利及统计、重试/返回按钮 | 1280×800 |
| battle-active-narrow.png | 540 宽窄屏，触摸拖动/捏合后相机与对局 UI | 540×800 |
| battle-defeat-narrow.png | 无塔布局失败，核心归零及完整结算 | 540×800 |

检查方法：`BattleChecks.Verify` 只允许在现有 `.scratch/soft-strategy-ui/unity-check` 隔离工程执行，会退出该隔离编辑器。启动、字段错误、下一波、重试、结束与重新进入照片页使用实际 Game View 鼠标按钮事件。为缩短验证时间，波次其余时间通过同一 `BattleSession.Advance` 接口加速，未把截图称为实时性能结果。触摸使用 Touch 帧注入，不模拟 Android 驱动。

固定 Game View 分辨率仅修改隔离进程的内存对象，结束时恢复原分辨率，不增加或保存自定义分辨率。对右键事件断言等待实际后续渲染帧；截图拍摄后再修改表单。

最终日志 `../unity-check-final.log` 本地忽略，包含 `BATTLE_UNITY_CHECKS_PASSED`，Unity 验证进程退出码 **0**。截图日志确认等待下一波为 `AwaitingNextWave`、胜利为 `Victory`、失败为 `Defeat`；拍摄这些画面后等待渲染再点击下一波或重试。全部六张图片已进行相应布局复核。纯 C# 验证：

```powershell
dotnet run --project Tests/BrickDefense.Checks.csproj -- --combat-checks
dotnet run --project Tests/BrickDefense.Checks.csproj
```

完整 `.NET` 回归和战斗检查均通过。Android APK、相册选择、触控手感及设备性能没有在这次 Windows 编辑器检查中验收。
