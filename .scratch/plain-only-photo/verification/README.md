# 无格纸唯一入口验证

日期：2026-10-11。Unity 6000.3.24f1 / Windows 编辑器，使用 `.scratch/soft-strategy-ui/unity-check` 隔离副本；保留用户正在打开的工作场景。

## 结果

| 检查 | 结果与覆盖 |
| --- | --- |
| `dotnet run --project Tests/BrickDefense.Checks.csproj` | 退出 0。200 个布局的独立 BFS、战斗与多入口、工作流快照/失败保留、默认无格纸实拍 10 块、角色互换、缩放、L 形/2×2 紧贴、未标定、非法设置与裁切拒绝通过。 |
| `BrickDefense.Editor.SoftStrategyUiChecks.VerifyPlainOnly` | 退出 0，`PLAIN_ONLY_UI_CHECKS_PASSED`。无需选模式，自动加载已有四颜色标定；实际示例按钮/出生点/扫描/取色/保存/分析/确认/返回，10 块实拍和裁切拒绝保护，照片原色、不透明页面与窄屏滚动通过。 |
| Unity 入口内的 `PhotoScanTools.VerifyPhotoIntegration` | `PHOTO_INTEGRATION_PASSED`。原 JPG 解码、PNG 编码/解码像素往返、10 块/40 个占用导航格和关卡生成通过。 |
| 报告保存 | `PLAIN_REPORT_PASSED`。实际页面保存报告，包含 `PlainBackground` 和 `RelativeBrickScale`。测试生成的 ScanReports 已加入忽略规则。 |

标定恢复验证在测试前备份默认键 `BrickDefense.PhotoCalibration.plain.v1`，写入已保存的四颜色配置；进入 Play 后检查页面实际恢复的各槽位，并在结束时恢复原值或原来的无键状态。自定义测试标定键也已清理。

当前运行时代码不再包含 `PhotoInputMode`、`SetInputMode`、格纸识别器/定位器/投影拟合/模板、ArUco 或格纸专用阈值。已有脚本 GUID 保留，删除的资源和对应 `.meta` 一起移除。早期照片和研究文档保留为历史。

## 截图

实际窗口为 1280×801、862×679、540×801。已目视检查无模式切换、中文文字和关键操作完整、窄屏可滚动到结果并返回。

- [默认扫描入口](photo-empty.png)
- [完整实拍识别成功](photo-success.png)
- [裁切拒绝](photo-rejected.png) / [较小窗口](photo-small.png)
- [竖屏顶部](photo-narrow-top.png) / [竖屏滚动到结果](photo-narrow-bottom.png)
- [示例地图](map-reference.png) / [封闭示例](map-enclosed.png) / [竖屏生成地图](map-narrow.png) / [较小窗口地图](map-small.png)

本次未测硬件相机或 Android 真机，也没有据单张样本宣称通用准确率提高。编辑器启动时仍有既有 `UnityEditor.Search.SearchDatabase` 索引异常，实际项目编译及操作检查完成，未发现项目脚本异常。
