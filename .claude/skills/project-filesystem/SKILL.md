---
name: project-filesystem
description: 新增、删除、重命名、移动或检查项目目录/Godot 资源目录后，用于迁移 res://、项目相对路径或当前仓绝对路径引用，重新生成资源 catalog，检查旧路径残留，并处理框架仓/游戏仓/submodule git 边界。
---

# Project Filesystem

用于新增、删除、重命名、移动或检查项目目录，尤其是 Godot 资源文件、`.tscn`、`.tres`、贴图、音频或资源目录变更后，迁移旧路径引用并验证旧路径是否残留。

## 核心判断

- `res://` 本身不是问题；它是 Godot project root 路径。
- 真正要处理的是路径 owner、引用迁移和 diagnostics。
- 当前工作目录决定 `res://` 的含义：框架仓、游戏仓和 `Games/*/SlimeAI/` submodule 不能混改。
- 本 skill 可以指导简单目录创建、删除、移动和检查；路径替换必须先 dry-run，再 apply。
- 移动 Godot 资源优先使用 Godot editor 或 `git mv`，避免丢失 `.tscn/.tres` 内部引用语义；移动完成后再用本 skill 替换文本引用。
- 目录稳定性属于 workflow / skill / diagnostics，不属于运行时 `ResourceManagement` / `ResourceLoading` 的职责。

## 支持的路径形态

| 形态 | 示例 | 处理口径 |
| --- | --- | --- |
| Godot 绝对资源路径 | `res://assets/Effect/Old` | 首选。含义由当前 `project.godot` 所在仓决定。 |
| 项目相对路径 | `assets/Effect/Old`、`Src/ECS/Tools/Old` | 可替换；通常用于源码、文档、生成器配置。 |
| 当前仓绝对路径 | `/home/slime/Code/SlimeAI/SlimeAI/assets/Effect/Old` | 只在确有残留时替换；必须确认没有跨 git boundary。 |

脚本默认只替换传入的 `--old` 字符串。需要同时替换上述明显变体时，加 `--include-variants`，并先检查 dry-run 输出。

## 流程

1. 确认 git boundary 和 dirty 范围。不要在工作区父目录 `/home/slime/Code/SlimeAI` 执行替换：

```bash
git rev-parse --show-toplevel
git status --short
```

2. 确认操作类型：

```text
mkdir: 新增目录，通常不需要路径替换，创建后更新 owner 文档或 generator scan root。
delete: 删除目录，先 rg 查引用；只删除无 current runtime/DataOS/DocsAI 引用的目录。
move/rename: 先移动目录，再替换 old/new 引用，最后检查旧路径残留。
check: 只检查路径引用、旧路径残留、generator 输出和 git boundary。
```

3. 确认 old/new path。优先使用 Godot 路径：

```text
old = res://assets/Effect/old
new = res://assets/Effect/new
```

4. 简单目录操作示例：

```bash
mkdir -p Src/ECS/Tools/CommonUtilities
git mv Src/ECS/Tools/OldDirectory Src/ECS/Tools/NewDirectory
```

删除目录前必须先检查引用：

```bash
rg -n "Src/ECS/Tools/OldDirectory|res://Src/ECS/Tools/OldDirectory" .
```

5. 先 dry-run 路径替换。框架仓内可用相对脚本路径：

```bash
python3 .ai-config/skills/core/project-filesystem/scripts/migrate_resource_path.py \
  --old "res://assets/Effect/old" \
  --new "res://assets/Effect/new"
```

游戏仓内没有 `.ai-config` 时，用框架仓脚本绝对路径，并显式把当前游戏仓作为 `--root`：

```bash
python3 /home/slime/Code/SlimeAI/SlimeAI/.ai-config/skills/core/project-filesystem/scripts/migrate_resource_path.py \
  --root . \
  --old "res://assets/Effect/old" \
  --new "res://assets/Effect/new"
```

如果同一次目录移动同时留下 `res://`、项目相对路径和当前仓绝对路径引用，可以 dry-run 变体替换：

```bash
python3 .ai-config/skills/core/project-filesystem/scripts/migrate_resource_path.py \
  --old "res://assets/Effect/old" \
  --new "res://assets/Effect/new" \
  --include-variants
```

6. 确认影响范围后应用。框架仓示例：

```bash
python3 .ai-config/skills/core/project-filesystem/scripts/migrate_resource_path.py \
  --old "res://assets/Effect/old" \
  --new "res://assets/Effect/new" \
  --apply
```

游戏仓示例：

```bash
python3 /home/slime/Code/SlimeAI/SlimeAI/.ai-config/skills/core/project-filesystem/scripts/migrate_resource_path.py \
  --root . \
  --old "res://assets/Effect/old" \
  --new "res://assets/Effect/new" \
  --apply
```

7. 检查旧路径残留。至少检查首选路径和可能的项目相对路径：

```bash
rg -n "res://assets/Effect/old" .
rg -n "assets/Effect/old" .
```

残留必须分类：current runtime/DataOS/DocsAI 不应残留；历史聊天、迁移记录或归档文档可以保留，但要在最终说明中列明。

8. 重新生成 catalog。框架仓当前命令：

```bash
dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
```

游戏仓后续应在游戏仓根运行游戏自己的 generator / catalog 输出；不要让框架仓 generator 默认拥有游戏资源。

9. 按影响面验证：

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

常用参数：

```text
--old <path>              旧路径或目录字符串
--new <path>              新路径或目录字符串
--root <dir>              替换根，默认当前目录
--apply                   写入文件；不加就是 dry-run
--include-variants        同时替换 res://、项目相对路径和当前 root 下绝对路径变体
--include-extension .gd   额外纳入扩展名
--exclude-dir <name>      额外排除目录名
```

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
- 不要删除仍被 current runtime、DataOS、DocsAI 或 generator 配置引用的目录。
- 不要把路径替换 root 设成 `/home/slime/Code/SlimeAI` 工作区父目录。
- 不要把 ResourceManagement 当路径自动修复器；路径移动后的事实闭环来自本 skill、ResourceGenerator 和 diagnostics。
