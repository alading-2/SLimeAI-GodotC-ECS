# SlimeAI 多仓库 AI 工作流协议

本文定义 AI CLI 在 `SlimeAI` 多仓库结构下如何打开项目、读取上下文、引用框架和处理跨仓库修改。

## 1. 基本原则

- 做框架时打开 `SlimeAI` 框架仓库。
- 做游戏时打开对应 `Games/<GameName>` 游戏仓库。
- 做 Godot 引擎 trace 或底层修复时打开 `Engine` 目录。
- 默认不在一个 AI 会话里扫描整个 `/home/slime/Code/SlimeAI`。
- 游戏仓库默认只改游戏内容；框架 bug 需要切换到框架仓库修复并发布新版本。

## 2. 推荐打开目录

```text
框架开发：
  cd /home/slime/Code/SlimeAI/SlimeAI

游戏开发：
  cd /home/slime/Code/SlimeAI/Games/BrotatoLike

Godot 引擎调试：
  cd /home/slime/Code/SlimeAI/Engine/godot-4.6.2-stable
```

不要为了方便在顶层工作区直接让 AI 全局搜索所有游戏。

## 3. 游戏仓库必须提供的框架映射

每个游戏仓库必须包含：

```text
DocsAI/ExternalFrameworkMap.md
```

建议内容：

```yaml
framework_name: SlimeAI Godot Game OS
framework_version: 0.1.0
framework_source_path: ../../SlimeAI
framework_package: SlimeAI.GameOS
dataos_schema_version: 0.1.0
godot_version: 4.6.2
engine_source_path: ../../Engine/godot-4.6.2-stable
package_mode: project-reference
```

AI 在游戏仓库中遇到框架 API、构建错误或运行时栈时，先读本文件，再按需读取框架契约或源码。

## 4. 游戏仓库 Skill 边界

游戏仓库 `.codex/skills` 只放入口型 Skill：

```text
project-index       # 本游戏导航
game-development    # 游戏内容开发
gameos-reference    # 框架引用和契约查询
data-authoring      # 本游戏数据修改
godot-scene-test    # 本游戏场景测试
```

框架深层 Skill，例如 `movement-system`、`collision-system`、`damage-system`、`ecs-component`，默认保留在 `SlimeAI` 框架仓库。游戏仓库不激活全部框架 Skill，避免 AI 在普通游戏任务中误改框架内部。

## 5. 什么时候读框架源码

允许读取：

- 游戏编译错误指向 `SlimeAI.GameOS` API。
- 游戏运行日志或栈追踪进入框架代码。
- 需要确认框架能力契约、DataOS schema 或 Capability manifest。
- 用户明确要求升级、修复或扩展框架。

优先级：

```text
1. 游戏本地 DocsAI/ExternalFrameworkMap.md
2. 框架发布包附带的 Contracts / ApiIndex / DebugGuide
3. 框架源码
4. 框架迁移计划和历史文档
```

禁止默认从游戏任务开始就全仓库搜索所有框架和其它游戏。

## 6. 跨仓库修改规则

普通游戏任务：

```text
允许：
  - 修改当前游戏 Data / Assets / Scenes / Src/Game / DocsAI
  - 使用当前锁定版本的 GameOS API

禁止：
  - 直接修改 ../../SlimeAI 源码
  - 直接覆盖框架 package
  - 隐式升级框架版本
```

框架修复任务：

```text
1. 切换到 SlimeAI 框架仓库。
2. 修复 GameOS / DataOS / Agent / Tools。
3. 运行框架验证。
4. 发布本地 NuGet / DLL / package。
5. 回到游戏仓库升级版本。
6. 运行游戏回归。
```

Godot 引擎任务：

```text
1. 切换到 Engine 目录。
2. 只做 trace、底层问题确认或明确引擎补丁。
3. 不把游戏框架代码放入引擎源码。
4. 输出引擎补丁版本和 GameOS 依赖说明。
```

当前 Godot 4.6.2 源码构建入口：

```text
/home/slime/Code/SlimeAI/Engine/Tools/build-linux-editor-mono.sh
```

若本机缺少 `SCons`，先记录阻塞，不要擅自安装依赖；待依赖可用后再构建 CLI 并更新 `GODOT_BIN`。

## 7. 包引用规则

游戏运行时依赖框架 package，不复制框架源码。

迁移期允许：

```text
project-reference
```

稳定后优先：

```text
local-nuget
```

长期发布：

```text
versioned-nuget / dll
```

每次游戏升级框架版本，必须记录：

```text
旧版本
新版本
breaking changes
DataOS schema 变化
迁移脚本
验证命令
结果
```

## 8. AI 最终回复要求

跨仓库任务最终回复必须说明：

- 当前打开的仓库。
- 是否读取了外部框架源码。
- 是否修改了框架仓库。
- 游戏当前依赖的 GameOS 版本。
- 运行了哪些验证命令。
- 是否需要后续发布或升级 package。
