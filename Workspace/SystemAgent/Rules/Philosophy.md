# Philosophy

> 从 AIFeatureDevelopmentProtocol 提取的独特原则。其余内容已由 NewFeature route + DeepThink + ReviewGates 覆盖。

## 基本原则

- AI 便利优先：入口少、事实源少、验证命令固定、artifact 可检查。
- 不为旧框架兼容保留新入口。旧实现只作为迁移输入；如果旧写法妨碍 AI 路由、验证或观察，按当前 GameOS 边界重构。
- 小内核，可选能力：Runtime kernel 保持少入口；功能进入 Capability、DataOS、GodotBridge 或游戏 adapter。
- 纯 Runtime / tooling 优先使用 C# 标准库和框架 API。JSON、普通文件、集合处理、时间、随机数等非引擎职责不要用 Godot helper。
- GodotBridge 只做引擎适配：`Node`、`SceneTree`、Physics、Input、Resource、可视化实例化和场景生命周期。

## 术语原则

SlimeAI 是 `AI-first GameOS / Capability Composition Runtime`，不是传统 ECS 框架。`Entity / Component / System` 命名敏感任务必须先读 `DocsAI/GameOS/Overview.md#术语表` 和 ECS 边界 ADR。
