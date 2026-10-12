# Windows 照片导入按钮验收

日期：2026-10-12。全部按钮与模态窗口检查使用真实 Windows x64 非开发 Player。

## 旧版本复现

启动 v1.3.0，点击扫描积木，再点导入照片，不填写路径。运行：

```powershell
powershell -NoProfile -File .scratch/windows-photo-picker/Verify-PhotoPicker.ps1 -PlayerId 43988 -Expected Dialog
```

实际结果：退出 1，`EXPECTED_FILE_DIALOG_MISSING: Import clicked, no native file picker opened.`。只保留最低限度的一次空路径点击复现；确定的空路径直接返回分支无需二分或添加产品调试日志。

## v1.3.1 结果

| 检查 | 证据与结果 |
| --- | --- |
| 构建 | `../build.log`：`Build Finished, Result: Success.`，正常退出 0 |
| 同一窗口断言 | `PHOTO_PICKER_DIALOG_PASSED: 选择积木照片`；取消/选择后 `PHOTO_PICKER_NO_DIALOG_PASSED` |
| JPG 选择与解码 | `03-import-jpg.png`：从系统文件窗口选择随包实拍，照片正确显示 |
| 原生系统窗口 | `04-native-dialog.png`：中文标题、文件名输入、PNG/JPG/JPEG 图片筛选、打开/取消按钮；再次进入时恢复上一张照片的目录 |
| 已分析照片取消 | `05-cancel-preserves-photo.png`：原照片及 10 块的分析、检测框、地图和进入校对按钮均保留，同时显示未选择新照片提示 |
| PNG、中文空格路径 | `06-import-png.png`：通过系统窗口加载 `积木 示例.png`，可见图像内容。仅测试格式加载，未分析格纸 |
| JPEG、中文空格路径 | `07-import-jpeg.png`：加载 `积木 示例.jpeg`，显示原实拍内容 |
| 首次取消 | 实际检查通过；没有生成图像或覆盖地图 |
| 异常 | `player-fixed.log` 不含 Exception、NullReference、ArgumentNull 或编译错误 |
| 返回地图 | `08-map-preserved.png`：原有 9 块示例与路线保持，没有因选图或取消被替换 |
| 成品包 | v1.3.1 ZIP 42,817,334 字节，SHA-256 `c5546c09bf9f0028ea82bbc11af1d3fc15b0fd5d454ef0734c13fd525b51f8aa`；解压到全新中文/空格目录后 159 个清单文件逐一匹配 |

修复检查不使用 EditorUtility 替代 Player API。Windows P/Invoke 在 Editor/其他平台由条件编译排除，独立程序调用实际系统 DLL。测试辅助脚本限定 Player PID 与窗口类，不向其他程序输入；不随包分发测试脚本或玩家扫描日志。

## 测试边界

本次只重复与导入修复相关的原生窗口、解码和取消状态检查；完整玩法与校对测试已在 v1.3.0 完成。跨电脑兼容性、Android 相册和真实摄像头仍未验收。
