# CommonUtilities

> status: current
> owner: tools
> lastReviewed: 2026-06-07

## 定位

`CommonUtilities` 只接收没有明确 Runtime / Capability / Tool owner 的小型纯 helper。当前目录先建立 owner 边界，不迁入旧 `CommonTool`。

## 允许

- 无 Godot scene tree 副作用的纯格式化、轻量转换、只读值对象 helper。
- 输入、输出、副作用和热路径限制都能在代码注释或测试中说明的 helper。

## 禁止

- 资源加载：归 `ResourceLoading`。
- Entity / Component / UI 查询：归 Runtime Entity、Component、UI 或 TargetSelector。
- Timer / 延迟 / 调度：归 Timer。
- 对象池生命周期：归 ObjectPool。
- 目标查询、几何筛选和随机落点：归 TargetSelector / Math。
- Damage / Ability / Feature / Movement 等 capability 公式或业务规则。

## 当前状态

暂无 runtime helper。新增 helper 前必须先证明没有更明确 owner。
