---
name: resource-path-migration
description: 移动或重命名 Godot 资源文件/目录后，用于替换 res:// 或项目相对资源路径、重新生成资源 catalog、检查旧路径残留，并处理框架仓/游戏仓/submodule git 边界。
---

# Resource Path Migration

用于移动或重命名 Godot 资源文件、`.tscn`、`.tres`、贴图、音频或资源目录后，迁移旧路径引用并验证旧路径是否残留。

## 核心判断

- `res://` 本身不是问题；它是 Godot project root 路径。
- 真正要处理的是路径 owner、引用迁移和 diagnostics。
- 当前工作目录决定 `res://` 的含义：框架仓、游戏仓和 `Games/*/SlimeAI/` submodule 不能混改。
- 默认不负责移动文件本身；先由 Godot editor、用户或 `git mv` 完成移动，再用本 skill 替换引用。

## 流程

1. 确认 git boundary 和 dirty 范围：

```bash
git rev-parse --show-toplevel
git status --short
```

2. 确认 old/new path。优先使用 Godot 路径：

```text
old = res://assets/Effect/old
new = res://assets/Effect/new
```

3. 先 dry-run。框架仓内可用相对脚本路径：

```bash
python3 .ai-config/skills/core/resource-path-migration/scripts/migrate_resource_path.py \
  --old "res://assets/Effect/old" \
  --new "res://assets/Effect/new"
```

游戏仓内没有 `.ai-config` 时，用框架仓脚本绝对路径，并显式把当前游戏仓作为 `--root`：

```bash
python3 /home/slime/Code/SlimeAI/SlimeAI/.ai-config/skills/core/resource-path-migration/scripts/migrate_resource_path.py \
  --root . \
  --old "res://assets/Effect/old" \
  --new "res://assets/Effect/new"
```

4. 确认影响范围后应用。框架仓示例：

```bash
python3 .ai-config/skills/core/resource-path-migration/scripts/migrate_resource_path.py \
  --old "res://assets/Effect/old" \
  --new "res://assets/Effect/new" \
  --apply
```

游戏仓示例：

```bash
python3 /home/slime/Code/SlimeAI/SlimeAI/.ai-config/skills/core/resource-path-migration/scripts/migrate_resource_path.py \
  --root . \
  --old "res://assets/Effect/old" \
  --new "res://assets/Effect/new" \
  --apply
```

5. 检查旧路径残留：

```bash
rg -n "res://assets/Effect/old" .
```

残留必须分类：current runtime/DataOS/DocsAI 不应残留；历史聊天、迁移记录或归档文档可以保留，但要在最终说明中列明。

6. 重新生成 catalog。框架仓当前命令：

```bash
dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
```

游戏仓后续应在游戏仓根运行游戏自己的 generator / catalog 输出；不要让框架仓 generator 默认拥有游戏资源。

7. 按影响面验证：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
python3 Workspace/SDD/sdd.py validate --all
```

## 跨仓规则

- 在 `/home/slime/Code/SlimeAI/SlimeAI`：只改框架仓资源和文档。
- 在 `/home/slime/Code/SlimeAI/Games/<Game>`：只改游戏仓资源、DataOS 和 game catalog。
- 禁止在 `Games/<Game>/SlimeAI/` submodule 内直接做框架业务改动。
- 如果框架资源在游戏仓里显示为 `res://SlimeAI/...`，框架源路径仍应在框架仓维护，游戏仓只更新 submodule 指针或游戏侧引用。
- 脚本可以从框架仓绝对路径调用，但替换 root 必须是当前 git boundary；不要把 `/home/slime/Code/SlimeAI` 工作区根作为 `--root`。

## 脚本说明

`scripts/migrate_resource_path.py` 默认 dry-run，只列出会修改的文本文件；加 `--apply` 才写文件。

默认排除：

```text
.git/ .godot/ .ai-temp/ bin/ obj/ .vs/ .idea/ Library/
```

默认文本扩展：

```text
.cs .tscn .tres .json .sql .md .csproj .godot .cfg .yaml .yml .txt
```

## 禁止

- 不要把 `res://` 全面替换为文件系统绝对路径。
- 不要用全仓 blind replace 修改二进制资产。
- 不要在没有 dry-run 输出的情况下大范围 `--apply`。
- 不要把 ResourceManagement 当路径自动修复器；路径移动后的事实闭环来自本 skill、ResourceGenerator 和 diagnostics。
