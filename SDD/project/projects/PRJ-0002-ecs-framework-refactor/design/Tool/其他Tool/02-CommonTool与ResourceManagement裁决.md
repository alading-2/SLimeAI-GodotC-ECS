# CommonTool 与 ResourceManagement 裁决

> 更新：2026-06-07
> 状态：current consolidated feature design
> 裁决：`CommonTool.LoadPackedScene` 不应继续留在杂项入口，应迁入 `ResourceLoading`；但用户确认“通用工具区域”概念可保留，且仍属于 `Src/ECS/Tools`。执行型 SDD 应删除或 internal 化当前 `CommonTool` 类，不保留无约束杂物箱；Common Utilities 最终位置为 `Src/ECS/Tools/CommonUtilities/` + `DocsAI/ECS/Tools/CommonUtilities/`，必须有 manifest、禁止项和测试。2026-06-07 用户校准：`res://` 不是问题，路径移动后的替换和残留检查由 project directory / `project-filesystem` workflow 负责。`ResourceManagement` 不应作为长期“资源管理器”概念保留，最终只保极薄 `ResourceLoading` 统一加载工具。

## 1. 当前证据

### 1.1 CommonTool 当前形态

`Src/ECS/Tools/CommonTool.cs` 只有一个方法：

```text
LoadPackedScene(string path, string usageName)
  -> 空路径 warn
  -> ResourceManagement.LoadPath<PackedScene>(path)
  -> null 时 error
```

调用点：

- `Src/ECS/Runtime/Entity/Spawn/EntitySpawnPipeline.cs`
- `Src/ECS/Capabilities/Effect/System/EffectTool.cs`
- `Data/Feature/Ability/ChainLightning/ChainLightning.cs`
- Data residual 设计文档中的历史示例

这说明它实际承担的是“DataOS / runtime record 中保存 res:// path，最终实例化点统一加载 PackedScene”的便利封装，不是通用杂项工具。

### 1.2 ResourceManagement 当前形态

`Data/ResourceManagement/ResourceManagement.cs` 提供：

- `Load<T>(string name, ResourceCategory category)`
- `LoadPath<T>(string path)`
- `LoadAll<T>(ResourceCategory category, string pathFilter = "")`
- `GetNames(ResourceCategory category, string pathFilter = "")`

`Data/ResourceManagement/README.md` 已明确：

- 资源加载统一入口是 `ResourceManagement`。
- 业务代码不应硬编码 `res://`。
- UI / 编辑器列资源用 `ResourceCatalog`。
- `ResourcePaths.cs` 由 `Tools/ResourceGenerator` 生成。

### 1.3 ResourceCatalog 当前形态

`Data/ResourceManagement/ResourceCatalog.cs` 从两类来源构建目录：

- `DataRuntimeBootstrap.Default` 的 runtime snapshot records。
- `ResourcePaths.Resources` 中的 asset entries。

它对 TestSystem/ResourcePicker 有价值，但当前仍缺少“目录覆盖验证”和“加载失败结构化结果”。

## 2. 是否必要

### CommonTool / Common Utilities

当前 `CommonTool` 作为独立 Tool owner 不必要。

它的名字过宽，会鼓励 AI 把“暂时不知道放哪”的方法继续塞进去。AI-first 框架最怕这种入口，因为它没有 owner、没有验证门禁、没有禁用列表。

用户补充后，裁决从“通用工具概念删除”调整为：

- 通用工具区域可以保留。
- 当前 `CommonTool.LoadPackedScene` 不是通用工具，应迁入资源加载。
- `CommonTool` 不作为 DocsAI/ECS/Tools 的正式 owner。
- Common Utilities 最终放在 `Src/ECS/Tools/CommonUtilities/` + `DocsAI/ECS/Tools/CommonUtilities/`，属于 Tools 体系内的独立 owner。
- 不新增 `CommonTool.SomeHelper()` 这种无 owner 方法。
- 当前文档和 skill 不再教 AI 使用 `CommonTool.LoadPackedScene`。

### ResourceLoading / current ResourceManagement

统一加载工具必要；`ResourceManagement` 这个“管理器”概念不必要，且容易误导。

原因：

- AI 需要从统一 manifest 查资源，而不是全局搜索 `res://`。
- DataOS snapshot 只应保存路径、stable id 或关系，不保存 `PackedScene` / `Node` / `Resource` 运行时对象。
- ResourceCatalog 是 TestSystem、资源选择器、视觉预览和调试面板的共同基础。
- 但 `res://` 本身是 Godot project root 路径，不应被当成错误格式；要限制的是业务裸加载和无 owner 的路径扩散。

所以后续执行期裁决是：

- 删除或重命名 current `ResourceManagement` public 心智，默认收敛为 `ResourceLoading`。
- `ResourceLoading` 只做加载、source/owner/usage 诊断和错误报告，不做目录移动、资源身份全局治理或跨游戏 catalog 合并。
- `ResourceCatalog` / `ResourceGenerator` 是旁路 catalog / generator，不属于 runtime loader 的职责。

## 3. 当前缺陷

| 缺陷 | 证据 | AI-first 影响 |
| --- | --- | --- |
| `CommonTool` 名字过宽 | `CommonTool.cs` 注释写“通用杂项工具”。 | AI 会把无 owner 的工具继续堆进去。 |
| `ResourceManagement` 名字过宽 | 当前代码本质只是 `Load` / `LoadPath` / `LoadAll` / `GetNames`，但名字像完整资源管理系统。 | AI 容易把目录移动、catalog、加载、修复、跨游戏资源归属都塞进一个类。 |
| `Load<T>` fallback 使用 contains 匹配 | 找不到精确 key 后遍历 `kvp.Key.Contains(name)`。 | 重名或相近名时可能静默加载错误资源。 |
| `LoadPath<T>` 缺 source policy | 任意 path 都能传入。 | 难区分 DataOS 合法资源引用和业务裸 `res://`。 |
| 加载结果只有 `T?` | 失败只写 log。 | AI 和测试无法拿到结构化失败原因。 |
| ResourceCatalog cache 缺 artifact | `ClearCache()` 存在，但没有目录覆盖报告。 | 资源生成后是否同步，AI 只能靠人工看 UI。 |
| ResourceGenerator gate 未挂到工具文档 | README 提醒运行，但没有 grep/validate gate。 | 添加/移动资源后容易忘记生成。 |
| 目录操作缺 workflow | 移动目录后只能人工搜旧路径；新增/删除/改名目录也缺统一检查步骤。 | AI-first 应提供 dry-run 替换、生成器刷新、旧路径残留检查和 git boundary 规则。 |

## 4. 目标架构

### 4.1 ResourceLoading 是唯一加载入口

目标边界：

| API | 目标职责 | 不做 |
| --- | --- | --- |
| `Load<T>(key, category)` | 严格按 `ResourcePaths` key/category 加载 | 不继续隐式 contains fallback |
| `LoadPath<T>(path, source)` | 只给 DataOS resource ref、runtime snapshot、debug/test 明确来源使用 | 不让业务随意传裸 `res://` |
| `TryLoad<T>(request, out result)` | 输出结构化加载结果 | 不只靠 log 说明失败 |
| `ResourceCatalog` | 提供选择目录、分组、搜索、snapshot/data asset 投影 | 不直接实例化业务对象 |
| `ResourceCatalogDiagnostics` | 输出目录覆盖、重复 key、missing path、stale generated source | 不参与 gameplay 热路径 |
| project directory / `project-filesystem` skill | 新增、删除、重命名、移动、检查目录后的 old/new path 替换和 `rg` 残留检查 | 不替代 runtime loader，不跨 git boundary 混改 |

命名裁决：

```text
current:
  ResourceManagement.Load<T>
  ResourceManagement.LoadPath<T>

target:
  ResourceLoading.Load<T>
  ResourceLoading.LoadPath<T>(..., source/owner/usage)
  ResourceLoading.TryLoad<T>(request, out result)
```

如果执行型 SDD 为降低切片风险短期保留 `ResourceManagement` 文件名，也必须让 DocsAI、skill 和 public 示例只暴露 `ResourceLoading` 心智；切片结束前删除或 internal 化旧 facade。

### 4.2 建议契约

后续实现可引入轻量 DTO：

```text
ResourceLoadRequest
  Key
  Category
  Path
  Source: GeneratedKey | DataSnapshot | Debug | Test | LegacyPath
  UsageName
  Owner

ResourceLoadResult
  Success
  ResourceType
  ResolvedPath
  MatchedBy: ExactKey | Path | LegacyContainsFallback
  ErrorCode
  Message
```

执行型 SDD 可以分阶段迁移调用点，但单个资源加载 cutover 完成前必须删除 contains fallback。旧资源 key 漂移应通过 `ResourceCatalogDiagnostics` 暴露并修复数据/生成源，而不是继续让 fallback 静默匹配。

### 4.3 CommonTool 收口与 Common Utilities 保留规则

迁移目标：

```text
CommonTool.LoadPackedScene(path, usageName)
  -> ResourceLoading.LoadPackedScenePath(path, source: DataSnapshot, usageName)
  -> 删除或 internal 化当前 CommonTool
```

切换策略：

- 旧方法只允许作为执行中临时编译桥，不作为最终产物。
- 任何新增资源加载逻辑必须放 `ResourceLoading` 或对应 capability owner，不放 `CommonTool`。
- DocsAI 不为当前 `CommonTool` 单独建 owner；本设计包就是它的裁决记录。
- 如保留通用工具区域，必须满足：
  - 最终目录是 `Src/ECS/Tools/CommonUtilities/` + `DocsAI/ECS/Tools/CommonUtilities/`。
  - 每个文件按功能命名，不叫 `CommonTool`。
  - 每个 helper 说明输入、输出、副作用、热路径要求和测试入口。
  - 可归入 Resource / Entity / Data / Event / Timer / TargetSelector / ObjectPool / Math / Capability 的函数，不允许放 Common Utilities。

## 5. 调用点建议

### EntitySpawnPipeline

`EntitySpawnPipeline` 从 record path 加载视觉场景是合理的，但应携带：

- record id
- entity id
- stable key 或 field name
- usage name
- source = `DataSnapshot`

这样 AI 看到失败 artifact 时能知道是哪个 snapshot record 缺资源。

### EffectTool / ChainLightning

Effect/Ability 通过 DataOS 或 feature data 加载视觉资源时，应继续允许 `LoadPath`，但要区分：

- 静态 generated key 资源：优先 `Load<T>(key, category)`。
- DataOS resource ref：允许 `LoadPath<T>`，但 source 必须说明来自哪条 record/field。

### UI / System / ObjectPool

这些大多已经使用 `ResourceManagement.Load<T>(nameof(...), category)`，应继续走资源加载 owner。但 `contains fallback` 应删除；如果精确 key 不存在，应让 diagnostics 明确失败并推动修正资源 key / manifest。

## 6. Project Directory / Resource Path Migration workflow

目录和资源路径移动不属于 runtime loader 的责任。单独 skill 是必要的，原因是：

- AI-first 框架需要可复现的目录变更流程，不能靠人工全仓搜索。
- 目录变更影响的不只是 `.tscn/.tres`，还包括 C# 字符串、DataOS snapshot、DocsAI、SDD、generator scan roots、游戏仓引用和绝对路径残留。
- 全局路径、项目相对路径、`res://` 路径含义不同；必须在当前 git boundary 内处理。

默认流程：

1. 确认当前 git boundary：

```bash
git rev-parse --show-toplevel
git status --short
```

2. 确认操作类型：新增、删除、重命名、移动或检查目录。新增目录通常只需要 owner 文档和 generator 配置；删除目录前必须先 `rg` 查引用；移动/重命名必须做 old/new 替换和残留检查。

3. 先移动文件或目录，可以用 `git mv`，也可以在 Godot editor 中移动。

4. dry-run 替换旧路径：

```bash
python3 .ai-config/skills/core/project-filesystem/scripts/migrate_resource_path.py \
  --old "res://assets/Effect/old" \
  --new "res://assets/Effect/new"
```

游戏仓内运行时，显式使用框架仓脚本并传 `--root .`：

```bash
python3 /home/slime/Code/SlimeAI/SlimeAI/.ai-config/skills/core/project-filesystem/scripts/migrate_resource_path.py \
  --root . \
  --old "res://assets/Effect/old" \
  --new "res://assets/Effect/new"
```

如果同一次目录移动同时存在 `res://`、项目相对路径和当前仓绝对路径残留，可先 dry-run 变体替换：

```bash
python3 .ai-config/skills/core/project-filesystem/scripts/migrate_resource_path.py \
  --old "res://assets/Effect/old" \
  --new "res://assets/Effect/new" \
  --include-variants
```

5. 确认 dry-run 输出后加 `--apply`。

6. 检查旧路径残留：

```bash
rg -n "res://assets/Effect/old" .
rg -n "assets/Effect/old" .
```

残留必须分类：历史文档可接受，runtime / DataOS / current DocsAI 不应残留。

7. 重新生成资源 catalog：

```bash
dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
```

8. 按影响面运行构建、DataOS、SDD 或游戏场景验证。

workflow 边界：

- 默认只在当前工作目录执行，不跨 git boundary 混改。
- `Games/*/SlimeAI/` 是 submodule 镜像，禁止在里面做框架业务改动。
- skill 负责目录操作指导、引用替换和验证，不替代 Godot editor 的资源移动语义。
- `res://` 的含义由当前 `project.godot` 所在项目决定。

## 7. Future Split：框架仓 / 游戏仓分离后的资源所有权

未来多游戏结构下：

```text
框架仓：
  只拥有框架资源 catalog。
  例如 res://SlimeAI/Src/ECS/... 在游戏仓中表现为框架 submodule 路径。

游戏仓：
  拥有游戏自己的 assets / Scenes / DataOS resource refs。
  需要 game-local catalog 或 generator --project-root / --output 能力。
```

默认裁决：

- 框架 ResourceGenerator 不默认扫描和拥有游戏资源。
- ResourceLoading facade 可以消费多个 catalog，但 catalog ownership 必须清楚；它不自动把框架和游戏资源合并成一个隐式全局 registry。
- 游戏资源移动在游戏仓根运行 migration workflow。

## 8. `uid://` 校准

Godot `uid://` 对资源移动有价值，但当前不直接作为主存储：

- 它需要验证 C#、DataOS snapshot、导出包、submodule 游戏仓链路。
- `.cs.uid` 文件不是这里讨论的 `uid://` 资源引用。
- 默认先保留 `res://` + ResourceGenerator + diagnostics；后续单独 SDD 验证 UID 可行性。

## 9. Not Recommended

- 不建议继续新增 `CommonTool.SomeHelper()`。
- 不建议长期保留 `CommonTool.LoadPackedScene(...)`。
- 不建议把“暂时不好分类”作为进入 Common Utilities 的理由；必须先证明没有更明确 owner。
- 不建议在 capability 里直接 `GD.Load<T>("res://...")`。
- 不建议把 `res://` 视为问题本身；它是 Godot 的合法 project root 路径。
- 不建议让 Data 保存 `PackedScene` 或 `Resource` 对象。
- 不建议让 `ResourceCatalog` 在 gameplay 热路径全量刷新。
- 不建议删除 `LoadPath`，因为 DataOS snapshot 中保存资源路径是当前明确边界；要做的是 source policy 和 diagnostics。
- 不建议把 ResourceManagement 描述成“自动防止移动目录报错”的工具。
- 不建议保留 ResourceManagement 作为大而泛的“资源管理器”概念；最终只需要 ResourceLoading 加载工具。
- 不建议由框架仓 generator 默认拥有游戏仓资源。

## 10. 验收关注点

完整命令统一放在 [06-实施路线与验证门禁.md](06-实施路线与验证门禁.md)，本功能文档只记录 ResourceLoading 必须证明什么：

```text
ResourceCatalogDiagnostics
  - duplicate keys = 0
  - missing file paths = 0
  - legacy contains fallback count = 0
  - DataOS resource refs loadable = true for selected smoke records
  - old resource path residue classified = true
```
