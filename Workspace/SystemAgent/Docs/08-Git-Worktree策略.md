# Git Worktree 策略

## 当前状态

**策略已写，从未使用。**

SDD-0010 建立了 git worktree 和 subagent 安全策略，但仓库当前只有一个工作树（`main` 分支），没有任何 `.worktrees/` 目录存在。

## 策略要点

### Git 边界

```
/home/slime/Code/SlimeAI/          ← 工作区根（AI 配置 / DocsAI / SDD）
/home/slime/Code/SlimeAI/SlimeAI/  ← 框架仓（独立 Git 边界）
/home/slime/Code/SlimeAI/Games/*/  ← 游戏仓（独立 Git 边界）
```

每个仓库管理自己的 worktree，位于 `<repo>/.worktrees/`。

### Worktree 检查清单

创建 worktree 前必须确认：
1. 目标仓库是真正的 Git 边界
2. 当前目录不是只读 submodule 镜像
3. 目标仓库的 `.gitignore` 包含 `.worktrees/`
4. 主工作区脏状态已记录，不会被清理/覆盖/回滚
5. 如果已在链接的 worktree 中，继续使用它

### 约束

- 脏 worktree 永远不自动清理
- 干净的 worktree 可以在验证后建议移除
- `Games/<Game>/SlimeAI/` submodule 镜像内禁止创建 worktree

## 多对话场景的问题

### 你的使用模式

你经常多开对话（多个 Claude Code/Codex 窗口），每个对话改不同部分。问题：

1. **所有对话都在同一个目录**（`~/Code/SlimeAI/SlimeAI`）
2. **改动混在一起**：对话 A 改了 Logger，对话 B 改了 Entity，最终 `git diff` 混在一起
3. **提交困难**：不能一次性提交所有改动，需要手动分拣

### Worktree 如何解决

```
~/Code/SlimeAI/SlimeAI/                    ← main（稳定基线）
~/Code/SlimeAI/SlimeAI/.worktrees/logger/  ← Logger 改动隔离
~/Code/SlimeAI/SlimeAI/.worktrees/entity/  ← Entity 改动隔离
~/Code/SlimeAI/SlimeAI/.worktrees/data/    ← Data 改动隔离
```

每个对话在一个独立的 worktree 中工作，互不干扰。完成后合并到 main。

### 核心问题：多个对话怎么知道自己改哪个 worktree？

这是你提出的关键问题。有几种解决方案：

#### 方案 1：手动分配（最简单）

对话开始时，用户明确告诉 AI："这个对话在 logger worktree 中工作"。AI 在对话开始时 `cd` 到对应 worktree 目录。

**优点**：简单、确定
**缺点**：需要用户记忆和手动分配

#### 方案 2：Skill 自动创建（推荐）

创建一个 `systemagent-worktree` skill：
- 用户说"在 worktree 中做 Logger 改动"
- Skill 自动创建 `.worktrees/logger-<date>` worktree
- Skill 在 SDD progress.md 记录 worktree 路径
- 后续操作自动在该 worktree 中执行

**优点**：自动化、有记录
**缺点**：需要实现 skill

#### 方案 3：SDD 驱动（最完整）

每个 SDD 任务自动关联一个 worktree：
- `sdd start <sdd-id>` 时自动创建 worktree
- `sdd done <sdd-id>` 时自动合并到 main 并清理
- progress.md 自动记录 worktree 信息

**优点**：与 SDD 完全集成
**缺点**：实现复杂，需要改 SDD CLI

#### 方案 4：不默认打开，用 prompt 语义触发（你的倾向）

不默认使用 worktree，但在特定场景下通过 skill 触发：
- "帮我隔离这个改动" → 创建 worktree
- "这个任务需要单独分支" → 创建 worktree
- "合并 worktree 到 main" → 合并并清理

**优点**：不增加日常开销，需要时才用
**缺点**：依赖 AI 正确识别场景

### 推荐方案

**方案 2 + 方案 4 的结合**：

1. 创建 `systemagent-worktree` skill，作为 worktree 操作的入口
2. 不默认打开 worktree，需要用户或 AI 明确触发
3. Skill 管理创建、切换、合并、清理的完整生命周期
4. 与 SDD progress.md 集成，记录 worktree 使用情况
5. 在 Git.md 规则中添加触发建议（何时应该建议使用 worktree）

### 触发建议规则

| 场景 | 建议 |
|------|------|
| 单文件小修复 | 不需要 worktree |
| 单模块改动（< 5 文件） | 不需要 worktree |
| 跨模块改动 | 建议 worktree |
| 实验性改动 | 建议 worktree |
| 与其他对话的改动可能冲突 | 强烈建议 worktree |
| 用户明确要求 | 创建 worktree |

## 未完成的部分

1. **没有 WorktreeCreate/WorktreeRemove hook**：settings.json 中没有配置
2. **没有 worktree skill**：没有可用的 skill 入口
3. **SDD CLI 不支持 worktree**：没有 `sdd start --worktree` 选项
4. **没有自动清理机制**：需要手动 `git worktree remove`
5. **没有 worktree 状态查看工具**：需要手动 `git worktree list`
