# 08 Tools Docs Code Alignment

## 目标

继续按代码校准 Tools 族源码旁文档，优先处理 ObjectPool、TargetSelector、TimerManager 中会让 AI 生成旧 API 或错误生命周期的说明。

## 本轮处理

- 修正 `Src/ECS/Tools/TargetSelector/README.md`：
  - 目标查询示例从旧 `AbilityTargetTeamFilter.Enemy` 改为当前 `TeamFilter.Enemy`。
- 修正 `Src/ECS/Tools/ObjectPool/ObjectPool.md`：
  - `ObjectPoolInit` 说明从旧 AutoLoad 入口改为 `SystemRegistry + ObjectPoolInitRuntime.OnRegistered`。
  - 对象池名称统一为 `ObjectPoolNames`。
  - 资源加载示例从不存在的 `ResourceManagement.LoadScene<T>()` 改为当前 `ResourceManagement.Load<PackedScene>(..., ResourceCategory.Entity).Instantiate()`。
  - `ObjectPoolManager.ReturnToPool` 说明按当前代码改为 Node 走 MetaData、纯 C# 对象走内部映射。
  - API 表和回收建议同步去掉“仅支持 Node”旧语义。
- 修正 `Src/ECS/Tools/Timer/TimerManager.md`：
  - 生命周期示例中的 `_regenTimer` 改为可空 `GameTimer?`，避免 AI 照抄后产生可空语义问题。

## 不做

- 不改 Tools 运行时代码。
- 不重写 ObjectPool 长设计说明，只修已确认与当前代码不一致的入口和示例。
- 不运行 Godot 场景测试；本轮只跑构建和静态扫描。

## 验证命令

```bash
rg -n "AbilityTargetTeamFilter|LoadScene|\\bPoolNames\\b|AutoLoad|_EnterTree\\(\\).*ObjectPoolInit|_Ready\\(\\).*ObjectPoolInit|仅支持 Godot Node|不支持静态归还|GetTree\\(\\)\\.CreateTimer|new Timer\\(" Src/ECS/Tools -g "*.md"
dotnet build
```
