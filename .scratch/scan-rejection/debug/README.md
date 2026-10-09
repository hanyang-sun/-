# 拒绝问题的直接回放

两个目录保存当时原 JPG、原 `result.json` 和该 JPG 解码的 `source.rgb.gz`。后者解压为小端 int32 宽、高及逐行 RGB24；没有对原照片做图像编辑。报告中的 palette/settings 由公开识别入口使用，不依赖 Unity 面板私有字段。

在项目根目录运行：

```powershell
dotnet run --project Tests/BrickDefense.Checks.csproj -- --replay-scan .scratch/scan-rejection/debug/scan-444a6d9f33ed433db335a9bfa49ba752
dotnet run --project Tests/BrickDefense.Checks.csproj -- --replay-scan .scratch/scan-rejection/debug/scan-d431df17628641cca0444cf315667db1 --expect-rejected
dotnet run --project Tests/BrickDefense.Checks.csproj -- --scan-rejection-checks
```

第一项修复前退出码 1、`BRICK_CROPPED`；修复后退出码 0、10 块。第二项原照片本身裁切，预期拒绝；新旧都退出码 0，包含两项裁切和拆分不确定。

诊断时单变量实验可在第一项后加 `--background-at 650,850`。该选项只改变回放的背景样本，不修改保存记录或用户配置。旧代码实验曾使背景未知组件消失、尺度保持 64、10 块通过，证明误判来自背景标定与明度变化。

`overlay-before.png` / `overlay-after.png` 为实际 Game 视图截图，`full-photo-level.png` 为完整原 JPG 经确认后相机渲染的关卡。临时生产 Console 分割输出已移除。
