# Framework Research Filter

读取时机：新功能涉及架构边界、Runtime kernel、Capability manifest、DataOS snapshot、Event / Schedule / Relationship 或大规模重构时读取。普通小修不用加载。

## 本地证据入口

- `Resources/Engine/Docs/FrameworkAnalysis/Reports/99-SlimeAI-引擎源码综合分析报告.md`
- `Resources/Engine/Docs/FrameworkAnalysis/Reports/01-Bevy-源码分析报告.md`
- `Resources/Engine/Docs/FrameworkAnalysis/Reports/02-Flecs-源码分析报告.md`
- `Resources/Engine/Docs/FrameworkAnalysis/Reports/07-Unity-Entities-Samples-源码分析报告.md`
- `Resources/Engine/Docs/FrameworkAnalysis/Reports/09-Unreal-GAS-文档对照报告.md`
- 旧事件资料：`Resources/Else/brotato-my/Docs/框架/ECS/Event/`

## 采纳筛选

只采纳对 AI 有直接帮助的机制：

- 少入口：public facade、manifest、profile、owner skill。
- 可验证：固定测试命令、validation report、scene artifact、blocked reason。
- 可观察：dump、trace、event subscription export、handler exception 记录。
- 可路由：Capability-owned service / selector / DataKeys / Events。
- 可迁移：authoring -> validation -> snapshot -> runtime loader。

默认拒绝：

- 通用 query DSL、wildcard join、隐式 observer query。
- 运行时大核心、动态插件生命周期、复杂继承图。
- 只为兼容旧代码保留的包装 API。
- 需要 AI 记大量例外的接口。

## 外部框架摘取口径

- Bevy：采纳 plugin/profile、schedule/run condition、deferred command 的边界思想；不复制 Rust ECS 和 query 系统。
- Flecs：采纳 module scope、lifecycle relationship 与 business reference 分离、defer/merge 可观测；不开放 pair/query DSL。
- Unity Entities：采纳 authoring/baking/runtime data 分层、validation sample 和 command buffer 思路；不复制 DOTS 存储模型。
- Unreal GAS / Modular Game Features：采纳 Ability / Effect / Cue / Feature 分工和可激活能力边界；不复制 GAS 复杂 tag/attribute/replication 栈。
- QFramework：采纳单入口 API 索引、toolkit/template/example 分层；不引入全局静态 Architecture 模式。

## 网络参考入口

- Bevy 官方学习入口：https://bevyengine.org/learn/
- Flecs 官方文档：https://www.flecs.dev/flecs/md_docs_2Docs.html
- Unity Entities 官方文档：https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/index.html
- Unreal Gameplay Ability System 官方文档：https://dev.epicgames.com/documentation/en-us/unreal-engine/gameplay-ability-system-for-unreal-engine
