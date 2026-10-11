# 正交桌游视角：实际 Unity 画面

2026-10-11，Windows / Unity 6000.3.24f1，内置渲染管线。

截图由隔离工程的 `OrbitCameraChecks.Verify` 使用 ScreenCapture.CaptureScreenshot 获取，包含真实 IMGUI。每次截图后等待渲染完成，再执行下一次旋转或复位。

本目录为最新桌游视角修正后的截图，已替换初版 25°—80°旋转检查的画面。相机默认 55°，俯仰 45°—75°，始终使用正交投影；取景为底部按钮和窄屏图例保留空间。

| 文件 | 画面 | 实际尺寸 |
| --- | --- | --- |
| map-tabletop.png | 默认 55°斜俯视，需求示例 16 步路线 | 1280×801 |
| map-orbited.png | 右键水平及垂直拖动后 | 1280×801 |
| map-low-angle.png | 45°最低俯仰 | 1280×801 |
| map-overhead.png | 75°最高俯仰 | 1280×801 |
| map-narrow-orbited.png | 窄屏转向后 | 540×801 |
| map-narrow-reset.png | 窄屏复位与重新选格 | 540×801 |

最终日志 `.scratch/orbit-camera/unity-check-tabletop-final.log`（本地忽略），退出 0，`ORBIT_CAMERA_CHECKS_PASSED`。检查默认/旋转/窄屏下两块同尺寸积木在不同深度的屏幕包围盒尺寸相差小于 0.01 像素；主动切换透视时差值超过 1 像素，证明检查能检测出透视。旋转和无输入的每帧更新均恢复正交；棋盘包围盒位于底部控件上方。

最终六张 PNG 的实际尺寸通过文件读取核对；运行中某一次截图请求日志显示瞬时 1280×832，渲染完成后的 PNG 为 1280×801，本表以文件尺寸为准。

复查时运行隔离工程：

```powershell
& 'D:\Unity\Editor\6000.3.24f1\Editor\Unity.exe' -projectPath '<项目>\.scratch\soft-strategy-ui\unity-check' -executeMethod BrickDefense.Editor.OrbitCameraChecks.Verify -logFile '<日志绝对路径>'
```

检查会关闭运行它的隔离编辑器；不可在用户正在使用的原项目调用。
