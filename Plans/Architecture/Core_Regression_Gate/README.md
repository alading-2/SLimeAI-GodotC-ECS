# ECS 核心回归与审查门禁

## 目标

建立 ECS 核心修改的审查门禁，避免 AI 在开发功能时无意破坏底层生命周期、对象池、事件和系统调度。

## 完成内容

### 核心高风险区定义

已在 `DocsAI/Workflows/ECS核心修改门禁.md` 中详细定义：
- 🔴 极高：Entity Core、Event、SystemCore、ObjectPool
- 🟡 高：TimerManager、ResourceManagement、IComponent 接口

### 修改前检查清单

每次触碰核心必须回答：
1. 为什么必须改核心？
2. 有没有替代方案？
3. 影响哪些模块？
4. 对象池/碰撞/事件安全？
5. 如何回滚？

### 回归测试矩阵

ECS 核心回归：ECSTestScene + SystemCoreRuntimeTest + ObjectPoolManagerTest
Gameplay 回归：DamageSystemTest + MovementCollisionRuntimeTest + SpawnTestScene + AbilitySystemPipelineTest

### Skill 门禁接入

4 个 ECS Skills 的"必读"段已链接 `ECS核心修改门禁.md`，验证命令已更新为 `.claude` 路径：
- `ecs-entity/SKILL.md`
- `ecs-component/SKILL.md`
- `ecs-event/SKILL.md`
- `tools/SKILL.md`

### 测试矩阵更新

- 已补充 LifecycleComponent 测试映射
- 已添加 `.claude/skills/GodotSkill/` 路径到所有测试命令

## 修改文件

- `DocsAI/Workflows/ECS核心修改门禁.md`（增强）
- `DocsAI/Tests/测试矩阵.md`（更新）
- `.claude/skills/ecs-entity/SKILL.md`（更新验证命令）
- `.claude/skills/ecs-component/SKILL.md`（更新验证命令）
- `.claude/skills/ecs-event/SKILL.md`（更新验证命令）
- `.claude/skills/tools/SKILL.md`（更新验证命令）
