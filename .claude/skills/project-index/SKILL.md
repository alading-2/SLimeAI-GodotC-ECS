---
name: project-index
description: 查找项目任意模块的文档、源码、模板文件时使用。当需要了解项目整体架构、定位某个系统的实现文件、查找设计文档或 API 手册时自动触发。这是 Godot C# ECS 项目的导航入口。
---

# 项目导航入口

## 职责

- 定位项目文档、源码、模板文件和测试场景。
- 不承载完整模块知识。
- 不替代具体 Skill。

## 必读入口

- AI 导航：`DocsAI/INDEX.md`
- 人类文档：`Docs/README.md`
- 完整框架索引：`Docs/框架/项目索引.md`
- 当前迁移状态：`DocsAI/ProjectState.md`
- Skill 映射：`DocsAI/Skills/Skill到DocsAI映射.md`

## 查找规则

- 找模块位置：先看 `Docs/框架/项目索引.md`。
- 找 AI 执行流程：先看 `DocsAI/INDEX.md`。
- 找测试场景：先看 `DocsAI/Tests/测试矩阵.md`。
- 找 Skill 分工：先看 `DocsAI/Skills/Skill到DocsAI映射.md`。
- 找源码实现：用 `rg` / `find` 搜索 `Src/`、`Data/`、`addons/`。

## 常用命令

```bash
rg -n "<关键词>" Docs DocsAI Src Data .codex/skills
find Src -name "*.cs" | sort
find DocsAI -maxdepth 3 -type f | sort
```

## 禁止事项

- 不要把项目索引内容复制回本 Skill。
- 不要使用机器绝对路径。
- 不要在本 Skill 里维护模块规则；模块规则放 `DocsAI/Modules/` 或具体 Skill。

