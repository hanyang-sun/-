# 文档导航

更新：2026-11-07。现行说明对应 Brick2Life-C 当前本地工作树（Unity 6000.3.24f1、Built-in、10×10 单层），不代表未提交改动已进入 GitHub。运行入口见 [项目 README](../README.md)。

## 当前使用与实现

| 文档 | 用途 |
| --- | --- |
| [架构与状态归属](architecture.md) | 模块依赖、确认合同、B/C 调用链 |
| [B 识别手册](task-b-recognition-guide.md) | 两种模式、固定采样、颜色恢复、稳定确认及限制 |
| [C 反馈手册](task-c-feedback-guide.md) | 关卡映射、投影生成、安装与验收入口 |
| [固定投影仪](physical-projector-guide.md) | 表面光路、识别输入、参数及暂停操作 |
| [投影贴合](projection-alignment-guide.md) | 已实现的几何校正、图像测量和硬件边界 |
| [实时编辑](live-battle-editing-guide.md) | 新确认布局、局部敌人脱离和全局堵路回退 |
| [系统目标玩法](generated-defense-round-guide.md) | 核心、多个入口、建造与重试规则 |
| [编辑器双窗口](editor-dual-window-guide.md) | 干净搭建视图、只读确认布局及显示模式 |
| [相机元数据](task-a-camera-metadata-guide.md) / [光照](task-a-lighting-guide.md) | 采集与公开标定配置；最新曝光与投影输入以固定投影仪手册为准 |
| [日常数据集](task-a-dataset-generation-guide.md) | 有限布局与光照样本生成；不等同于已完成投影干扰基准实验 |
| [逻辑 CI](logic-ci-guide.md) | 本地检查、Actions 范围与合并门禁 |
| [最新验证摘要](verification/current-simulation.md) | 最后一次本地实跑结果与尚未验证范围 |

## 历史与研究

- [历史归档](history/README.md)：旧交接、初始 B、A/C 整合、撤回计划及早期验收。
- [研究记录](research/README.md)：编码前选型和阶段实施过程；历史参数不作为现行配置。
- [验证资料与保存约定](verification/README.md)：区分当前复测入口、历史证据和本地临时产物。

发生冲突时，先对照当前源码与当前手册；不要按旧日期记录恢复已删除的快捷键、周期性消隐 API 或三维角色替身。
