# UI 模块契约

本文是 AI 修改 UIBase、HUD、技能栏、血条、伤害数字和 Entity 绑定 UI 时必须阅读的执行契约。

## 职责边界

UI 是 View 层，不是 Entity Component。

UI 负责：

- 绑定 Entity。
- 监听 `Entity.Events`。
- 根据 Data 变化刷新显示。
- 管理 HUD / 技能栏 / 血条 / 伤害数字等表现。

UI 不负责：

- 执行核心业务逻辑。
- 直接改变战斗结果。
- 作为 Component 挂载到 Entity 下。
- 每帧轮询 Data。

## 核心入口

- `Src/ECS/UI/Core/UIBase.cs`
- `Src/ECS/UI/Core/UIManager.cs`
- `Src/ECS/UI/UI/HealthBarUI/HealthBarUI.cs`
- `Src/ECS/UI/UI/DamageNumberUI/DamageNumberUI.cs`
- `Src/ECS/UI/UI/SkillUI/ActiveSkillBarUI.cs`
- `Src/ECS/UI/UI/SkillUI/ActiveSkillSlotUI.cs`

## 数据 / 事件 / 生命周期

- UI 通过 `Bind(IEntity entity)` 绑定目标。
- `OnBind` 中订阅该实体的 `Entity.Events`。
- `OnUnbind` 中清理 UI 自己持有的额外状态。
- UI 初始绑定后应立即刷新一次显示。
- 伤害数字这类瞬态 UI 可以不做持久 Bind，但必须说明原因。
- 高频 UI 优先走对象池。
- UI 场景加载使用 `ResourceManagement.Load<PackedScene>(typeof(T).Name, ResourceCategory.UI)` 或对象池，不使用旧 `LoadScene<T>()`。
- `UIManager` 通过 `SystemRegistry` 注册，不按旧 AutoLoad 单例文档理解。

## 禁止事项

- 禁止把 UI 做成 Entity Component。
- 禁止监听全局事件后筛选“是不是我的 Entity”作为常规绑定模式。
- 禁止在 `_Process` 每帧读取 Data 刷显示。
- 禁止 UI 直接修改 HP、冷却、技能结果等核心业务状态。
- 禁止 UI 绕过 AbilitySystem / SystemManager 执行业务命令。

## 修改流程

1. 明确 UI 是持久绑定还是瞬态表现。
2. 新建 UI 场景和脚本，优先继承 `UIBase`。
3. 在 `OnBind` 订阅 Entity 局部事件并初始刷新。
4. 高频创建的 UI 接入对象池。
5. 更新 UI README、项目索引或测试说明。

## 推荐测试

- `dotnet build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn --build`
- 涉及视觉预览时运行 `res://Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.tscn`。

## 人工审查重点

- UI 是否保持 View 层边界。
- 事件订阅是否绑定到正确实体。
- 是否存在每帧轮询 Data。
- 对象池 UI 是否正确 Unbind / Reset。
- UI 命令是否绕过系统门禁。
