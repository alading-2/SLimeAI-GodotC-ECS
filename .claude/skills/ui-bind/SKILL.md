---
name: ui-bind
description: 开发 UI 组件、将 UI 绑定到 Entity、实现响应式 HUD 时使用。适用于：血条/伤害数字/技能栏等 HUD 组件，UI 监听 Entity 状态变化，UIBase Bind 模式实现。触发关键词：UI组件、UIBase、Bind模式、血条、伤害数字、技能栏、HUD、响应式UI、OnBind。
---

# UI Bind 入口

## 什么时候用

- 开发 HUD、血条、伤害数字、技能栏等 UI。
- 让 UI 绑定 Entity 并响应 Data / Event 变化。
- 修改 `UIBase`、`UIManager` 或 UI 对象池。

## 转向其它 Skill

- 核心战斗 / 技能执行 -> `@ability-system` 或 `@damage-system`
- Data 容器语义 -> `@ecs-data`
- EventBus 协议 -> `@ecs-event`
- 高频 UI 对象池 / Timer -> `@tools`

## 必读

- `DocsAI/Modules/UI.md`
- 需要对象池、Timer 或资源加载时读 `DocsAI/Modules/Tools.md`
- 测试选择读 `DocsAI/Tests/测试矩阵.md`

## 最短流程

1. 判断 UI 是持久绑定还是瞬态表现。
2. 优先继承 `UIBase`，通过 `Bind(IEntity entity)` 绑定目标。
3. 在 `OnBind` 订阅该实体的 `Entity.Events` 并立即刷新显示。
4. 在 `OnUnbind` 清理 UI 自己持有的状态。
5. 高频创建的 UI 接入对象池。
6. 运行构建和 MainTest / VisualPreview 等相关场景。
7. 更新 `DocsAI/Modules/UI.md` 或 UI README。

## 禁止事项

- 不要把 UI 做成 Entity Component。
- 不要全局监听后筛选“是不是我的 Entity”作为常规绑定模式。
- 不要 `_Process` 每帧轮询 Data 刷显示。
- 不要 UI 直接修改 HP、冷却、技能结果等核心业务状态。
- 不要 UI 绕过 AbilitySystem / SystemManager 执行业务命令。

## 推荐验证

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn --build
```
