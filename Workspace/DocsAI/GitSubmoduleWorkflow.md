# Git Submodule 工作流

本文档是工作区内 git submodule 操作的唯一事实源。所有涉及 `Games/*/SlimeAI` 的更新、故障排查均以此为准。

## 仓库结构

```
/home/slime/Code/SlimeAI          (工作区根，非 Git 仓库)
├── SlimeAI/                        (框架仓，唯一可写)
│   └── GameOS/ DataOS/ ...
├── Games/BrotatoLike/              (游戏仓)
│   └── SlimeAI/                   (submodule → 框架仓的只读镜像)
└── Games/Game2/ ...                (未来)
```

**单向数据流**：框架改动只在 `SlimeAI/` 仓提交；游戏仓通过更新 submodule 指针拉取新版本。

> **本地优先**：框架仓提交后可以直接从本地路径同步，不需要先 push 到远程。push 远程只在需要共享或备份时做。

## 更新 submodule（日常）

### 方式 A：从本地框架仓同步（推荐，最快）

框架仓提交后直接 fetch 本地路径，无需 push 到 GitHub：

```bash
FRAMEWORK=/home/slime/Code/SlimeAI/SlimeAI

# 1. 进入 submodule 目录，fetch 本地框架仓的最新 commit
cd /home/slime/Code/SlimeAI/Games/BrotatoLike/SlimeAI
git fetch "$FRAMEWORK" main
git checkout FETCH_HEAD

# 2. 回到游戏仓，提交 submodule 指针变更
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
git add SlimeAI
git commit -m "chore: sync SlimeAI submodule from local framework"
```

### 方式 B：从远程同步（需要先 push 框架仓）

```bash
# 先 push 框架仓
cd /home/slime/Code/SlimeAI/SlimeAI
git push SlimeAIGameFramework main

# 再更新 submodule
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
git submodule update --remote SlimeAI
git add SlimeAI
git commit -m "chore: sync SlimeAI submodule from remote"
```

VSCode Task `update: BrotatoLike SlimeAI Submodule` 等价于方式 B。

## 故障排查

### 报错："未跟踪文件将会被覆盖"

```
error: 工作区中下列未跟踪的文件将会因为检出操作而被覆盖：
        Src/Validation/Runtime/Data/README.md
        Src/Validation/Runtime/Data/RuntimeDataValidationScene.cs
```

**原因**：submodule 工作区里有未跟踪文件，而远程新 commit 已包含同名文件。Git 出于安全拒绝覆盖。

**解决步骤**：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike/SlimeAI

# 方案 A：文件需要保留 → 先提交到框架仓，再本地同步
cd /home/slime/Code/SlimeAI/SlimeAI
git add <文件>
git commit -m "..."

# 本地 fetch 同步
cd /home/slime/Code/SlimeAI/Games/BrotatoLike/SlimeAI
git fetch /home/slime/Code/SlimeAI/SlimeAI main
git checkout FETCH_HEAD

# 方案 B：文件不需要 → 直接删除再本地同步
cd /home/slime/Code/SlimeAI/Games/BrotatoLike/SlimeAI
git clean -fd
git fetch /home/slime/Code/SlimeAI/SlimeAI main
git checkout FETCH_HEAD
```

### 报错：更新后 submodule 指针没变

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git log --oneline -3          # 看本地最新 commit
```

本地 fetch 同步不依赖 push，只要框架仓已 commit 即可直接更新 submodule。

### .uid 文件大量出现

Godot 4.x 会给每个 `.cs` 文件生成 `.uid` 文件。已在 `SlimeAI/.gitignore` 中全局忽略 `**/*.uid`。

如果仍然看到未跟踪 `.uid`：
- 确认 `.gitignore` 已生效：`git check-ignore -v <文件>`
- 如果之前已跟踪过 .uid，需先 `git rm --cached` 再忽略

## 禁区

- **禁止**在工作区根 `/home/slime/Code/SlimeAI` 执行 `git init` 或任何 git 操作
- **禁止**在游戏仓的 `SlimeAI/` 目录内做业务改动（它是只读镜像）
- 框架改动必须切换到 `/home/slime/Code/SlimeAI/SlimeAI` 提交
- 执行 `git status/commit` 前，必须先 `cd` 到对应仓库目录

## .gitignore 配置

```
# 工作区根 .gitignore
Games/BrotatoLike/SlimeAI/      # 父仓库不跟踪 submodule 内容

# 框架仓 SlimeAI/.gitignore
**/*.uid                         # 忽略 Godot 4 .uid 文件

# 游戏仓 Games/BrotatoLike/.gitignore（已存在）
**/*.uid
```

## 资源路径和 ResourceGenerator

游戏仓的 `project.godot` 所在目录就是 `res://` 根。框架作为 submodule 出现在游戏仓时，框架资源路径会是：

```text
res://SlimeAI/...
```

游戏自己的资源路径仍属于游戏仓：

```text
res://assets/...
res://Scenes/...
res://Src/Game/...
```

规则：

- 框架仓 `ResourceGenerator` 默认只生成框架资源 catalog，不应扫描游戏仓并把游戏资源写回框架仓。
- 游戏仓如果需要资源 catalog，应在游戏仓根运行 game-local generator 或后续支持 `--project-root` / `--output` 的生成器。
- 移动框架资源：在框架仓改路径、跑 generator、提交框架仓，再更新游戏仓 submodule 指针。
- 移动游戏资源：在游戏仓根处理路径替换、game catalog 和游戏验证，不改 `Games/<Game>/SlimeAI/` 内容。
- 移动或重命名资源后，用 `project-filesystem` skill/script 替换旧 `res://` 引用并运行 `rg` 检查旧路径残留。
