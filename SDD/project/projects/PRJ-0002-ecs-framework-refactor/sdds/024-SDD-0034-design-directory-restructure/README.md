# SDD-0034 Design Directory Restructure

## Index Card

- **Status**: done
- **Created**: 2026-06-07
- **Updated**: 2026-06-07
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - sdd
- **Tags**: directory, restructure

## What This SDD Is About

精简 `PRJ-0002` 项目目录结构：

1. **Core/** — 将项目根的管理文件（roadmap、progress、notes）和执行提示词移入 `Core/`
2. **design/Runtime/** — 将 `design/` 顶层的编号设计目录（1-8）和 ECS框架优化 移入 `Runtime/`
3. **design/Foundation/** — 将散落的早期分析文档（00、01、03、04、06）移入新建的 `Foundation/`
4. 更新所有路径引用

## Target Structure

```
PRJ-0002/
├── README.md
├── project.json
├── Core/
│   ├── roadmap.md
│   ├── progress.md
│   ├── notes.md
│   ├── data-rewrite-execution-prompt.md
│   ├── directory-architecture-restructure-execution-prompt.md
│   └── entity-rewrite-execution-prompt.md
├── design/
│   ├── INDEX.md
│   ├── main.md
│   ├── Runtime/
│   │   ├── 1.Event系统优化/
│   │   ├── 2.Data系统优化/
│   │   ├── 3.Entity系统优化/
│   │   ├── 4.SystemAgent目录更改到SlimeAI里面/
│   │   ├── 5.文档目录/
│   │   ├── 6.ECS框架目录架构大重构/
│   │   ├── 7.Component/
│   │   ├── 8.System优化/
│   │   └── ECS框架优化/
│   ├── Foundation/
│   │   ├── 00-旧ECS框架问题总览.md
│   │   ├── 01-Data系统问题分析.md
│   │   ├── 03-字符串键名统一问题分析.md
│   │   ├── 04-优化优先级与SDD拆分建议.md
│   │   └── 06-ECS完全重构执行原则.md
│   └── Tool/
└── sdds/
```

## Reading Order

1. 本 README
2. `tasks.md` — 执行清单
