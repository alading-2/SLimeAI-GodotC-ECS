# Plan Template

## 当前目标

一句话说明本计划要完成什么。

## 当前阶段

说明阶段编号、依赖阶段和是否可独立执行。

## 输入文件

- `AGENTS.md`
- `DocsAI/README.md`
- `DocsAI/INDEX.md`
- 相关 DocsAI 模块契约
- 相关源码 / Skill / 测试场景

## 修改范围

列出允许修改的文件或目录。

## 已完成

执行中滚动更新。

## 未完成

执行中滚动更新。

## 阻塞问题

记录无法继续的原因、复现命令和关键日志摘要。

## 执行步骤

1. 读入口文档和相关契约。
2. 搜索现有实现，避免重复造轮子。
3. 制定最小改动方案。
4. 修改代码 / 文档。
5. 运行验证。
6. 更新索引、Skill 和 Done。

## 验证方式

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list
```

## 人工审查重点

- 是否违反 AGENTS 架构红线。
- 是否绕过 EntityManager / SystemManager / EventBus。
- 是否补充必要测试和文档。
