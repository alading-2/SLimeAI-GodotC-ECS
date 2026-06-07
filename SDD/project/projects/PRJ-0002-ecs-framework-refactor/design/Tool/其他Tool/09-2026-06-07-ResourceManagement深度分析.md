# ResourceManagement 深度分析

> 更新：2026-06-07
> 状态：current deepthink decision, user-calibrated
> 范围：`Data/ResourceManagement/`、`Tools/ResourceGenerator/`、`ResourceCatalog`、DataOS resource ref、业务资源加载入口。

## 1. Goal

本轮回答一个更底层的问题：SlimeAI 自建 `ResourceManagement + ResourceGenerator + ResourcePaths` 这种资源路径管理工具是否合理，是否只是项目局部绕路，以及它能不能真正解决“移动目录后路径全炸”的问题。

2026-06-07 用户校准后，本文件的核心修正是：

```text
res:// 本身不是问题。
它是 Godot 的项目根路径格式，在游戏仓、框架 submodule、.tscn/.tres 和 DataOS resource ref 中都合理存在。
真正的问题是：谁拥有这些路径、路径变更如何自动迁移、是否有 diagnostics 证明旧路径已清零。
```

非目标：

- 本轮不修改 C# runtime 实现。
- 本轮不把 Godot 资源系统替换成自研资源系统。
- 本轮不引入 Unity Addressables / Unreal Asset Manager 式依赖或复杂打包流水线。
- 本轮不把 BrotatoLike 资产路径上提为框架默认。

## 2. Context Read

已读本地事实源：

- `Data/ResourceManagement/README.md`
- `Data/ResourceManagement/ResourceManagement.cs`
- `Data/ResourceManagement/ResourceCatalog.cs`
- `Data/ResourceManagement/ResourcePaths.cs`
- `Data/ResourceManagement/ResourceCategory.cs`
- `Tools/ResourceGenerator/README.md`
- `Tools/ResourceGenerator/ResourceGenerator.cs`
- `DocsAI/ECS/README.md`
- `.ai-config/skills/core/tools/SKILL.md`
- `design/Tool/其他Tool/02-CommonTool与ResourceManagement裁决.md`
- `design/Tool/其他Tool/07-2026-06-04-AI-first完全重构校准.md`
- `design/Tool/其他Tool/08-2026-06-04-用户裁决后执行前复核.md`
- 调用点搜索：`ResourceManagement.Load`、`LoadPath`、`ResourcePaths`、`CommonTool.LoadPackedScene`、`GD.Load`、`res://`

Git boundary：

- 当前仓：`/home/slime/Code/SlimeAI/SlimeAI`
- 本轮只更新文档和 skill 源，不改运行时代码。

未覆盖：

- 未跑 Godot editor 内部重命名/移动资源行为验证。
- 未确认当前 Godot 导出包中 `uid://` 对所有 `.tscn/.tres` 路径的实际可用性。
- 未检查所有 `.tscn` 内部 `ext_resource path="res://..."` 是否已经带 `uid://`。

## 3. Evidence / Search Coverage

### 3.1 当前实现证据

`ResourceGenerator` 扫描：

- `assets/Effect`
- `assets/Unit`
- `assets/Unit/Player`
- `assets/Unit/Enemy`
- `assets/Projectile`
- `Src/ECS/Runtime`
- `Src/ECS/Capabilities`
- `Src/ECS/Tools`
- `Src/ECS/UI`
- `Src/ECS/Test`

它生成 `Data/ResourceManagement/ResourcePaths.cs`，内容包括：

- `ResourcePaths.<Category>_<Name>` 常量。
- `Dictionary<ResourceCategory, Dictionary<string, ResourceData>> Resources`。
- `ResourceData.Path` 仍是 Godot `res://...` 路径。

`ResourceManagement` 当前：

- `Load<T>(name, category)` 先按 key 精确匹配。
- 精确匹配失败后用 `kvp.Key.Contains(name, OrdinalIgnoreCase)` fallback。
- 最终调用 `Godot.GD.Load<T>(data.Path)`。
- `LoadPath<T>(path)` 接受任意 path，缺 source policy。
- 失败只 log，返回 `null`。

`ResourceCatalog` 当前：

- Data 目录条目来自 `runtime_snapshot.json` records。
- Asset 目录条目来自 `ResourcePaths.Resources`。
- UI / TestSystem 选择器可用目录分组，但没有生成 stale/missing/duplicate 的正式 diagnostics artifact。

调用点搜索显示：

- 大多数 System/UI/ObjectPool 已通过 `ResourceManagement.Load<PackedScene>(key/name, category)` 加载。
- `CommonTool.LoadPackedScene` 仍包装 `ResourceManagement.LoadPath<PackedScene>`，被 `EntitySpawnPipeline`、`EffectTool`、`ChainLightning` 调用。
- UI 仍有少量 `DEFAULT_SKILL_ICON = "res://icon.svg"` 和 `LoadPath<Texture2D>("res://icon.svg")`。
- `.tscn` 文件内部仍大量保存 `ext_resource path="res://..."`，这是 Godot 场景格式层事实，不属于 C# 业务直接加载入口。

### 3.2 `res://` 事实校准

`res://` 是 Godot project root 路径，不是 SlimeAI 需要规避的坏格式。尤其在未来多游戏仓结构中：

- 游戏仓根是 `project.godot` 所在目录，也是 `res://` 根。
- 框架作为 submodule 放进游戏仓后，框架资源路径会自然变成 `res://SlimeAI/...`。
- 游戏自己的资源仍在游戏仓 `res://assets/...`、`res://Scenes/...` 或游戏自定义目录下。
- `.tscn` / `.tres` 内部资源引用、Godot C# `ResourceLoader.Load`、场景 runner 参数都天然使用 `res://`。

所以需要反对的不是 `res://`，而是：

- 业务 Capability 自己硬编码并绕过资源加载 owner。
- 目录移动后靠人工全仓搜索修路径。
- 没有 generator / diagnostics 证明 `ResourcePaths`、DataOS snapshot、`.tscn/.tres`、DocsAI 示例是否仍残留旧路径。

### 3.3 外部资料证据

externalResources:

```yaml
enabled:
  - engine-framework
  - official-docs
scope:
  - Resources/Engine/Docs/FrameworkAnalysis/Reports 中 resource/cache/AssetManager/Capability-owned selector 片段
  - Context7 /godotengine/godot-docs 的 ResourceLoader/PackedScene 加载资料
  - Context7 /needle-mirror/com.unity.addressables 的 AssetReference/GUID 资料
  - Web 搜索 Epic Unreal Asset Management 官方文档
reason: 判断 SlimeAI 自建资源 manifest 是否是合理轻量方案，以及和主流引擎资源引用机制的差异
expires: current-task
copiedCodeOrAssets: none
```

Evidence：

- Godot 官方文档：C# 可通过 `ResourceLoader.Load<PackedScene>()` 或 `GD.Load<PackedScene>()` 加载 `res://` 路径并实例化；Godot `ResourceUID` 用 `uid://` 维护资源唯一标识，使资源移动/重命名时引用保持稳定。
- Godot 官方文档还说明 `ProjectSettings.get_global_class_list()` 返回的脚本和图标路径是项目文件系统本地路径，通常以 `res://` 开头。这进一步证明 `res://` 是 Godot API 层的正常事实。
- Unity Addressables 官方资料：`AssetReference` 是可序列化资源引用，`AssetGUID` 保存 asset GUID，可用 `Addressables.LoadAssetAsync` 加载；Addressables 推荐用 `AssetReference` 替代裸字符串地址，以便延迟加载和编辑器赋值。
- Unreal 官方 Asset Management：`Asset Manager` 管理 Primary Asset 的发现、加载、卸载和审计；Primary Asset ID 由类型和名称构成，配置可包含扫描目录、排除目录和 redirect。
- 本地 EnTT 报告：EnTT resource cache 的 stable id / loader / handle 模式只能作为 `ResourceCatalog` dump / validation 的参考；SlimeAI 不应替换 Godot 资源系统。
- 本地综合报告：SlimeAI 应继续保持小 Runtime kernel、manifest、Observation 和 capability-owned selector，不复制外部 ECS runtime 或完整引擎资产系统。

Inference：

- 其他引擎“看起来没有这么做”，主要是因为它们已经有编辑器级 GUID、address、soft reference、AssetManager、Reference Viewer、redirect 和 cooking/chunking 工具链；不是因为“业务代码散落物理路径”是推荐做法。
- SlimeAI 的 `ResourcePaths.cs` 更像轻量自建 Addressables / AssetManager manifest：它把资源发现、分类和可检查索引集中起来，但不负责真实资源生命周期、异步加载、打包、依赖分析或自动 redirect。
- 用户提出的“移动路径不靠人工全仓搜索，而靠工具替换 + diagnostics 暴露”是 AI-first 框架应有的方向；这应做成 Resource Path Migration workflow，而不是让 `ResourceManagement` 假装自己能自动修复所有移动。

Unknown：

- Godot `uid://` 在当前 Godot C# 项目、导出包、DataOS snapshot、ResourceGenerator、跨 submodule 游戏仓中的完整链路是否足以作为主存储格式，还需要专门验证。
- 若未来框架仓与游戏仓分离，当前生成器固定扫描框架仓目录并写框架仓 `ResourcePaths.cs` 的模式是否仍合适；本轮裁决为“不应让框架 generator 默认拥有游戏资源 catalog”。

## 4. Problem Reality Check

问题真实存在，但要拆成三层：

1. `res://` 作为 Godot project-root 路径没有问题。
2. 业务 C# / DataOS / docs 示例 / `.tscn/.tres` 中散落无 owner 的路径，确实会在目录移动后产生隐性断点。
3. 当前 `ResourceManagement` 只能降低“散落路径”的面积，不能单独保证“移动目录不报错”。

原因很直接：`ResourcePaths.cs` 仍保存 Godot 项目相对路径。移动资源后，如果没有重新运行 `ResourceGenerator`，generated manifest 仍然是旧路径；如果 DataOS snapshot、`.tscn/.tres`、DocsAI 示例或 UI fallback 中保存了旧 `res://`，它们也不会自动更新。

所以这套工具的真实价值不是“路径永远不会坏”，而是：

- 把大部分 C# 业务加载入口收敛到一个统一 owner。
- 让 AI 不需要全仓猜路径语义。
- 让路径变更后有一个集中重新生成和检查点。
- 为资源选择器、TestSystem、视觉预览和 diagnostics 提供统一目录。

它当前还缺少：

- stale generated source 检查。
- missing path / duplicate key artifact。
- DataOS resource ref 和 generated key 的一致性检查。
- `Load<T>` 精确 key fail-fast。
- `LoadPath<T>` source policy。
- structured failure result。
- Resource Path Migration workflow：移动目录后自动替换旧路径、重新生成 manifest、检查旧路径残留。

## 5. Idea Check

用户当前思路“做资源路径管理，避免移动目录就报错”方向成立，但目标表述要校准：

成立部分：

- 统一加载 owner 和可检查 manifest 是必要的，尤其适合 AI-first 框架。
- 资源加载必须有唯一 owner，Capability 不应直接 `GD.Load("res://...")`。
- 路径变更应该通过工具生成和 diagnostics 暴露，不应靠人工全仓搜索。
- 新增“资源路径迁移 skill / workflow”是合理补位：它处理真实移动后的文本引用替换与残留检查。

不完全成立部分：

- 仅生成 `ResourcePaths.cs` 不能自动抵抗文件移动。
- 如果 generated key 仍来自文件名，且同分类资源名可冲突，移动/重命名后还是可能漂移。
- `Contains` fallback 会把“路径错误”伪装成“加载了另一个相近资源”，这比立刻报错更危险。
- 如果框架仓和游戏仓分离，框架仓内的 `ResourcePaths.cs` 不应尝试成为所有游戏资源的全局事实源；游戏资源 catalog 必须由游戏仓拥有。

更准确的定位应是：

```text
ResourceManagement / ResourceLoading 是 SlimeAI 的统一资源加载 facade。
ResourceGenerator / ResourcePaths 是当前框架仓的 generated manifest。
Resource Path Migration workflow 才是目录移动后的路径替换和残留检查工具。
它不是 Godot ResourceUID、Unity Addressables 或 Unreal Asset Manager 的完整替代。
```

## 6. Problem Shape

### 思路问题

- 不能把“禁止业务裸加载”误写成“`res://` 本身有问题”。
- 不能把“减少硬编码路径”误写成“移动目录不会出错”。
- 不能用 contains fallback 追求宽容加载；AI-first 下错误资源比启动失败更难查。
- 不能把 `LoadPath` 完全删除，因为 DataOS snapshot 当前确实需要保存资源引用；但必须标明 source。
- 不能让 `ResourceManagement` 同时承担“统一加载、资源身份、路径迁移、跨游戏 catalog、打包系统”所有职责。

### 信息缺口

- 当前是否愿意把 DataOS resource ref 从 `res://` 迁到 `ResourceKey + Category` 或 `uid://`。
- Godot `uid://` 在当前 C# / snapshot / export / submodule 场景下是否稳定可用。
- 资源加载失败是要 throw、返回 structured result，还是 startup preflight 失败后禁止进入游戏。
- 游戏仓分离后，每个游戏是否需要自己的 `GameResourcePaths` / `GameResourceCatalog` 输出位置和生成命令。

### 决策未定

- `ResourcePaths` key 长期是否仍用文件名，还是引入显式 `ResourceId` descriptor。
- `ResourceGenerator` 是否进入默认验证门禁，资源变更未生成就失败。
- `ResourceCatalogDiagnostics` 是 CLI、Godot TestSystem 模块，还是两者都要。
- `resource-path-migration` skill 是否只替换文本引用，还是也负责 `git mv` / Godot editor 移动操作。默认建议：文件移动由用户、Godot editor 或 `git mv` 完成；skill 负责替换引用和验证。

## 7. Main Risks

| 风险 | 说明 | 处理方式 |
| --- | --- | --- |
| stale manifest | 移动资源但忘记跑生成器，`ResourcePaths.cs` 仍指旧路径。 | 增加 generator validate / stale check，CI 或本地 gate 必跑。 |
| key 冲突 | 生成 key 当前主要取文件名，同分类重名只能 warning / skip。 | diagnostics 把 duplicate keys 作为 error；未来引入显式 ResourceId。 |
| fallback 错载 | `Contains` fallback 可能加载相近名字资源。 | 删除 fallback，精确 key 不存在即 structured fail-fast。 |
| LoadPath 滥用 | 业务可以绕过 manifest 直接传 `res://`。 | `LoadPath<T>(path, source)` 必须带 `ResourceLoadSource` 和 usage/owner。 |
| 过度设计 | 直接复制 Addressables/AssetManager 会引入打包、远程内容、异步句柄复杂度。 | 当前只做 manifest、strict load、diagnostics；异步/cache/unload 等待真实需求。 |
| 与 Godot UID 重复 | Godot 已有 `ResourceUID`，自研 key 可能形成第二套身份。 | 短期把 UID 作为 path 稳定性参考，不替换 manifest；后续用实验决定是否接入。 |
| 跨仓所有权混乱 | 框架仓 generator 如果扫描游戏资源，会让框架事实源依赖游戏仓。 | 框架只拥有框架资源；游戏仓生成游戏 catalog；统一加载 facade 可消费多个 catalog。 |
| 路径迁移误替换 | 简单字符串替换可能改到历史文档、日志、第三方目录或不该改的示例。 | skill 默认 dry-run，限制作用域，排除 `.git/.godot/bin/obj`，替换后必须 `rg` 查旧路径残留。 |

## 8. Options

### Option A：删除 ResourceManagement，回到 Godot 直接路径

优点：实现最简单。

缺点：违背本仓规则和 AI-first 目标；路径散落后更难审计；DataOS/TestSystem/视觉预览失去统一目录。

结论：Reject。

### Option B：保留但大量简化为 ResourceLoading facade + generated catalog + diagnostics

做法：

- 继续保留统一加载入口，但把概念从“路径管理器”降级为“ResourceLoading facade”。
- `ResourceGenerator -> ResourcePaths` 作为当前框架仓 generated catalog，而不是所有游戏资源的全局身份系统。
- `ResourceCatalog` 继续服务 UI/Test/AI 可检查目录。
- 删除 `Contains` fallback。
- `LoadPath` 增加 source policy。
- 增加 `ResourceLoadRequest / ResourceLoadResult`。
- 增加 `ResourceCatalogDiagnostics`，覆盖 missing path、duplicate key、stale generated source、DataOS resource refs loadable。
- 把 `ResourceGenerator` 加入资源变更验证门禁。
- 新增 `resource-path-migration` SystemAgent skill，用于移动目录后的引用替换和旧路径残留检查。

优点：贴合当前 Godot C# 项目和 AI-first 目标，成本可控。

缺点：仍依赖生成器及时运行；不自动解决所有资源移动问题。

结论：Recommended。

### Option C：升级为完整资源身份系统

做法：

- 引入显式 `ResourceId` descriptor。
- DataOS 记录 `ResourceId + Category` 或 `uid://`，不直接记录 `res://`。
- 生成 manifest 包含 id、category、path、uid、type、owner、source。
- 支持 redirect、deprecated id、migration。

优点：长期最稳，最接近 Unity/Unreal 的资源身份层。

缺点：工作量明显大；需要 DataOS schema、generator、validator、Godot export 和 UI 选择器同步设计。

结论：Adopt Later，作为 Resource Loading Hard Cutover 之后的下一阶段。

## 9. Recommendation

采用 Option B，但把命名和职责进一步收窄：

```text
ResourceManagement 当前保留为兼容名，目标语义是 ResourceLoading facade。
ResourcePaths 是框架仓当前 generated catalog，不是跨游戏全局资源身份。
ResourceCatalog 是 UI/Test/AI 可检查目录。
ResourcePathMigration skill/workflow 负责目录移动后的文本引用替换、生成器刷新和旧路径残留检查。
Godot ResourceLoader / ResourceUID 仍是底层引擎事实。
```

我认为当前 `ResourceManagement` 的目标方向合理，但实现形态确实偏冗余，后续可以删掉或大量简化“路径管理”语义，只留下统一加载、source policy、structured diagnostics 和 catalog 读取。其他引擎“没有这么做”的表象不应误导判断；它们通常有更重的编辑器资产引用系统。SlimeAI 当前没有完整 Addressables / AssetManager，轻量 facade + generated catalog + diagnostics 是合适折中。

但必须接受一个边界：它不是路径自动修复器。要让“移动目录不炸”变成工程事实，至少需要：

- 资源移动后生成器自动或强制运行。
- diagnostics 能发现 `ResourcePaths` 指向不存在文件。
- DataOS resource refs 也被纳入检查。
- 业务调用只用 key/category 或带 source 的 path，不散落裸 `res://`。
- Resource Path Migration skill 能在当前工作目录替换旧路径为新路径，并验证旧路径残留。

## 10. Future Split：框架仓 / 游戏仓分离后的资源所有权

未来游戏和框架分离后，资源路径要按 Godot project root 理解：

```text
游戏仓根 = project.godot 所在 = res://
框架 submodule = res://SlimeAI/
游戏资源 = res://assets/、res://Scenes/、res://Src/Game/ 等游戏仓自有目录
```

因此资源 catalog 的 owner 不能只有一个：

| 资源类型 | 路径形态 | Catalog owner | 说明 |
| --- | --- | --- | --- |
| 框架场景 / 测试 / prefab | `res://SlimeAI/Src/ECS/...` | 框架仓提供默认 catalog 规则，游戏仓消费 submodule 后可重新生成或引用 | 框架不能依赖具体游戏美术。 |
| 游戏单位 / 特效 / UI / 玩法场景 | `res://assets/...`、`res://Scenes/...` | 游戏仓自己的 generator / catalog | 游戏资源不能写入框架仓事实源。 |
| DataOS resource refs | 当前多为 `res://...` | 写入哪个仓的 DataOS，就由哪个仓校验 | 框架 DataOS 只校验框架数据；游戏 DataOS 校验游戏数据。 |
| `.tscn/.tres` 内部引用 | `res://...` 或未来 `uid://...` | Godot 项目本身 | 不由 ResourceManagement 重写引擎语义。 |

推荐后续目标：

```text
ResourceLoading facade 位于框架。
ResourceCatalogProvider 支持多个 catalog：framework catalog + game catalog。
ResourceGenerator 支持 --project-root / --scan-root / --output 参数，可在游戏仓根运行。
框架仓默认只生成框架 catalog；游戏仓生成游戏 catalog 或合并后的 game-local catalog。
```

当前不要让 `Tools/ResourceGenerator` 扫描外部游戏仓并把结果写回框架仓 `Data/ResourceManagement/ResourcePaths.cs`。这会把框架仓变成某个游戏的资源事实源，后续 submodule 模式会失败。

## 11. `uid://` 校准

用户提到的 `.cs.uid` 文件和 Godot `uid://` 不是一回事：

- `.cs.uid` 是 Godot 4 给脚本文件生成的旁路 UID 文件，本仓已经在 `.gitignore` 中忽略。
- Godot `ResourceUID` 是资源级 UID 系统，可通过 `uid://` 引用资源，并用于在文件重命名或移动时保持资源间引用。

这说明 `uid://` 对“移动资源不坏”确实有意义，但本轮不建议直接把 DataOS 或 ResourcePaths 全部迁到 `uid://`，原因是：

- 当前 DataOS、ResourceGenerator、ResourceCatalog、Godot C# 加载调用和 submodule 路径都围绕 `res://` 工作。
- Godot 4.4+ 的导出文件路径行为存在版本差异，`@export_file` 可能写 `uid://`，Godot 4.5+ 又提供保留路径行为的注解。
- `uid://` 是否能覆盖 C# 代码加载、snapshot JSON、导出包、游戏仓 submodule 和生成器 diff，还要实测。

后续建议单独做一个 `Resource UID Adoption Spike`：

1. 创建小资源和场景引用，记录 `res://` 与 `uid://` 的加载行为。
2. 移动资源目录，验证 `.tscn/.tres`、C# `GD.Load` / `ResourceLoader.Load`、DataOS snapshot 中的引用是否仍可加载。
3. 在游戏仓 submodule 形态验证 `res://SlimeAI/...` 与 `uid://` 的关系。
4. 只在证据证明稳定后，考虑让 diagnostics 记录 UID 或允许 DataOS resource ref 接收 `uid://`。

## 12. Resource Path Migration Skill 设计

新增 skill 目标：

```text
当移动/重命名 Godot 资源文件或目录后，在当前工作目录内替换旧路径为新路径，并检查旧路径残留。
```

默认流程：

1. 确认当前 git boundary，不跨仓混改。
2. 记录旧路径和新路径，支持 `res://...` 或项目相对路径。
3. 先 dry-run，列出将修改的文本文件。
4. 应用替换时只处理文本文件，排除 `.git/`、`.godot/`、`bin/`、`obj/`、`.ai-temp/`。
5. 替换后运行 `rg -n "<old_path>" .`，旧路径残留必须有解释。
6. 若当前目录有 `project.godot` 和 ResourceGenerator，运行 `dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj`。
7. 按仓库类型运行 build / DataOS validate / SDD validate / grep gate。

不建议让 skill 默认执行文件移动本身。文件移动最好由 Godot editor、`git mv` 或用户明确命令完成；skill 处理移动后的引用迁移和验证闭环。

## 13. Must Confirm

### 思路问题

暂无。用户已确认：路径变更应该通过工具生成和 diagnostics 暴露，不靠人工全仓搜索；当前 `ResourceManagement` 可以删掉或大量简化，但仍需要统一资源加载工具。

### 信息缺口

1. DataOS resource ref 未来是否允许从 `res://` 迁到 `ResourceKey + Category` 或 `uid://`？  
   为什么问：这决定 ResourceLoading Hard Cutover 是否只改加载 API，还是要改 DataOS schema。  
   默认值 / 影响：默认本轮不改 DataOS schema，只给 `LoadPath` 加 source policy；未来另起 resource identity SDD。

2. Godot `uid://` 是否要进入下一阶段验证？  
   为什么问：Godot UID 正是解决移动/重命名引用稳定性的引擎机制，但当前项目没有验证 C#、snapshot、导出和 submodule 的完整链路。  
   默认值 / 影响：默认先不把 `uid://` 作为主存储，只在 diagnostics 中记录可选 UID 研究项。

### 决策未定

1. 资源加载失败最终是 `throw`、返回 `ResourceLoadResult`，还是 preflight 失败后禁止启动？  
   为什么问：这会影响所有调用点签名和测试写法。  
   默认值 / 影响：默认提供 `TryLoad` / structured result，同时 startup/preflight 可 fail-fast；不在 gameplay 热路径到处抛未捕获异常。

## 14. Should Confirm

1. `ResourceGenerator` 是否进入默认验证门禁。默认建议进入文档和手动验证，CI/commit hook 另行确认。
2. `ResourceCatalogDiagnostics` 优先做 CLI 还是 TestSystem UI。默认建议先 CLI / pure C# 输出 artifact，再接 TestSystem UI。
3. `ResourcePaths` key 是否继续用文件名。默认短期继续用文件名，并把同分类 duplicate 变 error；显式 `ResourceId` 留到下一阶段。
4. `resource-path-migration` skill 是否后续升级为更强脚本，直接支持批量 JSON manifest 输入。默认本轮只做 old/new 一次替换。

## 15. Defaults I Will Use

如果用户回复“按建议执行”，后续 Resource Loading Hard Cutover 默认采用：

- 保留统一资源加载工具，但允许把 `ResourceManagement` 重命名或简化为 `ResourceLoading`。
- 保留 `ResourceCatalog` / `ResourceGenerator` 的轻量 manifest 价值，但不让它承担跨游戏全局资源身份。
- 删除 `ResourceManagement.Load<T>` 的 contains fallback。
- `LoadPath` 改为必须携带 `ResourceLoadSource`、owner、usageName。
- 新增 structured result，不只依赖 log。
- 新增 ResourceCatalogDiagnostics，至少检查 duplicate key、missing path、stale generated source、DataOS selected refs loadable。
- `CommonTool.LoadPackedScene` 迁入 ResourceLoading，当前 `CommonTool` 删除或 internal 化。
- 不引入 Unity Addressables / Unreal Asset Manager 依赖。
- 不把 Godot `uid://` 直接升为主存储，先做验证和可选记录。
- 新增 `resource-path-migration` skill：默认在当前工作目录替换路径并用 `rg` 验证旧路径残留。
- 未来游戏仓分离后，框架 ResourceGenerator 不默认扫描游戏资源；游戏仓有自己的 catalog 输出或在游戏仓根运行 generator。

## 16. Not Recommended

- 不建议删除统一加载工具后直接在业务里散落 `GD.Load("res://...")`。
- 不建议继续保留 contains fallback。
- 不建议让 Data 保存 `PackedScene` / `Resource` 运行时对象。
- 不建议让业务 Capability 直接接受裸 `res://`。
- 不建议把 `ResourceCatalog` 放到 gameplay 热路径全量刷新。
- 不建议现在复制 Addressables 的远程内容、异步句柄和依赖计数全套模型。
- 不建议现在复制 Unreal Asset Manager 的 Primary/Secondary Asset、Bundle、Chunking 和 redirect 全套模型。
- 不建议把 `res://` 当成错误本身；它是 Godot 项目路径事实。
- 不建议在框架仓 generated `ResourcePaths.cs` 中写入某个游戏的专属资源路径。
- 不建议把 `.cs.uid` 文件和 Godot `ResourceUID` / `uid://` 混为一谈。

## 17. Artifact Updates

本轮写入：

- 本文件：`design/Tool/其他Tool/09-2026-06-07-ResourceManagement深度分析.md`
- `design/Tool/其他Tool/11-2026-06-07-ResourcePathMigrationSkill设计.md`
- `DocsAI/ECS/Tools/ResourceManagement/README.md`
- `DocsAI/ECS/README.md`
- `Data/ResourceManagement/README.md`
- `Tools/ResourceGenerator/README.md`
- `Workspace/DocsAI/MultiGameLayout.md`
- `Workspace/DocsAI/GitSubmoduleWorkflow.md`
- `.ai-config/skills/core/resource-path-migration/SKILL.md`
- `.ai-config/skills/core/tools/SKILL.md`
- `Workspace/SystemAgent/Registry/skills.yaml`
- `design/Tool/其他Tool/README.md`
- `design/INDEX.md`
- `Core/roadmap.md`
- `Core/progress.md`
- `Core/notes.md`

本轮不写入：

- `Src/ECS/**` runtime 代码。
- `Data/DataOS/**` schema / snapshot。
- 新执行型 SDD。
