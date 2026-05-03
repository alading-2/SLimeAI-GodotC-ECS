# Done

## Src .md 完全删除

- 40 个 Src .md 文件通过 git rm 删除
- InputManager.md 保留（自动生成）

## DocsAI 更新

- 15 个模块契约移除 Src .md 引用（第一行和内部交叉引用）
- ProjectState.md 更新当前阶段
- INDEX.md 重构（新增计划入口、.claude 引用）
- Skill到DocsAI映射.md 更新 GodotSkill 名称
- 文档迁移协议.md 重写（反映 Src .md 已移除）
- AI开发闭环.md 更新验证清单

## Docs/ 更新

- 项目索引移除所有 Src .md 条目
- 8 个设计文档中的 Src .md 引用替换为 DocsAI 路径
- 3 个绝对路径（旧机器路径）修复
- Docs/README.md 新增 .claude 和新计划链接

## .claude/skills 更新

- 5 个 SKILL.md 移除 Src .md 引用（ecs-entity, ecs-component, ecs-data, ecs-event, test-system）

## 验证

- `dotnet build` 通过，0 错误
- `find Src -name "*.md"` 仅剩 InputManager.md
- 活跃文档零 Src .md 失效引用
