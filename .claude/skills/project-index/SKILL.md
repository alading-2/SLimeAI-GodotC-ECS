---
name: project-index
description: SlimeAI 框架仓导航入口。用于查找 ECS、Data、SDD、验证和工具位置。
---

# SlimeAI 框架仓导航

## 必读入口

- `DocsNew/README.md` — 方向决策入口
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md` — 当前项目设计
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/INDEX.md` — 设计索引
- `Workspace/SystemAgent/README.md` — 流程工具
- `SDD/INDEX.md` — SDD 总索引

## 查找规则

- 找 ECS 模块事实源：查 `Src/ECS/**` 旁文档。
- 找 Data 当前说明：查 `DocsNew/ECS/Data/Data系统说明.md`。
- 找方向决策：查 `DocsNew/ECS框架与AIFirst方向决策.md`。
- 找设计文档：查 `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/`。
- 找计划源：查 `SDD/project/projects/`，历史 SDD 查 `SDD/`。
- 找测试：查 `Src/ECS/Test/` 和 `Tools/run-tests.sh`。
- 找第一个游戏接入验证：游戏仓库是 `/home/slime/Code/SlimeAI/Games/BrotatoLike`。

## 常用命令

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
rg -n "<关键词>" Src/ECS DocsNew SDD Workspace/SystemAgent
find Src/ECS -maxdepth 4 -type f -name '*.cs' | sort
Tools/run-build.sh
Tools/run-tests.sh
python3 Workspace/SDD/sdd.py list
python3 Workspace/SDD/sdd.py validate --all
```

## 边界

- 框架仓只改 ECS 通用 Runtime、Data、Event、Entity、System、Validation 和工具逻辑。
- 游戏资产、游戏特定场景和 BrotatoLike 玩法胶水默认放到 `Games/BrotatoLike`。
- **子模块禁区**：`Games/*/SlimeAI/` 是框架仓的只读镜像（git submodule），**禁止在其中直接修改**。所有框架代码修改必须在当前框架仓内完成。
- `DocsAI/` 已删除，不作为当前事实源。
- `openspec/` 仅保留历史资产，不作为默认入口。
