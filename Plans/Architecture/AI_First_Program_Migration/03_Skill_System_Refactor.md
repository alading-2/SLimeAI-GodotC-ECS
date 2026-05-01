# 计划 03：Skill 体系重构

## 目标

把项目 Skill 从“长篇知识文档”调整为“任务触发入口”：

- Skill 负责：触发条件、适用边界、必须读的 DocsAI、执行步骤、验证命令、禁止事项。
- DocsAI 负责：模块契约、详细语义、踩坑记录、测试矩阵、长任务上下文。
- scripts 负责：重复且容易出错的命令流程。

这样可以降低上下文占用，让不同 AI 工具也能复用 `DocsAI`。

## 输入文件

- `.codex/skills/*/SKILL.md`
- `.codex/skills/ability-system/references/ability-logic-parameters.md`
- `Docs/框架/项目索引.md`
- `DocsAI/INDEX.md`

## 修改范围

- 优先处理：
  - `.codex/skills/project-index/SKILL.md`
  - `.codex/skills/godot-scene-test/SKILL.md`
  - `.codex/skills/ability-system/SKILL.md`
  - `.codex/skills/ecs-component/SKILL.md`
  - `.codex/skills/test-system/SKILL.md`
- 新增 `DocsAI/Skills/Skill设计规则.md`
- 新增 `DocsAI/Skills/Skill到DocsAI映射.md`

## 执行步骤

1. 统计 Skill 行数和引用路径：
   - `find .codex/skills -maxdepth 2 -name SKILL.md -print | sort | xargs wc -l`
   - `rg -n "Docs/|Src/|references/|/mnt/e|file:///" .codex/skills`
2. 定义 Skill 标准模板：
   - 什么时候触发
   - 先读哪些 DocsAI
   - 允许改什么
   - 禁止改什么
   - 必须跑什么验证
   - 最终输出必须包含什么
3. 将长篇模块语义迁移到 `DocsAI/Modules/` 或 `DocsAI/Tests/`。
4. Skill 内只保留短流程和链接。
5. 清理旧绝对路径和不存在路径。
6. 更新 `Skill到DocsAI映射.md`，保证 AI 能从 Skill 找到详细文档。

## 验证命令

```bash
find .codex/skills -maxdepth 2 -name SKILL.md -print | sort | xargs wc -l
rg -n "/mnt/e|file:///|复刻土豆兄弟" .codex/skills
rg -n "DocsAI/|Skill到DocsAI映射|Skill设计规则" .codex/skills DocsAI
```

## 验收标准

- 每个核心 Skill 都有明确触发场景和任务边界。
- 每个核心 Skill 都指向对应 DocsAI 详细文档。
- `SKILL.md` 不再承载大量历史解释。
- 没有旧机器绝对路径。
- Skill 和 DocsAI 没有互相复制大段重复内容。

## 风险点

- Skill 过短会导致 AI 不知道下一步，因此必须保留最小执行流程。
- DocsAI 太分散会增加查找成本，因此必须维护 `DocsAI/INDEX.md`。
- 不要一次性重写所有 Skill；优先重构高频 Skill。

## 完成输出

最终回复必须包含：

- 已重构 Skill 列表
- 每个 Skill 对应的 DocsAI 文档
- 未处理 Skill 和原因
- 路径清理验证结果

