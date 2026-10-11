# 地图缩放：实际 Unity 画面

2026-10-11，Windows / Unity 6000.3.24f1，内置渲染管线。由隔离工程 OrbitCameraChecks.Verify 的 ScreenCapture 获取；每次截图后等待渲染再继续操作。这里的窄屏图来自 Windows 编辑器，不是 Android APK 截图。

| 文件 | 内容 | 文件尺寸 |
| --- | --- | --- |
| map-tabletop.png | 默认 100%，新增缩放按钮与倍率显示 | 1280×800 |
| map-zoomed.png | 250%局部放大，仍为正交视图 | 1280×800 |
| map-zoom-out.png | 65%缩小，旋转后的视图 | 1280×800 |
| map-narrow-zoomed.png | 窄屏 115%，旋转后轻点选出生点 `(10,9)` | 540×800 |

最终日志 ../unity-check-fixed.log（本地忽略），退出 0，TOUCH_FRAME_CHECKS_PASSED 和 ORBIT_CAMERA_CHECKS_PASSED。真实滚轮/按钮事件与 Touch 帧注入的验证范围及 Android 待验收项见 ../spec.md。

仅在已有隔离工程复查：

```powershell
& 'D:\Unity\Editor\6000.3.24f1\Editor\Unity.exe' -projectPath '<项目>\.scratch\soft-strategy-ui\unity-check' -executeMethod BrickDefense.Editor.OrbitCameraChecks.Verify -logFile '<日志绝对路径>'
```

检查会关闭运行它的隔离编辑器，不可在用户正在使用的原工程调用。
