# Resource Path Migration Skill 设计

> 更新：2026-06-07
> 状态：current design decision
> 范围：Godot `res://` 资源文件/目录移动、DataOS resource ref、ResourcePaths 生成、DocsAI 示例路径、跨框架/游戏仓资源边界。

## 1. Goal

建立一个 SystemAgent skill：当资源文件或目录移动、重命名后，在当前工作目录内把旧路径替换成新路径，并验证旧路径残留。

它解决的是 `ResourceManagement` 不应该承担的事情：

```text
ResourceManagement / ResourceLoading 负责统一加载。
ResourceGenerator 负责生成当前项目的资源 catalog。
Resource Path Migration skill 负责移动目录后的引用迁移和残留检查。
```

非目标：

- 不替代 Godot editor 的资源移动/重命名操作。
- 不默认跨 git boundary 修改框架仓、游戏仓和 submodule。
- 不把 `res://` 当错误格式；它是 Godot 项目根路径。
- 不在本轮实现完整 `uid://` 迁移或 ResourceId schema。

## 2. 使用场景

触发场景：

- 移动或重命名 `.tscn`、`.tres`、贴图、音频、目录。
- `assets/Effect/...`、`assets/Unit/...`、`Src/ECS/...`、`Scenes/...` 路径发生变化。
- DataOS seed / snapshot 中资源路径需要同步。
- `ResourcePaths.cs` 生成后仍能搜到旧路径。
- 框架 submodule 进入游戏仓后，需要在游戏仓根修 `res://SlimeAI/...` 或游戏自有资源路径。

## 3. 当前工作目录原则

skill 默认只在当前工作目录执行：

```text
cd /home/slime/Code/SlimeAI/SlimeAI
# 修框架资源路径

cd /home/slime/Code/SlimeAI/Games/BrotatoLike
# 修游戏资源路径
```

原因：

- `res://` 的含义由当前 `project.godot` 所在项目决定。
- 框架仓和游戏仓是不同 git boundary。
- `Games/*/SlimeAI/` 是 submodule 镜像，禁止在里面做框架业务改动。

## 4. 默认流程

1. 确认当前 git boundary：

```bash
git rev-parse --show-toplevel
git status --short
```

2. 先移动文件或目录：

```bash
git mv old/path new/path
```

也可以先在 Godot editor 里移动。skill 不强制负责移动文件本身。

3. dry-run 替换旧路径。框架仓内可用相对脚本路径：

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

4. 确认输出后应用。框架仓示例：

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

5. 验证旧路径残留：

```bash
rg -n "res://assets/Effect/old" .
```

残留必须逐项解释：历史文档可接受，runtime/DataOS/current docs 不应残留。

6. 重新生成资源 catalog：

```bash
dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
```

如果当前目录是游戏仓，后续 generator 应支持在游戏仓根运行并输出游戏仓自己的 catalog；框架仓 generator 不应默认拥有游戏资源。

7. 按影响面运行验证：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
python3 Workspace/SDD/sdd.py validate --all
```

游戏仓则运行对应游戏的 build、DataOS validate 和 Godot scene smoke。

## 5. 替换范围

默认处理文本文件，排除：

- `.git/`
- `.godot/`
- `.ai-temp/`
- `bin/`
- `obj/`
- `.vs/`
- `.idea/`
- `Library/`

默认应该覆盖：

- `.cs`
- `.tscn`
- `.tres`
- `.json`
- `.sql`
- `.md`
- `.csproj`
- `.godot`
- `.cfg`
- `.yaml` / `.yml`

不要用一次替换覆盖整个工作区的二进制资产。

## 6. 验收标准

```gherkin
Scenario: Old resource path is fully migrated
  Given a resource path was moved from old path to new path
  When Resource Path Migration is applied
  Then current runtime code, DataOS resource refs, generated catalog, and current DocsAI contain the new path
  And rg old path has no unclassified runtime/data/current docs matches
```

```gherkin
Scenario: Migration stays inside one git boundary
  Given the current working directory is the framework repo
  When Resource Path Migration runs
  Then it does not modify a game repo or Games/*/SlimeAI submodule content
```

## 7. 和 ResourceManagement 的关系

`ResourceManagement` 不再被描述为“移动目录不出错”的解决方案。它后续可以简化为：

- `Load<T>(key, category)` strict lookup。
- `LoadPath<T>(path, source, owner, usage)` 受控路径加载。
- `TryLoad` / `ResourceLoadResult` 结构化失败。
- `ResourceCatalogDiagnostics` 输出 duplicate、missing、stale、DataOS refs loadable。

目录移动后的修复责任在本 skill 和 diagnostics，不在 runtime 加载器里猜测。

## 8. 后续升级

后续可选增强：

- 支持批量 JSON manifest：一次替换多组 old/new。
- 支持 `--project-root`，自动定位 `project.godot` 并同时替换 `res://` 和项目相对路径。
- 支持 Godot `ResourceUID` 研究输出：记录 path -> uid 映射，但不默认改主存储。
- 支持生成 resource migration report artifact，供 CI 或 SDD progress 引用。
