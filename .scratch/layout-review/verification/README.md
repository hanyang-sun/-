# 地图校对验证

日期：2026-10-12。Unity 6000.3.24f1 / Windows 编辑器。隔离副本为 `.scratch/layout-review/unity-check`，测试记录输出到本目录的忽略项 `ScanReports/`，未改写用户主工程场景或默认颜色标定。

## 结果

| 检查 | 结果 |
| --- | --- |
| 完整 .NET 回归 | `dotnet run --project Tests/BrickDefense.Checks.csproj` 退出 0；独立 BFS 200 布局、战斗、多出生点、工作流、实拍与裁切回归通过。 |
| 最终校对回归 | `--layout-review-checks` 退出 0，`LAYOUT_REVIEW_CHECKS_PASSED`；颜色/格位独立统计、漏检误检、稳定 ID、撤销、整体平移、越界/占格保护、冻结真值及证据、缺核心草稿、恢复与发布通过。 |
| Unity 实际操作 | `BrickDefense.Editor.LayoutReviewUiChecks.Verify` 记录 `LAYOUT_REVIEW_UI_CHECKS_PASSED` 并正常退出；最终日志 `.scratch/layout-review/unity-ui-check-verified.log`。 |
| 真实照片入口 | 原 JPG 检测 10 块；实际点击进入校对、选格、移位、撤销、确认勾选和确认按钮通过，确认前没有统计，也不替换当前关卡。 |
| 两阶段与持久化 | 人工确认后保存 original/verified/designed 三组数据及两项统计；自由设计增删不改真值。发布后 JSON 标记 published=true，原 result.json 仍存在。 |
| 三维预览和发布保护 | 可预览 11 块设计地图及没有核心的草稿；无核心生成被拦截，旧关卡保持；补核心后成功发布。 |
| 部分检测和窄屏 | 裁切原图保留非空的可用检测，状态仍为 Rejected，可进入校对；实际 540×801 竖屏滚动、工具和固定底部返回按钮通过。 |

纯 C# 构造了 4 块真值的纠错用例，预期识别正确 2/4、位置正确 2/4，并同时含 1 漏检、1 误检、1 颜色错误和 1 已匹配块位置错误，验证分母与指标没有混用。移动同一块两次再撤销、整体平移和后续游戏设计均有覆盖。

## 截图

- [照片入口](photo-entry.png)
- [实物校对 / 宽屏](review-physical-wide.png)
- [确认后的统计与游戏设计](review-design-wide.png)
- [三维预览](review-3d-preview.png) / [没有核心也可预览](review-3d-no-core.png)
- [发布后的游戏地图](review-published-map.png)
- [裁切照片的可修正草稿](review-partial-draft.png)
- [竖屏顶部](review-narrow-top.png) / [竖屏工具与确认勾选](review-narrow-tools.png)

已目视检查最终截图；确认勾选改为可见方框，宽窄屏工具与底部按钮完整，三维预览沿用正交桌游视角。

## 边界

测试中的 100% 为单次人工确认流程的预期值，不代表通用实拍准确率或独立正式测试集验收。颜色与格位真值由玩家逐块核验，稳定 ID 用于对照；修正位置应使用移动，删除再补加会分别记作误检和漏检。

位置仅评价按预先固定 min-column/min-row → 1 约定的相对布局，未恢复绝对物理原点、毫米尺度或堆叠高度。Android 真机、硬件相机和设备性能未验收；其余两项评价指标的汇总仍待后续工作。

最终日志有启动期的既有 `UnityEditor.Search.SearchDatabase` 索引异常，栈仅涉及 Editor Search；项目编译及实际操作检查通过。开发过程中编辑器检查缺失的命名空间引用已修正，最终版本不再出现该编译错误。
