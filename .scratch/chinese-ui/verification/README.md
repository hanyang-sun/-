# 中文界面与右侧战斗卡片验证

> 归档说明（2026-11-04）：本页记录当时的验证范围，不能直接作为当前主线验收。截图、扫描中间数据和一次性调试代码已停止跟踪，本地文件保留；完整清单与固定历史提交见[验证资料索引](../../../docs/verification/README.md)。旧 `BrickDefense.Checks.csproj` 命令需在对应旧工程中使用。

日期：2026-10-11。Unity 6000.3.24f1，Windows 编辑器，使用 `.scratch/soft-strategy-ui/unity-check` 隔离项目；未修改用户打开的工作场景。

## 验证结果

| 入口 | 结果 | 覆盖 |
| --- | --- | --- |
| `BrickDefense.Editor.BattleChecks.VerifyChineseUi` | 退出 0，`CHINESE_UI_CHECKS_PASSED` | 右侧设置按钮真实点击、中文非法波数提示、UI 开始的拖动不会旋转棋盘、原左侧空出区域可操作地图；三入口、跨波、暂停、重试、胜利/失败、布局锁定与返回；宽/窄屏截图。 |
| `BrickDefense.Editor.SoftStrategyUiChecks.Verify` | 退出 0，`SOFT_UI_CHECKS_PASSED` | 两个示例、缩放后的出生点点击、照片打开/真实取色/保存标定、完整实拍识别 10 块、确认生成、裁切拒绝保留地图；照片原色和不透明背景、窗口缩放、窄屏滚动与返回。 |

中文字体通过 Resources 实际加载，动态字体数据随包保留；按四个界面源码中出现的中文字符逐字检查，共 **374** 个均有字形。字体资源的 SHA-256 与来源记录一致，完整 OFL 和 Unity `.meta` 均已保留。

两个日志在进入 Play 之前均出现既有 `UnityEditor.Search.SearchDatabase` 启动索引异常，堆栈全部位于编辑器搜索服务；实际项目编译、运行检查与截图均完成，未出现项目脚本异常。

## 截图核对

实测地图/战斗为 1280×800 和 540×800；照片流程为 1280×801、862×679、540×801（Game View 边框导致高度相差 1 像素）。已检查中文字形、关键按钮和说明显示，无缺字或关键操作裁切。

- [宽屏地图和右侧开始入口](https://github.com/hanyang-sun/Brick2life/blob/ab7df00acc5a633d5d33d4ce2702a381b4458085/.scratch/chinese-ui/verification/multiple-starts-layout-wide.png)
- [中文参数弹窗](https://github.com/hanyang-sun/Brick2life/blob/ab7df00acc5a633d5d33d4ce2702a381b4458085/.scratch/chinese-ui/verification/battle-setup-wide.png)
- [宽屏战斗](https://github.com/hanyang-sun/Brick2life/blob/ab7df00acc5a633d5d33d4ce2702a381b4458085/.scratch/chinese-ui/verification/battle-active-wide.png) / [下一波](https://github.com/hanyang-sun/Brick2life/blob/ab7df00acc5a633d5d33d4ce2702a381b4458085/.scratch/chinese-ui/verification/battle-next-wave-wide.png) / [胜利](https://github.com/hanyang-sun/Brick2life/blob/ab7df00acc5a633d5d33d4ce2702a381b4458085/.scratch/chinese-ui/verification/battle-victory-wide.png)
- [竖屏地图](https://github.com/hanyang-sun/Brick2life/blob/ab7df00acc5a633d5d33d4ce2702a381b4458085/.scratch/chinese-ui/verification/multiple-starts-layout-narrow.png) / [多入口战斗](https://github.com/hanyang-sun/Brick2life/blob/ab7df00acc5a633d5d33d4ce2702a381b4458085/.scratch/chinese-ui/verification/multiple-starts-active-narrow.png) / [暂停](https://github.com/hanyang-sun/Brick2life/blob/ab7df00acc5a633d5d33d4ce2702a381b4458085/.scratch/chinese-ui/verification/battle-active-narrow.png) / [失败结算](https://github.com/hanyang-sun/Brick2life/blob/ab7df00acc5a633d5d33d4ce2702a381b4458085/.scratch/chinese-ui/verification/battle-defeat-narrow.png)
- [照片入口竖屏](https://github.com/hanyang-sun/Brick2life/blob/ab7df00acc5a633d5d33d4ce2702a381b4458085/.scratch/chinese-ui/verification/photo-entry-narrow.png) / [完整照片成功](https://github.com/hanyang-sun/Brick2life/blob/ab7df00acc5a633d5d33d4ce2702a381b4458085/.scratch/chinese-ui/verification/photo-success.png) / [裁切拒绝](https://github.com/hanyang-sun/Brick2life/blob/ab7df00acc5a633d5d33d4ce2702a381b4458085/.scratch/chinese-ui/verification/photo-rejected.png)
- [较小窗口照片页](https://github.com/hanyang-sun/Brick2life/blob/ab7df00acc5a633d5d33d4ce2702a381b4458085/.scratch/chinese-ui/verification/photo-small.png) / [竖屏顶部](https://github.com/hanyang-sun/Brick2life/blob/ab7df00acc5a633d5d33d4ce2702a381b4458085/.scratch/chinese-ui/verification/photo-narrow-top.png) / [竖屏滚动到底部](https://github.com/hanyang-sun/Brick2life/blob/ab7df00acc5a633d5d33d4ce2702a381b4458085/.scratch/chinese-ui/verification/photo-narrow-bottom.png)

右侧卡片的绘制与命中测试共享矩形，地图输入跳过同一区域；参数弹窗保留居中表单。以上为 Windows 编辑器检查；Android 构建模块缺失，未据此声称已完成手机硬件验收。
