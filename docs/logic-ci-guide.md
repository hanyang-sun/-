# 逻辑 CI 使用说明

工作流：[Logic CI](../.github/workflows/logic-checks.yml)。适配比较见 [实施前记录](research/logic-ci-github.md)。

## 自动执行范围

- 每次推送分支和每个 PR 的创建、更新、重新打开均触发检查；合并后目标分支的推送也会触发。
- 工作流进入默认分支后，可在 GitHub 的 Actions → Logic CI → Run workflow 手动运行。
- 使用 Ubuntu runner、PowerShell 和 .NET 10.0 稳定 SDK，Release 配置执行现有控制台检查。SDK 选择由根目录 `global.json` 控制。
- 检查 Core、Application、Vision、Battle 及其纯逻辑检查，当前 12 组共 1076 项断言；不是覆盖率或 1076 个独立测试用例。
- 同一事件和分支的旧运行被新运行取消；PR 与分支推送运行互不取消。

流程为签出 → 安装 SDK → 执行检查 → 上传日志。检查非零退出会使 `Logic checks` 失败；即便程序退出码为 0，缺少完成标志也会失败。未启用 `continue-on-error`。

每次运行生成的日志会尝试上传为 `logic-checks-logs`，保留 14 天。在 Actions 运行页可查看步骤输出、失败位置及下载附件。如果签出或 SDK 安装失败，可能尚未生成日志，以步骤输出为准。

## 本地使用

```powershell
pwsh -File Tests/RunLogicChecks.ps1
```

默认输出到已被忽略的 `Tests/obj/logic-checks/logic-checks.log`。可通过 `-OutputDirectory` 指定其他目录。直接运行原来的 `dotnet run --project Tests/Brick2Life.Checks.csproj` 仍然有效。

## 合并门禁

工作流提交到远端后才开始执行。本地脚本通过不代表 GitHub 已运行成功。

若要强制所有 PR 检查通过后才能合并，在首次运行产生 `Logic checks` 状态后，由仓库维护者为目标分支设置规则：要求通过 PR 合并，并将 `Logic checks` 配为必需状态检查。仅新增 YAML 不会自动开启分支保护或限制直接推送。本次没有修改远端仓库规则。

## 后续边界

Unity 编辑器检查、GPU 渲染、窗口交互及 Windows Player 构建仍由现有隔离工程脚本执行。将这些检查接入 CI 时，需要另行验证编辑器授权、图形环境及目标版本。

现有控制台检查没有标准测试报告或覆盖率数据。后续可按需要迁移测试框架或引入覆盖率采集，不影响先运行现有逻辑检查。

## 本次本地验证（2026-11-04）

- Windows、PowerShell 7、.NET SDK 10.0.401：通过新脚本运行 Release 配置，1076 项断言通过，退出码 0。
- 隔离小程序验证：即使打印完成标志，程序退出 23 时仍返回 23；无输出且日志中预存旧完成标志时仍返回 1；编译错误返回 1。三种情况均生成对应日志。
- YAML 结构、触发事件、只读权限、SDK 配置、日志上传条件及 PowerShell 语法检查通过；日志位于本机已忽略的 `.scratch/repository-hygiene/`。
- 未在 GitHub runner 或 Linux 环境实际执行；远端首次运行仍待工作流推送后确认。本次未重跑 Unity 或 Windows 构建。
