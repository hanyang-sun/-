# Issue tracker: Local Markdown

本项目的问题与规格以 Markdown 文件保存在 `.scratch/`。

## 文件约定

- 每个功能使用 `.scratch/<feature-slug>/`。
- 规格文件是 `.scratch/<feature-slug>/spec.md`。
- 实施任务逐条保存在 `.scratch/<feature-slug>/issues/<NN>-<slug>.md`，从 `01` 编号。
- 每条任务顶部用 `Status:` 记录分流状态；状态名称见 `triage-labels.md`。
- 讨论记录追加在文件末尾的 `## Comments` 下。

技能要求“发布任务”时，在对应功能目录创建文件；要求“读取任务”时，读取用户指定的任务文件。

## Wayfinder 约定

- 地图保存在 `.scratch/<effort>/map.md`；子任务保存在同目录的 `issues/`。
- 子任务顶部用 `Type:` 标明 `research`、`prototype`、`grilling` 或 `task`。
- 用 `Blocked by: NN, NN` 标明依赖；只有依赖任务均为 `resolved` 时才可领取。
- 在开放、未阻塞且未领取的任务中，按编号领取第一条，先将 `Status:` 设为 `claimed`。
- 完成时在 `## Answer` 下记录结果，将 `Status:` 设为 `resolved`，并在地图的 Decisions-so-far 中追加摘要与链接。
