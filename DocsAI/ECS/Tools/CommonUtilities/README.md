# CommonUtilities

> status: current
> sourcePaths: Src/ECS/Tools/CommonUtilities/
> lastReviewed: 2026-06-07

## 定位

`CommonUtilities` 是受约束的通用工具 owner，不是旧 `CommonTool` 杂物箱。当前只建立边界和禁止项；旧 `CommonTool.LoadPackedScene` 已迁入 `ResourceLoading`，`CommonTool` current 入口已删除。

## 接收标准

- helper 没有更明确的 Runtime、Capability、Tool 或 UI owner。
- helper 输入、输出、副作用、热路径限制可被测试或文档说明。
- helper 不隐式访问全局单例、SceneTree、资源加载、Entity 查询或 Timer。

## 禁止项

- 资源加载、PackedScene path 加载：`ResourceLoading`。
- Entity / Component / UI 查询：对应 Runtime/UI owner，或 `TargetQueryEngine`。
- 计时、调度、延迟：`TimerManager` / Timer owner。
- 对象池生命周期和 parking：ObjectPool owner。
- 目标筛选、随机落点：TargetSelector owner。
- Capability 公式：对应 Damage / Ability / Movement / Feature owner。

## 验证

```bash
rg -n "CommonTool|ResourceLoading|EntityManager|TimerManager|TargetQueryEngine" Src/ECS/Tools/CommonUtilities DocsAI/ECS/Tools/CommonUtilities
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```
