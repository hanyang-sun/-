# 无格纸实拍验证记录

`spec.md` 记录用户授权的范围与完成结果。
`unity-play.log` 保存 2026-10-09 隔离 Unity Play 回归日志；`Artifacts/real-unmarked-photograph.png` 是由上传原 JPG 生成的三维地图截图。
`unity-check/` 是被 Git 忽略的验证副本及缓存，未改变正在打开的工作项目。

可重复检查：项目根运行 `dotnet run --project Tests/BrickDefense.Checks.csproj`；另在隔离 Unity 项目中使用 `-batchmode -executeMethod BrickDefense.Editor.DemoSceneChecks.VerifyExamples`，保持图形渲染，完成后工具自行退出。
批量检查通过后不要在原项目同时运行另一个 Unity 实例。
