# Windows x64 独立程序验收

日期：2026-10-12。Unity 6000.3.24f1，Mono，非开发构建，1280×800 窗口。源基线 `b5607ac` 加当前工作区修改。

## 结果

使用真实打包的 `BrickDefense.exe`（不是 Unity Game 视图）运行并操作。Windows 测试辅助脚本仅向该进程的 `UnityWndClass` 窗口发送消息，截图限定为此窗口的客户区；不会向其他程序输入。辅助脚本位于 `.scratch/windows-release/Smoke-Windows.ps1`，不包含在成品程序中。

| 检查 | 结果与证据 |
| --- | --- |
| 构建 | `../build-fixed.log`：`Build Finished, Result: Success.`，Unity 正常以 0 退出 |
| 启动、材质与中文 | `01-start.png`：9 块示例积木、中文 UI、正交桌游视角正常 |
| 独立程序扫描入口 | `02-photo-page.png`：真实 Player 专属的本地路径输入框可用 |
| 中文路径导入 JPG | `03-imported-photo.png`：通过包内示例的中文完整路径导入原始照片 |
| 背景与三种颜色采样、分析 | `04-analyzed-photo.png`：实际点击照片取色、保存颜色，识别为 10 块 |
| 实物校对入口 | `05-physical-review.png`：原图、10×10 草稿、编辑工具、确认框可见 |
| 玩家确认与指标 | `06-design.png`：确认实物后进入设计阶段，本样本两项均 10/10；这仅是一个人工核验样本，不是通用准确率 |
| 三维草稿 | `07-3d-preview.png`：可显示未发布的 10 块草稿 |
| 关卡发布 | `08-published-map.png`：主地图变为实拍的 10 块，路线有效 |
| 战斗入口与配置 | `09-battle-config.png`：一波、8 敌人、24 生命、1.4 格/秒、伤害 10、间隔 1.1 秒 |
| 实际出怪 | `10-battle.png`：敌人、血条、战斗状态与出怪计数可见 |
| 实际结算 | `11-result.png`：防守成功，1/1 波，击杀 8，抵达核心 0，核心 100/100 |
| 记录落盘 | 本次生成 `scan-ccb7d7737f774c4b9dc6ccc0a9dd2f24`；原始/真值/设计各 10 块、`published=true`、两指标 1.0，原结果与校对 JSON 均存在 |
| 运行时异常 | `player-fixed.log` 未出现 Exception、NullReference、ArgumentNull 或 shader 缺失 |
| 压缩包完整性 | ZIP 解压到包含中文与空格的全新目录；159 个清单文件逐个 SHA-256 校验通过，ZIP 本身与随附 checksum 一致 |
| 解压版启动 | `12-unpacked-start.png`：从新目录启动 EXE 后显示完整中文 UI 与 9 块示例；`player-unpacked.log` 无运行时异常，测试后正常关闭 |

## 修复及测试辅助说明

首次真实 Player 运行出现 Standard shader 被裁剪导致的材质异常；添加 Resources 材质后重新构建，构建日志明确编译 Standard，以上全部运行检查使用修复后的同一 EXE。

测试辅助首次按尺寸寻找窗口误选隐藏辅助窗口，已修正为限定 `UnityWndClass`。首次测试脚本使用 ANSI 消息导致测试输入的中文路径失真，改用 Unicode 窗口消息后正确导入；这两个问题只在辅助脚本中，没有改产品输入逻辑。脚本的早期无效截图被有效程序截图覆盖。

## 范围

本次验收为当前 Windows 电脑的完整单波流程。核心规则、编辑与异常输入检查沿用 `.scratch/layout-review/verification/README.md` 中已通过的纯 C# 和 Unity 回归，不重复执行无关测试。

未覆盖摄像头硬件、其他 Windows 电脑或 Android 真机。原始标定与校对日志副本仅保存到被忽略的 `ScanReports/`，不随程序分发玩家扫描记录。
