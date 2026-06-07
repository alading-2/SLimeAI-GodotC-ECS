# Data 文档更新与门禁清单

> 日期：2026-05-30
> 目的：列出当前 Data 仍然要更新的文档、它们为什么要改、以及需要用什么门禁确认改对了。
> 关系：本文件服务于 `03-对比AiFirst框架分析Data重构后剩余框架问题深度复查.md`、`04-BUG:Data无兼容重构后移动与施法失败根因说明.md` 和 `05-Data残余问题代码修复分解.md`。

## 1. 当前应区分的三类文档

### 1.1 current

当前要继续作为事实源的 `Src/` 旁文档，必须只写 descriptor-first / snapshot-first / no-compat 的当前做法，不再推荐旧 `DataMeta`、`DataRegistry`、`RuntimeTables`、`.tres`、裸字符串键名。

### 1.2 historical

只保留历史证据和迁移背景，不再作为当前实现入口。

### 1.3 bug / follow-up

专门记录当前仍然成立的问题、代码改法和门禁，不和主事实源混写。

`03`、`04`、`05`、`06` 这一组就是 bug / follow-up 事实源。

## 2. 必改文档清单

### 2.1 直接会误导 AI 的文档

| 路径 | 当前问题 | 需要改成什么 |
| --- | --- | --- |
| `SlimeAI/Src/ECS/Base/System/TestSystem/README.md` | 仍在示例里使用 `DataKey.XXX.Key` 和 `DataMeta`。 | 改成 generated handle 主导的说明。 |
| `SlimeAI/Src/ECS/Base/System/Core/README.md` | 仍残留 `TestDataRegister` / `DataMeta` 旧说法。 | 改成系统配置来自 `system.config` / `system.preset` snapshot records。 |
| `SlimeAI/Src/ECS/Base/Component/Component规范.md` | 仍写 `static readonly DataMeta` 和 `TargetNode` const string。 | 改成当前 generated handle / runtime ref 边界。 |
| `SlimeAI/Src/ECS/Base/Entity/Entity规范.md` | 仍提 `DataMeta.CanMigrate`。 | 改成当前 catalog / snapshot / record 语义。 |
| `SlimeAI/Src/ECS/Base/Entity/Core/EntityManager.md` | 仍混有旧 Data 迁移和 `new Data()` 旧说法。 | 改成显式 snapshot record + catalog-bound Data。 |
| `SlimeAI/Src/ECS/Base/System/Movement/README.md` | 仍有 `TargetNode`、`DefaultMoveMode` 旧说明。 | 改成当前 Movement / DataOS 事实源。 |
| `SlimeAI/Src/ECS/Base/System/Movement/EntityMovementComponent说明.md` | 仍以“实体初始化时写入 DefaultMoveMode”为入口。 | 改成“DefaultMoveMode 必须在 snapshot record 中前置”。 |
| `SlimeAI/DocsNew/ECS/Data/Data系统说明.md` | 当前较新，但不是用户裁决的长期统一文档入口。 | 可保留已补充的 residual 入口；后续统一文档时再裁决是否删除。 |

### 2.2 旧 DocsAI 处理

`SlimeAI/DocsAI` 已被用户裁决为旧入口，并且目录已经删除。后续处理方式是不要恢复旧 `DocsAI`，也不要继续局部修补；当前文档更新落到 `Src/` 旁文档、DocsNew 和 SDD design。

### 2.3 需要降级为历史证据的文档

这些页面不一定要删，但如果继续保留，就必须明确标记为历史输入，不得作为当前推荐入口：

- `SlimeAI/Src/ECS/Base/Component/Component规范.md`
- `SlimeAI/Src/ECS/Base/Entity/Entity规范.md`
- `SlimeAI/Src/ECS/Base/Entity/Core/EntityManager.md`
- `SlimeAI/Src/ECS/Base/System/TestSystem/README.md`
- `SlimeAI/Src/ECS/Base/System/Core/README.md`

### 2.4 应继续保持 current 的文档

- `SlimeAI/DocsNew/ECS/Data/Data系统说明.md`
- `SlimeAI/Src/ECS/Base/**` 旁文档

这些页面应该继续讲当前主链路，但需要补上 residual docs 的指引，不要再让读者把“主链路已收口”误解成“所有中层契约都已经完了”。

## 3. 文档更新方式

### 3.1 先修路径，再修内容

先把 README / INDEX / ProjectState 的入口链路修正，让读者能先找到最新分析，再逐步修内容页。

### 3.2 先去旧入口，再加新入口

每个要改的页面都应先删掉以下表达：

- `DataMeta` 作为长期事实源
- `DataRegistry` 作为长期 runtime 来源
- `RuntimeTables` 作为当前数据源
- `DataKey.XXX.Key` 作为推荐入口
- `const string TargetNode` 作为稳定引用方式
- `new Data()` 作为默认推荐构造方式

然后再补当前推荐写法。

### 3.3 文字必须写清楚三件事

每个 current 页面都要写清楚：

1. 现在的问题是什么。
2. 为什么会这样。
3. 具体要改哪些代码和哪些文档。

## 4. 门禁

### 4.1 代码门禁

```bash
cd /home/slime/Code/SlimeAI
rg -n "class DataMeta|class DataRegistry|public static implicit operator string|RuntimeTables|DataKey\\.XXX\\.Key|const string TargetNode|new Data\\(" SlimeAI/DocsNew SlimeAI/Src/ECS/Base -g '*.md'
```

当前目标不是让所有命中都消失，而是让 current 文档不再出现这些旧表达。

### 4.2 Data 门禁

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
jq -r '(.descriptors | map({key: .stableKey, value: .valueType}) | from_entries) as $d | .records[] | .table as $table | .id as $id | .fields | to_entries[] | select(($d[.key] // "__missing__") != .value.type) | [$table,$id,.key,($d[.key] // "missing_descriptor"),.value.type] | @tsv' Data/DataOS/Snapshots/runtime_snapshot.json
```

第二条命令必须无输出。

### 4.3 行为门禁

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```

如果要验证移动和施法链路，再补 Godot scene smoke。

## 5. 结论

Data 当前最需要修的不是“再找一个兼容入口”，而是把文档、门禁和代码都按同一张事实源地图重新对齐。

如果 current 文档继续同时推荐 descriptor-first 和旧 DataKey/DataMeta 写法，AI 还是会把自己引回过去。
