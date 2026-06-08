# Git 与 Worktree 策略

> 日期：2026-05-24  
> 目标：统一 SlimeAI 多 Git 边界下的 worktree 使用方式，避免污染主工作区和误跨仓提交

---

## 1. 当前问题

SlimeAI 工作区存在多个 Git 边界：

- `/home/slime/Code/SlimeAI`：工作区根，包含 AI 配置、Workspace DocsAI、OpenSpec。
- `/home/slime/Code/SlimeAI/SlimeAI`：框架仓。
- `/home/slime/Code/SlimeAI/Games/*`：游戏仓。
- `Games/BrotatoLike/SlimeAI/`：submodule，只读镜像。

当前问题是：

- 根仓长期存在未跟踪目录，`git status` 信号噪声很大。
- worktree 是否使用、什么时候使用、放在哪里没有写入统一策略。
- 跨仓任务容易混淆“当前改动属于哪个 Git 边界”。
- 现有 `refine-systemagent-workflow` 已提出 `.worktrees/`，但尚未落地到正式策略。

---

## 2. 基本原则

### 2.1 每个 Git 仓独立管理 worktree

worktree 不跨 Git 边界共享。

推荐目录：

```text
<repo>/.worktrees/<branch-or-task-name>
```

示例：

```text
/home/slime/Code/SlimeAI/.worktrees/systemagent-sdd-mvp
/home/slime/Code/SlimeAI/SlimeAI/.worktrees/gameos-capability-service-scope
/home/slime/Code/SlimeAI/Games/BrotatoLike/.worktrees/hud-refactor
```

### 2.2 `.worktrees/` 必须被当前仓忽略

每个仓如果要使用 worktree，必须先确认：

```bash
git check-ignore -q .worktrees
```

如果未忽略，应先把 `.worktrees/` 加入该仓 `.gitignore`，并单独提交该配置改动。

### 2.3 先检测是否已经在 worktree

创建前必须判断：

- 当前是否是 linked worktree。
- 当前是否在 submodule 内。
- 当前 repo 是否 dirty。

不要在已有 worktree 里再创建嵌套 worktree。

### 2.4 Dirty worktree 不自动删除

任何 cleanup 都必须保护 dirty worktree。

如果 worktree 有未提交改动，只能报告路径和状态，不能自动删除。

---

## 3. 什么时候需要 worktree

### 3.1 必须使用

- 大型重构。
- 实验性改动。
- 多文件、多阶段、需要多轮恢复的任务。
- 修改框架核心或 SystemAgent 正文，并且当前主工作区已有大量未提交改动。
- 需要并行推进多个 SDD。

### 3.2 建议使用

- OpenSpec/SDD 实施任务超过 3 个 code/config task。
- 预计要运行大量验证且可能产生中间状态。
- 需要隔离用户当前 IDE 工作区。

### 3.3 不强制使用

- 纯文档分析。
- 单文件小修。
- 拼写、链接、格式修复。
- 只读调研。
- 用户明确要求在当前工作区完成。

---

## 4. 与 SDD 的关系

SDD 记录任务上下文，但不自动创建 worktree。

建议在 SDD `README.md` 或 `sdd.json` 中记录：

```text
Git Boundary: /home/slime/Code/SlimeAI
Worktree: none | /home/slime/Code/SlimeAI/.worktrees/<name>
Branch: <branch>
```

如果任务使用 worktree，`progress.md` 应记录：

- 创建时间。
- 创建命令。
- worktree 路径。
- 分支名。
- baseline status。
- cleanup 状态。

---

## 5. 推荐创建流程

```text
1. 识别目标 Git 边界。
2. 运行 git status --short。
3. 检测是否已在 linked worktree。
4. 检查是否是 submodule。
5. 检查 .worktrees 是否被 ignore。
6. 如果当前仓 dirty，询问是否继续创建隔离 worktree。
7. 创建 worktree。
8. 在 SDD progress.md 记录路径和分支。
9. 在 worktree 内运行 baseline 验证或至少记录未验证原因。
```

---

## 6. 推荐清理流程

```text
1. 确认 SDD 状态为 done 或任务明确取消。
2. 在 worktree 路径运行 git status --short。
3. 如果 dirty，停止清理并报告。
4. 如果 clean，执行 git worktree remove。
5. 执行 git worktree prune。
6. 更新 SDD progress.md cleanup 记录。
```

---

## 7. 根仓当前特殊问题

根仓当前长期存在大量未跟踪目录，例如：

- `Resources/Skills/*`
- `Resources/Agent/*`
- `Games/BrotatoLikeOld/`
- `SlimeAIOld/`

这些目录会削弱 `git status --short` 的信号。

建议后续单独做 workspace hygiene SDD：

- 分类哪些资源应加入 `.gitignore`。
- 哪些资源应纳入版本管理。
- 哪些旧目录应迁移到 `Resources/` 或删除。
- 不在本次 SystemAgent 优化文档中直接清理。

---

## 8. 与 submodule 的边界

`Games/BrotatoLike/SlimeAI/` 是 submodule，只读镜像。

规则：

- 不在该目录直接做框架业务改动。
- 框架改动必须在 `/home/slime/Code/SlimeAI/SlimeAI` 仓处理。
- 游戏仓只更新 submodule 指针。
- 如果需要游戏侧 worktree，应创建在游戏仓自己的 `.worktrees/` 下。

---

## 9. 不建议的做法

### 9.1 不使用全局 worktree 目录作为默认

全局目录方便，但对 SlimeAI 多仓边界不够直观。

项目本地 `.worktrees/` 更容易判断归属。

### 9.2 不由 hook 自动创建 worktree

worktree 创建是 Git 写操作，必须由 workflow 或用户明确触发。

### 9.3 不把 worktree 策略绑定到所有 SDD

SDD 可以不使用 worktree。

worktree 是隔离策略，不是任务管理的必要条件。

---

## 10. 最终建议

采用每仓独立 `.worktrees/`。

但把 worktree 作为“中大型或高风险任务的隔离工具”，不要作为所有新功能的默认仪式。

SystemAgent 应先做到：

- 判断 Git 边界。
- 判断是否需要 worktree。
- 把 worktree 信息写入 SDD。
- 保护 dirty worktree。

而不是一上来自动创建或自动清理。
