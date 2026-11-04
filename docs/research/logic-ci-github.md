# 纯逻辑 CI：已有实现比较

日期：2026-11-04。范围：为现有控制台检查接入推送、PR 和手动触发的 GitHub Actions，不改动游戏规则、识别算法或测试框架。

## 检索与实际阅读

检索词：`actions/setup-dotnet dotnet 10 workflow`、`actions/upload-artifact logs always`、Unity test runner。检索 GitHub 及其官方文档，并通过 GitHub 连接器阅读以下仓库文件。

| 候选 | 阅读文件 | 授权 | 匹配与取舍 |
| --- | --- | --- | --- |
| [actions/setup-dotnet](https://github.com/actions/setup-dotnet/tree/v5) | `README.md`、`action.yml`、`src/setup-dotnet.ts`、`LICENSE` | MIT | 适合当前 `net10.0` 控制台入口。采用其 SDK 安装能力，运行仓库自己的脚本；无需 Unity。该 action 的 `global.json` 支持和缓存条件已核对；本项目无额外包依赖，不启用 NuGet 缓存。 |
| [game-ci/unity-test-runner](https://github.com/game-ci/unity-test-runner/tree/v4) | `README.md`、`action.yml`、`LICENSE` | MIT | 面向 Unity 项目和 Unity 测试运行器，涉及编辑器、容器和授权条件；不能直接替代现有控制台断言入口。本轮不引入，后续 Unity CI 单独设计。 |

辅助 action 已阅读 [checkout v5 的 action.yml](https://github.com/actions/checkout/blob/v5/action.yml) 与 [LICENSE](https://github.com/actions/checkout/blob/v5/LICENSE)、[upload-artifact v4 的 action.yml](https://github.com/actions/upload-artifact/blob/v4/action.yml) 与 [LICENSE](https://github.com/actions/upload-artifact/blob/v4/LICENSE)。采用签出和日志附件能力，不复制或改写 action 源码，使用范围见第三方声明。

上述 action 仅在 GitHub 执行，不进入 Unity 程序、Android 包或离线游戏运行链。纯 C# 源码继续由本项目编译；本轮不要求 Unity 授权密钥。

## 适配方案与验收

1. 保留 `Tests/Program.cs` 的 12 组检查，用 `Tests/RunLogicChecks.ps1` 统一执行 Release 配置并记录输出。
2. `global.json` 限制 SDK 为稳定的 .NET 10.0 系列，允许该系列中的 SDK 更新，避免 runner 预装新主版本时改变选择。
3. GitHub hosted Ubuntu runner 使用 `pwsh` 调用同一入口；工作流只需 `contents: read`，不配置仓库写入或发布操作。
4. 推送和 PR 不设路径过滤，避免必须检查被跳过；允许手动触发，同一事件和分支的新运行取消旧运行。稳定检查名称为 `Logic checks`。
5. 保留失败退出码；退出码为 0 时还应出现 `BRICK2LIFE_CHECKS_PASSED`，否则按失败处理。生成的日志在运行后尝试上传，保留 14 天。
6. 本地执行实际的 Release 检查；用隔离的小程序分别验证非零退出和缺少完成标志会失败，并校验 YAML 与 PowerShell 语法。

本地验证不能代替 GitHub 首次运行。工作流提交到远端后才生效；是否强制通过后才能合并由仓库分支规则决定。当前阶段没有覆盖率数据，也不把 1076 个断言称为 1076 个独立测试用例。
