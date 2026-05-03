# AI-First 功能开发闭环试点

## 目标

选择 LifecycleComponent + MaxLifeTime 作为小功能复验，验证 AI 是否能按新 DocsAI、Skill、测试流程独立完成完整开发闭环。

## 选择理由

- 不需要改 ECS 核心
- 生命周期边界清晰
- 可写场景测试
- 能验证 EntityManager.Destroy、TimerManager、Event、Data 文档是否足够
- 原代码已存在 `LifecycleComponent.cs`

## 成果

复验发现 LifecycleComponent 实现完整，覆盖了：
- 状态机：Alive → Dead → Reviving → Alive
- 死亡类型：Normal / Hero / Instant / Summon
- Timer 管理：LifeTimer / ReviveTimer / DeathLingerTimer
- 事件驱动：Unit.Killed / Unit.StateChanged / Unit.Reviving / Unit.Revived
- 动画联动：OnAnimationFinished → 延迟销毁或启动复活
- 数据驱动：MaxLifeTime / IsDead / DeathType / DeathCount

无缺口，无需新增代码。

## 闭环验证

1. 读 DocsAI 模块 → 理解契约
2. 读源码 → 验证实现一致
3. 运行测试 → ECSTestScene + SystemCoreRuntimeTest 通过
4. 构建验证 → dotnet build 0 错误
5. 文档同步 → 更新测试矩阵，补充 LifecycleComponent 入口
