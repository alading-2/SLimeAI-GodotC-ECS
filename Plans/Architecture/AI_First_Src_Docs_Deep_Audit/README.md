# AI-First Src 文档完全迁移

将 `Src/**/*.md` 全量删除，文档体系简化为 DocsAI + Docs/ + 项目索引三层结构。

## 结果

- 删除 40 个 `Src/**/*.md` 文件（保留 `InputManager.md` 自动生成文件）
- Src 文档从 ~7300 行降到 41 行
- 无规则丢失，独特内容已迁入 DocsAI/Modules/
- DocsAI/Docs/ 交叉引用全部更新
- .claude/skills/ 引用全部更新

## 最终架构

```
AI 入口: Skill → DocsAI/Modules/X.md → .cs 源码
人类入口: Docs/README.md → 项目索引 → Docs/ 设计文档 或 .cs 源码
Src/: 只有 .cs 代码 + InputManager.md
```
