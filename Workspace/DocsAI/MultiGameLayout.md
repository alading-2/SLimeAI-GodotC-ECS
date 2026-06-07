# 工作区多游戏架构

本文档描述 SlimeAI 工作区的整体仓库架构，面向 AI 理解跨目录边界。

## 架构模型

```
SlimeAI 框架仓（唯一可写，remote: alading-2/SlimeAIGameFramework）
   │
   │  git submodule（只读镜像）
   ▼
游戏仓 A（remote: alading-2/BrotatoLike）
游戏仓 B（未来）
游戏仓 C（未来）
```

- **SlimeAI 仓**：独立 git 仓库，框架的唯一事实源。所有框架代码、通用场景、验证场景在此维护。
- **每个游戏仓**：独立 git 仓库，有自己的 `project.godot`。通过 git submodule 持有一份 SlimeAI 的只读镜像。
- **单向数据流**：框架改动只在 SlimeAI 仓提交 → push；游戏仓通过 `git submodule update` 拉取新版本。

## 为什么能工作

游戏仓结构：

```
Games/BrotatoLike/          (游戏仓根 = project.godot 所在 = res:// 根)
├── project.godot
├── BrotatoLike.csproj      (默认 **/*.cs glob)
├── SlimeAI/               (git submodule)
│   ├── GameOS/
│   ├── DataOS/
│   └── Src/Validation/
├── Scenes/Main.tscn
└── Src/Game/
```

机制链路：

1. `BrotatoLike.csproj` 的默认 `**/*.cs` glob 把 `SlimeAI/**/*.cs` 编译进游戏主 assembly。
2. `ScriptPathAttributeGenerator` 基于游戏 csproj 的 `GodotProjectDir`（= 游戏仓根）生成路径。
3. `.tscn` 里 `ExtResource("Script", path="res://SlimeAI/...")` 能正确解析。

结论：SlimeAI 的代码和场景都在游戏的 `res://` 空间内，脚本加载链路完全成立。

## 版本管理

- submodule commit hash = 框架版本号，天然写在游戏仓的 git 记录里。
- 游戏锁定版本：不更新 submodule 指针即可。
- 游戏升级框架：详见 `GitSubmoduleWorkflow.md`。
- SlimeAI 仓可用 tag 标记里程碑版本（如 `v0.3.0`）。

## 禁区

- **游戏仓禁止对 `SlimeAI/` 目录做业务改动**。只允许"submodule 指针前进"（commit hash 变化）。
- **SlimeAI 仓禁止依赖任何游戏专属资源**（禁止出现 BrotatoLike 特有美术路径、特有输入键名）。
- **场景路径必须用 `res://` 绝对路径**，不用相对路径。相对路径在跨目录实例化时会失效。

## 资源路径与 Catalog 归属

`res://` 是当前 Godot 项目的 project root，不是问题本身。在多游戏架构下，同一套框架代码进入游戏仓后路径会自然变成：

```text
游戏仓根 = project.godot 所在 = res://
框架 submodule = res://SlimeAI/
游戏资源 = res://assets/、res://Scenes/、res://Src/Game/ 等游戏仓自有目录
```

资源 catalog 归属规则：

- 框架仓只拥有框架资源 catalog，例如通用场景、验证场景、框架 prefab。
- 游戏仓拥有游戏资源 catalog，例如美术、玩法场景、HUD、游戏 DataOS resource refs。
- 框架 `ResourceLoading` 可以作为统一加载 facade，但不代表框架 `ResourceGenerator` 默认扫描并写入所有游戏资源。
- 后续 generator 应支持在游戏仓根运行，输出 game-local catalog；不要把 BrotatoLike 或其他游戏专属资源写入框架仓 `Data/ResourceManagement/ResourcePaths.cs`。
- 移动资源目录后，用 `resource-path-migration` skill 在当前仓根替换旧路径并检查残留；不要跨框架仓、游戏仓和 `Games/*/SlimeAI/` submodule 混改。

## 场景归属

| 场景类型 | 放哪里 | 原因 |
|----------|--------|------|
| 框架验证场景（Observation、Event 等） | SlimeAI 仓 `Src/Validation/` | 通用，所有游戏复用 |
| 框架 prefab（可选） | SlimeAI 仓 `Scenes/Prefabs/` | 通用组件 |
| 游戏主场景（Main.tscn） | 游戏仓 `Scenes/` | 游戏专属入口 |
| 游戏玩法场景 | 游戏仓 `Scenes/Game/` | 游戏专属 |
| 游戏美术资产场景 | 游戏仓 `assets/` | 游戏专属 |

## 当前实例

| 仓库 | Remote | 角色 |
|------|--------|------|
| `/home/slime/Code/SlimeAI/SlimeAI` | `alading-2/SlimeAIGameFramework` | 框架仓（唯一可写） |
| `/home/slime/Code/SlimeAI/Games/BrotatoLike` | `alading-2/BrotatoLike` | 第一个游戏仓 |
| `Games/BrotatoLike/SlimeAI/` | submodule → SlimeAIGameFramework | 只读镜像 |

## csproj 配置

游戏 csproj 通过 `ProjectReference` 引用 submodule 内项目：

```xml
<ProjectReference Include="SlimeAI/GameOS/SlimeAI.GameOS.csproj" />
```

- `ScriptPathAttributeGenerator` 基于游戏 csproj 的 `GodotProjectDir`（= 游戏仓根）生成路径。
- 游戏 csproj 默认 `**/*.cs` glob 会自动包含 `SlimeAI/` submodule 目录下的源码；需用 `<Compile Remove="SlimeAI/Tests/**/*.cs" />` 排除测试文件。

## 相关文档

- submodule 操作细节：`GitSubmoduleWorkflow.md`
- 工作区打开方式：`OpenWorkspace.md`
