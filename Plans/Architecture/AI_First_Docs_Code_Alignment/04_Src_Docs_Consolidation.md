# 04 Src Docs Consolidation

## 目标

第一轮只清理明显过期路径和加新入口，不删除 `Src/**/*.md` 历史文档。

## 本轮处理

- 清理 `Src/**/*.md` 中明显旧 Windows 机器绝对路径、`file:///` 链接和项目旧名。
- 修正 `Docs/框架/项目索引.md` 中旧 `.windsurf/skills/` 入口。
- 保留源码旁长文档正文，后续按模块逐步迁到 `Docs/` 或 `DocsAI/`。

## 不做

- 不删除历史 `Src/**/*.md`。
- 不大规模重写源码旁说明。
- 不改运行时代码。

## 验收标准

```bash
rg -n "/mnt/e|file:///|复刻土豆兄弟|\\.windsurf" DocsAI .codex/skills Docs/框架/项目索引.md Src -g "*.md"
```

结果应只剩允许的历史说明或无结果；若仍有结果，写入 `Backlog.md`。
