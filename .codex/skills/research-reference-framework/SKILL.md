---
name: research-reference-framework
description: 研究成熟开源框架、游戏项目或 Godot 底层源码时使用。适用于架构重构、复杂系统设计、底层 Debug、需要广泛搜索、需要 clone 仓库阅读源码、或 AI 多轮 Debug 无法收敛的任务。
---

# Reference Framework Research

## 职责

- 搜索外部资料并 clone 源码到临时目录。
- 用源码、测试、examples、benchmarks 验证设计判断。
- 输出可落地到本项目的 ResearchBrief。
- 不引入依赖，不直接照搬外部目录结构。

## 必读

- `DocsAI/Protocols/外部资料与源码研究协议.md`
- `DocsAI/Protocols/AI表现复盘协议.md`（Debug 多轮失败时）
- 当前任务对应的 `DocsAI/Modules/*` 或迁移计划。

## 推荐命令

```bash
mkdir -p /tmp/brotato-research
git clone --depth 1 <repo-url> /tmp/brotato-research/<name>
find /tmp/brotato-research/<name> -maxdepth 3 -type d | sort
rg -n "<keyword>" /tmp/brotato-research/<name>
```

Godot 底层源码优先读用户在任务上下文提供的本地 Godot 源码路径：

```bash
find <godot-source-path> -maxdepth 3 -type d | sort
rg -n "<keyword>" <godot-source-path>
```

## 执行步骤

1. 明确研究问题和候选项目。
2. 优先查官方文档或 issue，确认关键词。
3. clone 仓库到 `/tmp`，不要放进项目目录。
4. 阅读目录、核心源码、测试和 examples。
5. 写出可借鉴点、不能照搬点和本项目决策。
6. 更新当前计划、DocsAI 或经验库。
7. 最终回复说明 clone 了什么、读了什么、结论如何落地。

## 禁止事项

- 禁止只读文档不看源码就下框架级结论。
- 禁止把外部框架复杂度原样搬进项目。
- 禁止新增依赖或 vendor 外部代码，除非用户明确要求。
- 禁止把研究产物只留在聊天上下文。
