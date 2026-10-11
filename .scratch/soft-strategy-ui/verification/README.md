# 柔和策略 UI：实际 Unity Game 视图

2026-10-11，Windows，Unity 6000.3.24f1，内置渲染管线。

这些图由项目内隔离副本的 `SoftStrategyUiChecks.Verify` 使用 `ScreenCapture.CaptureScreenshot` 获取，包含真实 IMGUI；不是设计稿或 Camera.Render 的地图截图。

| 画面 | 文件 | 实际尺寸 |
| --- | --- | --- |
| 需求示例与包围示例 | map-reference.png / map-enclosed.png | 1280×801 |
| 照片空态、10 块实拍成功、裁切拒绝 | photo-empty.png / photo-success.png / photo-rejected.png | 1280×801 |
| 用户常用小窗口 | photo-small.png / map-small.png | 862×679 |
| 窄屏照片顶部、滚动到底部、地图 | photo-narrow-top.png / photo-narrow-bottom.png / map-narrow.png | 540×801 |

检查通过真实 Game 视图鼠标事件操作按钮、缩放后的棋盘选点和照片取色，验证保存、分析、确认、拒绝禁用、照片 RGB、面板遮盖和滚动。测试窗口设置自由比例；关闭隔离编辑器的其他 Game 视图；按渲染像素与编辑器窗口的 DPI 比例换算尺寸。容许窗口边框误差，记录实际截图尺寸。

在 `.scratch/soft-strategy-ui/unity-check` 隔离副本中运行：

```powershell
& 'D:\Unity\Editor\6000.3.24f1\Editor\Unity.exe' -projectPath '<隔离副本绝对路径>' -executeMethod BrickDefense.Editor.SoftStrategyUiChecks.Verify -logFile '<日志绝对路径>'
```

检查会退出该隔离编辑器。不要对正在使用的原项目执行此命令。
