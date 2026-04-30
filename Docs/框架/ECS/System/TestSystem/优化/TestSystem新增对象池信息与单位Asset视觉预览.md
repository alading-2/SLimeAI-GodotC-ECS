# TestSystem 新增对象池信息与单位 Asset 视觉预览

## Summary

- 在 TestSystem 内新增两个运行时开发模块：
    - 信息.对象池：只读查看所有对象池摘要与单池详情
    - 资源.视觉预览：按分类批量实例化单位 Asset 到当前世界，统一选择动作播放
- 复用并扩展现有 TestSystem + ResourceCatalog + ObjectPool 体系，不新开独立测试场景，不做编辑器插件。
- 第一版聚焦“可观测性 + 批量验收”，不引入高风险管理操作，不引入复杂编辑能力。

## Key Changes

- TestSystem：
    - 在 TestModuleGroupId 新增 信息
    - 在 TestSystem.tscn 的 ModuleHost 挂入两个新模块场景
    - 模块路径固定为 信息.对象池、资源.视觉预览
- 对象池信息模块：
    - 新增对象池观测服务，汇总两类数据：
        - ObjectPoolManager.GetAllStats() 的运行时统计
        - 对象池注册时登记的容量元数据 InitialSize / MaxSize
    - 模块 UI 采用“总览列表 + 详情面板”
    - 第一版只读，不暴露 CleanupAll、DestroyAll
    - 总览至少显示：池名、闲置数、活跃数、总创建数、复用率、异常提示
    - 详情至少显示：Count / ActiveCount / TotalCreated / TotalAcquired / TotalReused / TotalCreatedOnAcquire / TotalReleased / TotalDiscarded / ReuseRate / InitialSize / MaxSize
- ObjectPool / 初始化链路：
    - 在对象池创建时同步登记观测元数据，避免 UI 层反查源码
    - 第一版元数据只要求：池名、InitialSize、MaxSize
- ResourceCatalog：
    - 保持 ResourcePaths.Resources 为唯一索引来源
    - 扩展单位 Asset 分类支持：
        - ResourceCategory.AssetUnitEnemy -> AssetUnit.Enemy
        - ResourceCategory.AssetUnitPlayer -> AssetUnit.Player
    - 不改动现有 Unit.* 配置资源分类语义，避免和 Asset 混淆
- 视觉预览模块：
    - 第一版只消费 AssetUnit.Enemy、AssetUnit.Player
    - 模块 UI 提供：分类选择、动作选择、刷新目录、重建预览、清空预览、状态面板
    - 模块不在面板里做 SubViewport；实例统一挂到当前世界中的稳定预览根节点
    - 预览实例固定网格排布，布局参数先写死在代码里
    - 当前分类动作列表取“并集”，选中动作后：
        - 有该动作的场景循环播放
        - 没有该动作的场景停止播放
    - 只支持现有主路径：AnimatedSprite2D + SpriteFrames
    - 模块失活、面板隐藏、切换分类、手动清空时，统一销毁当前预览实例
- 结构边界：
    - 模块 UI 只负责展示与交互
    - 服务层负责资源/池数据汇总
    - 世界预览控制器负责实例化、排布、统一播动作
    - 两个模块都不依赖当前 SelectedEntity

## Public Interfaces / Types

- 新增对象池观测只读视图模型：
    - 单池统计视图
    - 单池容量元数据视图
    - 聚合详情视图
- 扩展 ResourceCatalog 输出分类范围，但不修改现有调用方式；仍由 GetEntries() / GetGroups() 提供分类结果。
- TestModuleGroupId 新增 信息 常量。
- 若需要稳定世界挂点，新增视觉预览根节点路径常量或预览控制器配置常量。

## Behavior Details

- 对象池模块空态：
    - 无池时显示空列表和“当前没有可观测对象池”
    - 只有统计无容量元数据时，详情区显示“未登记”
- 视觉预览模块空态：
    - 分类无资源时不生成实例
    - 资源加载失败时跳过并记录到状态区
    - 场景不满足 AnimatedSprite2D 主路径时允许列出，但不参与动作控制，并在状态区标记
- 不做内容：
    - 不做特效/投射物预览
    - 不做布局参数运行时调节
    - 不做拖拽摆位
    - 不做动作语义映射
    - 不做对象池管理操作

## Test Plan

- 对象池模块：
    - 能列出 ObjectPoolInit 注册的核心池
    - 运行中统计值刷新正确
    - 单池详情正确显示容量参数
    - 缺失元数据时能降级显示
    - 模块切换、面板显隐后重新进入仍能正确读取
    - 刷新不会改变池状态
- 视觉预览模块：
    - ResourceCatalog 出现 AssetUnit.Enemy、AssetUnit.Player
    - 选分类后在世界预览根节点生成该分类全部实例
    - 实例按固定网格稳定排布
    - 动作下拉列出该分类动作并集
    - 选动作后“可播则循环，不可播则停止”
    - 切换分类、重建、清空、模块失活、面板隐藏后实例正确清理
    - 加载失败或不支持动作控制的资源能输出状态提示
- 回归：
    - ResourceCatalogTestModule 原有配置资源分类继续正常
    - SpawnTestModule 仍只消费 Unit.Enemy
    - TestSystem 模块树新增 信息 后，原有模块注册、切换、默认顺序正常

## Docs And Skill Sync

- 更新 /mnt/e/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Docs/框架/项目索引.md
- 更新 /mnt/e/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Docs/框架/ECS/System/TestSystem.md
- 更新 /mnt/e/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Src/ECS/Base/System/TestSystem/README.md
- 若修改测试系统接入规范或资源目录职责，同步更新相关 Skill 文档，至少覆盖 test-system

## Assumptions

- 第一版保持运行时工具定位，不进入正式玩法链路。
- 视觉预览资源分类命名固定采用 AssetUnit.Enemy / AssetUnit.Player。
- 视觉预览默认在当前世界挂专用根节点，且模块失活即自动清空。
- 视觉预览默认循环播放；缺失动作不回退、不自动改播其他动作。
- 对象池详情第一版只补容量信息，不补 ParentPath / ItemType 等上下文字段。
