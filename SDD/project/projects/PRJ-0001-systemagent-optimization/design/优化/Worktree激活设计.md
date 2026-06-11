# Worktree 激活设计

> 来源：SystemAgent 深度分析
> 日期：2026-06-11
> 优先级：P1

## 问题陈述

Git Worktree 策略已写（SDD-0010），但从未实际使用。用户经常多开对话，改的地方混在一起，提交时需要手动分拣。

**核心问题**：多个对话都在同一个目录工作，改动无法隔离。

## 设计原则

1. **不默认打开**：worktree 不是默认行为，只在需要时触发
2. **Prompt 语义触发**：用户通过自然语言或 skill 触发
3. **SDD 集成**：worktree 使用记录在 SDD progress.md
4. **自动清理**：合并后建议清理

## Skill 设计

### 入口

`/systemagent-worktree` 或 `.ai-config/skills/systemagent-skill/systemagent-worktree/SKILL.md`

### 子命令

| 命令 | 作用 | 示例 |
|------|------|------|
| `create <name>` | 创建 worktree | "创建 logger worktree" |
| `list` | 列出所有 worktree | "列出 worktree" |
| `switch <name>` | 切换到指定 worktree | "切换到 logger worktree" |
| `merge <name>` | 合并到 main | "合并 logger worktree" |
| `clean <name>` | 清理已合并的 worktree | "清理 logger worktree" |
| `status` | 显示当前 worktree 状态 | "当前在哪个 worktree" |

### create 流程

```
1. 确认 Git 边界（当前仓库）
2. 检查是否已在 worktree 中
3. 生成 worktree 名称：<name>-<YYMMDD>
4. git worktree add .worktrees/<name>-<YYMMDD> -b <name>-<YYMMDD>
5. 在 SDD progress.md 记录 worktree 信息（6 字段）
6. 输出 worktree 路径和切换命令
```

### merge 流程

```
1. 确认 worktree 存在且有未合并的改动
2. 在 worktree 中 git add + commit（如未提交）
3. cd 回主仓库
4. git merge <worktree-branch>
5. 如有冲突，暂停并提示用户
6. 合并成功后建议清理
```

### 触发建议规则（写入 Git.md）

| 场景 | 建议 |
|------|------|
| 单文件小修复 | 不需要 worktree |
| 单模块改动（< 5 文件） | 不需要 worktree |
| 跨模块改动（≥ 2 个 owner） | 建议 worktree |
| 实验性改动（可能需要回滚） | 建议 worktree |
| 与其他对话的改动可能冲突 | 强烈建议 worktree |
| 用户明确要求隔离 | 创建 worktree |

## 多对话识别问题

### 问题

多个 Claude Code 对话都在 `~/Code/SlimeAI/SlimeAI` 目录打开。每个对话怎么知道自己应该在哪个 worktree 中工作？

### 解决方案

**方案 A：Session 标记文件**

在 `.claude/session-worktree.json` 中记录当前会话对应的 worktree：

```json
{
  "sessions": {
    "session-abc123": { "worktree": ".worktrees/logger-0611", "created": "2026-06-11T10:00:00" },
    "session-def456": { "worktree": ".worktrees/entity-0611", "created": "2026-06-11T11:00:00" }
  }
}
```

问题：session ID 不可控，且 Claude Code 不一定提供。

**方案 B：SDD 关联（推荐）**

每个 worktree 与一个 SDD 任务关联：

```
SDD-0040 (Log) → .worktrees/log-0611/
SDD-0027 (Timer) → .worktrees/timer-0611/
```

AI 在对话开始时通过 SDD 状态知道自己应该在哪个 worktree。

**方案 C：用户显式指定**

对话开始时，用户告诉 AI："这个对话在 logger worktree 中工作"。

**推荐**：方案 B + 方案 C 的结合。SDD 关联是自动的，但用户可以覆盖。

## 实施路径

| 阶段 | 内容 | 工作量 |
|------|------|--------|
| Phase 1 | 创建 worktree skill（基础 CRUD） | 1 SDD |
| Phase 2 | SDD CLI worktree 集成 | 1 SDD |
| Phase 3 | 触发建议规则 + Git.md 更新 | 规则更新 |
