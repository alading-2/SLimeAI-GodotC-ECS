# 计划 07：ECS 核心回归与人工审查门禁

## 目标

建立 ECS 核心修改的审查门禁，避免 AI 在开发功能时无意破坏底层生命周期、对象池、事件和系统调度。

本计划不是马上重构 ECS 核心，而是定义：

- 哪些文件属于核心高风险区
- 修改前必须回答什么
- 修改后必须跑什么测试
- 最终回复必须提示人类审查什么

## 输入文件

- `Docs/框架/项目索引.md`
- `DocsAI/Modules/Entity.md`
- `DocsAI/Modules/Component.md`
- `DocsAI/Modules/Event.md`
- `DocsAI/Modules/SystemCore.md`
- `DocsAI/Tests/测试矩阵.md`
- `Src/ECS/Base/Entity/Core/`
- `Src/ECS/Base/System/Core/`
- `Src/ECS/Base/Event/`
- `Src/ECS/Tools/ObjectPool/`

## 修改范围

- 新增 `DocsAI/Workflows/ECS核心修改门禁.md`
- 更新 `DocsAI/Tests/测试矩阵.md`
- 更新 `.codex/skills/ecs-entity/SKILL.md`
- 更新 `.codex/skills/ecs-component/SKILL.md`
- 更新 `.codex/skills/ecs-event/SKILL.md`
- 更新 `.codex/skills/tools/SKILL.md`

## 执行步骤

1. 定义核心高风险区：
   - Entity 创建 / 注册 / 销毁
   - Component 注册 / 注销
   - EntityRelationshipManager
   - EventBus / GlobalEventBus
   - SystemManager / SystemRegistry
   - ObjectPool 激活 / 回收
   - TimerManager
   - ResourceManagement
2. 定义修改前问题清单：
   - 为什么必须改核心？
   - 有没有不改核心的替代方案？
   - 会影响哪些 System / Component？
   - 会影响对象池或生命周期吗？
   - 如何回滚？
3. 定义回归测试矩阵：
   - Entity 生成销毁
   - Component 注册注销
   - Event 分发
   - System 启停
   - Data 初始化与清理
   - ObjectPool 复用
   - 典型 gameplay loop
4. 把门禁链接写入相关 Skill。
5. 规定最终回复必须列出“建议人工重点审查”。

## 验证命令

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run-all --build --continue-on-fail
rg -n "ECS核心修改门禁|核心高风险区|建议人工重点审查" DocsAI .codex/skills
```

## 验收标准

- AI 在修改核心前能找到门禁文档。
- 核心修改后有明确回归测试列表。
- 最终回复能把人类审查从逐行检查转为重点审查。
- Skill 不再只说“谨慎修改”，而是给出可执行门禁。

## 风险点

- 回归测试矩阵不能一开始就过大，否则每个小任务都跑不动。
- 需要区分核心修改和普通功能修改。
- 门禁文档必须和真实测试场景保持同步。

## 完成输出

最终回复必须包含：

- 核心高风险区列表
- 回归测试矩阵
- 已接入门禁的 Skill
- 仍缺失的测试覆盖

