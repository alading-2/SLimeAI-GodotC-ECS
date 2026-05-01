# 计划 06：AI-First 功能开发闭环试点

## 目标

选择一个小型功能，让 AI 按新文档、新 Skill、新测试流程独立完成完整开发闭环：

1. 读文档
2. 输出任务计划
3. 小步实现
4. 构建验证
5. 运行 Godot 场景测试
6. 读取日志并修复
7. 更新文档和 Skill
8. 输出风险点和人工审查重点

试点目标不是功能复杂，而是验证工程流程是否真正减少人工重复劳动。

## 推荐试点功能

优先选择低风险、低耦合、可测试的小功能：

- `LifecycleComponent + MaxLifeTime` 复验：项目已有生命周期组件，验证到期自动死亡 / 销毁链路，不新增重复 `LifetimeComponent`。
- `MovementDebugSystem`：输出选中实体运动状态，用于调试。
- `SimplePickupComponent` 扩展：补充拾取事件和测试。

默认推荐：`LifecycleComponent + MaxLifeTime` 复验。

原因：

- 不需要改 ECS 核心。
- 生命周期边界清晰。
- 可写场景测试。
- 能验证 EntityManager.Destroy、TimerManager、Event、Data 文档是否足够。
- 原始代码已经存在 `Src/ECS/Base/Component/Unit/Common/LifecycleComponent/LifecycleComponent.cs` 和 `DataKey.MaxLifeTime`，新增 `LifetimeComponent` 会重复造轮子。

## 输入文件

- `DocsAI/README.md`
- `DocsAI/Workflows/AI开发闭环.md`
- `DocsAI/Modules/Entity.md`
- `DocsAI/Modules/Component.md`
- `DocsAI/Modules/Event.md`
- `DocsAI/Tests/Godot场景测试.md`
- `.codex/skills/ecs-component/SKILL.md`
- `.codex/skills/godot-scene-test/SKILL.md`

## 修改范围

以 `LifecycleComponent + MaxLifeTime` 复验为例：

- 优先不修改 Component 源码，先补测试或复用现有测试实体
- 如发现缺口，再最小修改 `LifecycleComponent`
- 新增测试场景或测试脚本
- 更新对应 DocsAI 模块契约
- 更新项目索引
- 必要时更新 Skill

## 执行步骤

1. 执行前先读相关 DocsAI 和 Skill。
2. 明确功能输入、输出、生命周期和测试方式。
3. 检查是否已有类似组件，避免重复造轮子。
4. 小步实现，不改 ECS 核心。
5. 运行 `dotnet build`。
6. 运行对应 Godot 测试场景。
7. 根据日志修复问题。
8. 更新 DocsAI / Docs / Skill。
9. 输出人工审查重点。

## 验证命令

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list --filter Life
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run <新增测试场景> --build
```

## 验收标准

- 功能实现完成。
- 构建通过。
- 场景测试通过。
- AI 能从日志中定位并修复失败。
- 文档和 Skill 同步更新。
- 最终输出包含风险点和人工审查重点。

## 风险点

- 试点功能不能选太大，否则无法判断是流程问题还是功能复杂度问题。
- 不要为了试点改 ECS 核心。
- 不要把测试写成只打印成功，必须有失败条件。

## 完成输出

最终回复必须包含：

- 功能实现文件
- 测试文件 / 场景
- 验证命令和结果
- 文档更新位置
- 本次流程暴露的问题
