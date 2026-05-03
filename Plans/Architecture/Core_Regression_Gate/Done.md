# Done - ECS 核心回归与审查门禁

## 完成内容

建立 ECS 核心修改审查门禁，包括核心高风险区定义、修改前检查清单、回归测试矩阵、Skill 门禁接入。

## 修改文件

- `DocsAI/Workflows/ECS核心修改门禁.md` — 增强：添加风险等级表、具体文件路径、门禁豁免规则、`.claude` 路径
- `DocsAI/Tests/测试矩阵.md` — 补充 LifecycleComponent 映射、添加 `.claude` 路径到所有测试命令
- `.claude/skills/ecs-entity/SKILL.md` — 验证命令更新为 `.claude` 路径
- `.claude/skills/ecs-component/SKILL.md` — 验证命令更新为 `.claude` 路径
- `.claude/skills/ecs-event/SKILL.md` — 验证命令更新为 `.claude` 路径
- `.claude/skills/tools/SKILL.md` — 验证命令更新为 `.claude` 路径
- `Plans/Architecture/Core_Regression_Gate/README.md`（新增）
- `Plans/Architecture/Core_Regression_Gate/Progress.md`（新增）
- `Plans/Architecture/Core_Regression_Gate/Done.md`（新增）
- `Plans/Architecture/Core_Regression_Gate/Backlog.md`（新增）

## 验证命令

```bash
dotnet build
node .claude/skills/GodotSkill/scripts/godot-scene-runner.mjs list
```

## 验证结果

- `dotnet build` 通过，0 错误 0 警告
- 可用测试场景 18 个
- Skill 门禁引用完整：`.claude/skills/*/SKILL.md` 全部链接到 `DocsAI/Workflows/ECS核心修改门禁.md`

## 风险点

- `.codex/skills/` 的验证命令更新由用户自行维护
- 测试场景大部分为手动/交互式，CLI 测试会 timeout（已知限制）
- 门禁文档需要随核心代码变更同步维护
