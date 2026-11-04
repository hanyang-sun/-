# 无格纸实拍验证记录

> 归档说明（2026-11-04）：本页记录当时的验证范围，不能直接作为当前主线验收。截图、扫描中间数据和一次性调试代码已停止跟踪，本地文件保留；完整清单与固定历史提交见[验证资料索引](../../docs/verification/README.md)。旧 `BrickDefense.Checks.csproj` 命令需在对应旧工程中使用。

`spec.md` 记录用户授权的范围与完成结果。
`unity-play.log` 保存 2026-10-09 隔离 Unity Play 回归日志；`Artifacts/real-unmarked-photograph.png` 是由上传原 JPG 生成的三维地图截图。
`unity-check/` 是被 Git 忽略的验证副本及缓存，未改变正在打开的工作项目。

可重复检查：项目根运行 `dotnet run --project Tests/BrickDefense.Checks.csproj`；另在隔离 Unity 项目中使用 `-batchmode -executeMethod BrickDefense.Editor.DemoSceneChecks.VerifyExamples`，保持图形渲染，完成后工具自行退出。
批量检查通过后不要在原项目同时运行另一个 Unity 实例。
