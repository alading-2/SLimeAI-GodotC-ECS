---
name: project-index
description: SlimeAI 框架仓库导航入口。用于查找 GameOS、DataOS、Agent、Packages、测试、计划和迁移输入位置。
---

# SlimeAI 项目导航

## 必读入口

- `DocsAI/INDEX.md`
- `DocsAI/ProjectState.md`
- `../Workspace/SystemAgent/README.md`
- `../Workspace/SDD/README.md`
- `DocsAI/GameOS/Contracts.md`
- `DocsAI/GameOS/ApiIndex.md`
- `DocsAI/GameOS/DebugGuide.md`

## 查找规则

- 找 Runtime / Capability API：先看 `DocsAI/GameOS/Contracts.md` 和 `DocsAI/GameOS/ApiIndex.md`。
- 找源码：查 `GameOS/Runtime/`、`GameOS/Capabilities/`、`GameOS/GodotBridge/`。
- 找 DataOS：查 `DocsAI/DataOS/Overview.md`、`DataOS/Schema/`、`DataOS/Migrations/`、`DataOS/Generators/`、`DataOS/Validation/`。
- 找测试：查 `Tests/SlimeAI.GameOS.Tests/` 和 `Tools/run-tests.sh`。
- 找计划源：新执行计划默认查 `../SDD/active/<sdd>/`，历史规范基线可查 `../openspec/specs/`。
- 找迁移来源：旧输入仓库是 `/home/slime/Code/Godot/Games/MyGames/brotato-my`。
- 找第一个游戏接入验证：游戏仓库是 `/home/slime/Code/SlimeAI/Games/BrotatoLike`。

## 常用命令

```bash
rg -n "<关键词>" DocsAI GameOS DataOS Tests Tools ../Workspace/SystemAgent ../SDD ../openspec
find GameOS -maxdepth 4 -type f | sort
Tools/run-build.sh
Tools/run-tests.sh
```

## 边界

- 框架仓库只改通用 Runtime、Capability、DataOS、Validation、Observation、Agent 和包发布逻辑。
- 游戏资产、游戏特定场景和 BrotatoLike 玩法胶水默认放到 `Games/BrotatoLike`。
- **子模块禁区**：`Games/*/SlimeAI/` 是框架仓的只读镜像（git submodule），**禁止在其中直接修改、新增、删除文件或用 `cp -a` 覆盖**。所有框架代码修改必须在 `/home/slime/Code/SlimeAI/SlimeAI/` 框架仓内完成；同步到游戏仓只能通过更新 submodule 指针（`git fetch` + `git checkout FETCH_HEAD` 或 `git submodule update --remote`）。
- 不从旧仓库复制长期架构入口；旧仓库只作为迁移输入和历史对照。
