# 独立视觉预览场景重构设计

## 背景

现有 `Src/ECS/Base/System/TestSystem/VisualPreview` 把视觉预览作为 `TestSystem` 模块存在，这与当前需求不再匹配：

- 视觉预览需要独立测试场景，独立运行，不再依赖 `MainTest` 或 `TestSystem`
- 预览实例需要支持鼠标选择，并显示实体名、资源路径、分类等信息
- 预览对象不再是单纯的场景节点，而是可被 `MouseSelectionSystem` 选中的轻量 `Entity`
- 资源来源需要统一以 `ResourcePaths.Resources` 中的全部 `Asset*` 分类为准，而不是只处理 `AssetUnit`

这次重构的目标，是把“批量摆放视觉资源并统一验收表现”的能力，从 `TestSystem` 模块迁移为一套独立的全局测试场景与专用预览实体体系。

## 目标

构建一个独立的视觉预览测试场景，运行后可以：

- 从 `ResourcePaths.Resources` 中收集全部 `ResourceCategory.Asset*` 条目
- 按分类批量生成预览实体，并在地图上按固定网格摆放
- 左侧通过可折叠调试面板筛选分类、切换统一动作、查看当前选中资源信息
- 点击或框选预览实体时，显示选中实体名称、资源键、资源路径、分类、默认动作、当前动作等必要信息
- 统一复用 `UnitAnimationComponent` 管理 `AnimatedSprite2D` 资源的动作切换，而不是维护一套平行的动画控制器

## 非目标

本次不做以下内容：

- 不再把视觉预览接回 `TestSystem`
- 不做编辑器插件
- 不做拖拽摆位或运行时自定义网格参数面板
- 不为 `Projectile` 等非 `AnimatedSprite2D` 资源补专门动画系统
- 不引入完整单位战斗、移动、AI、伤害等无关组件
- 不做每个资源的显式默认动作配置表

## 核心方案

### 1. 独立场景，不再依赖 TestSystem

新增独立测试场景到 `Src/ECS/Test/GlobalTest`，它是一个单独运行入口，不依赖：

- `MainTest`
- `TestSystem`
- `TestModuleBase`
- 旧的 `AssetVisualPreviewModule`

该场景只负责三类职责：

- UI 宿主：左侧可折叠调试面板与选中详情展示
- 世界预览控制：批量生成、布局、隐藏、清理预览实体
- 选择结果消费：监听 `MouseSelectionSystem` 的全局选择结果并维护当前选中实体

### 2. 新建专用预览实体 VisualPreviewEntity

新增 `VisualPreviewEntity`，不要复用 `TargetingIndicatorEntity`。

原因：

- `TargetingIndicatorEntity` 语义属于技能瞄准指示器，与视觉验收无关
- 其现有组件和输入语义会污染预览场景职责
- 预览实体只需要最小 `IEntity` 容器，不需要瞄准控制组件

`VisualPreviewEntity` 的边界：

- 实现 `IEntity`
- 可被 `MouseSelectionSystem` 选中
- 通过 `EntityManager.Spawn/Register/Destroy` 管理生命周期
- 挂载 `UnitAnimationComponent`
- 允许注入任意视觉场景到 `VisualRoot`
- 只存本次预览需要的最小运行时数据

### 3. 资源来源统一为 ResourcePaths.Resources 中全部 Asset* 分类

预览数据只从 `ResourcePaths.Resources` 获取，不扫描目录，不额外维护另一套资源清单。

筛选规则：

- 包含全部 `ResourceCategory` 名称以 `Asset` 开头的分类
- 当前已知包括但不限于：
  - `Asset`
  - `AssetEffect`
  - `AssetUnit`
  - `AssetUnitEnemy`
  - `AssetUnitPlayer`
  - `AssetProjectile`
- 未来若 `ResourceGenerator` 新增新的 `Asset*` 分类，本场景应自动纳入

统一抽象出预览条目视图模型，至少包含：

- `ResourceKey`
- `Category`
- `ResourcePath`
- `DisplayName`
- `SceneName`
- `CatalogPath`

其中：

- `SceneName` 取 `ResourcePath` 最后一段场景名
- `DisplayName` 与 `DataKey.Name` 统一使用 `SceneName`

### 4. DataKey.Name 与详情信息语义

每个预览实体生成后，都要写入运行时数据：

- `DataKey.Name` = 视觉场景名称，即资源路径最后一段场景名
- 资源键
- 资源路径
- 资源分类
- 默认动作
- 当前动作

这些数据用于：

- 鼠标选中后左侧详情面板展示
- 统一显示名称，不再依赖原始节点名
- 后续若需要扩展调试信息，也不必反查控制器内部缓存

`DataKey.Name` 这次不是保留原配置名，而是明确改成视觉场景名，这是预览工具的显示语义。

### 5. 动画统一复用 UnitAnimationComponent

既然预览对象已经是 `Entity`，就不再维护自定义动画控制器，统一复用 `UnitAnimationComponent`。

这样可以直接获得：

- `VisualRoot` 下 `AnimatedSprite2D` 的查找
- 可用动作缓存到 `DataKey.AvailableAnimations`
- 动画存在性检查
- 动画回退逻辑

但预览场景有两个额外约束：

- 预览实体不需要移动逻辑，因此不能让 `UnitAnimationComponent` 因实体静止而持续把动画改回 `idle`
- 统一切动作时应以“显式请求播放某动作”为主，而不是依赖 `_Process` 中的移动判定

因此需要补一层明确设计：

- 预览实体默认进入静态预览模式
- 统一播放动作时，通过实体事件触发 `GameEventType.Unit.PlayAnimationRequested`
- 若当前资源没有该动作，则按默认动作规则回退；若回退动作也不存在，则暂停

### 6. 默认动作规则

默认动作不做单独配置表，先按分类约定推断：

- `AssetUnit*` 默认动作为 `idle`
- `AssetEffect` 默认动作为 `Effect`
- 其他存在 `AnimatedSprite2D` 的资源，默认动作取第一个可用动画
- `AssetProjectile` 目前不做统一动画控制，因为其主资源不是 `AnimatedSprite2D`

统一切换动作时，回退规则为：

1. 若资源存在目标动作，则播放目标动作
2. 否则回退到该资源的默认动作
3. 若默认动作也不存在，则暂停或停在当前初始状态

这里不再为每个资源额外维护显式默认动作配置。

### 7. 鼠标选择与当前选中项

独立预览场景直接监听：

- `GameEventType.Global.MouseSelectionCompleted`
- 必要时也可监听 `MouseSelectionMissed`

场景内部维护自己的 `SelectedEntity`，不再依赖 `TestSystem.SetSelectedEntity(...)`。

选择行为规则：

- 点击命中预览实体后，详情面板展示该实体信息
- 框选时默认主目标使用 `MouseSelectionSystem` 给出的 `PrimaryEntity`
- 若命中非预览实体，场景忽略该结果
- 若未命中任何目标，可保留上次选择，或按最终交互设计清空选择；默认推荐清空

### 8. 分类筛选与显示隐藏

左侧面板按分类展示全部 `Asset*` 分类。

筛选行为：

- 选中某个分类时，只显示该分类下的实体
- 未选中的分类实体不销毁，只隐藏
- 动作切换只作用于当前显示分类

这样可以避免：

- 切换分类反复销毁/重建实体
- 每次切换都重新加载场景资源
- 鼠标选择状态在分类切换时频繁丢失

同时需要保证：

- 隐藏实体时同步停止其预览动画或保持当前状态的一致性
- 当前选中实体若被分类切换隐藏，详情面板应同步处理为“隐藏中”或清空选择

### 9. 布局策略

世界中的预览实体按固定网格布局：

- 固定列数
- 固定横向间距
- 固定纵向间距
- 原点可基于场景中央或摄像机中心偏移

第一版布局参数允许写死在代码中，不放到运行时 UI 调节。

因为资源范围扩展到了全部 `Asset*`，需要注意：

- 不同视觉场景尺寸差异较大
- 同分类里可能同时存在单位、特效、投射物等不同视觉重心

因此布局计算应只负责规则排布，不尝试做自动包围盒对齐。

## 组件与文件边界

### 新增

- 独立视觉预览测试场景与入口脚本
- `VisualPreviewEntity`
- 视觉预览资源收集服务
- 视觉预览实体生成/布局控制器
- 场景侧 UI 控制脚本
- 预览实体所需的最小数据键或视图模型

### 删除或迁移

- `Src/ECS/Base/System/TestSystem/VisualPreview` 下旧模块不再作为主实现
- 旧的 `AssetVisualPreviewModule`
- 旧的 `AssetVisualPreviewController`
- 旧的 `AssetVisualPreviewService`

这些旧实现应从“TestSystem 模块逻辑”迁移为“独立测试场景逻辑”，而不是继续叠补。

### 保留并复用

- `ResourcePaths.Resources`
- `ResourceCategory`
- `ResourceCatalog` 中已有的资源目录抽象（如适合复用）
- `UnitAnimationComponent`
- `MouseSelectionSystem`
- `EntityManager`

## 关键实现约束

### 预览实体最小组件集

`VisualPreviewEntity` 只保留本次需要的最小组件：

- `UnitAnimationComponent`
- 若点选精度需要，可保留碰撞节点承载视觉模板同步

禁止把以下无关组件带进来：

- `TargetingIndicatorControlComponent`
- 攻击、移动、受击、AI、恢复、技能、碰撞伤害等业务组件

### VisualRoot 注入

预览实体应复用现有 `EntityManager.InjectVisualScene` 机制，把视觉场景作为 `VisualRoot` 注入。

这样可以顺便复用：

- `VisualRoot` 命名约定
- 视觉碰撞模板同步
- `UnitAnimationComponent` 既有查找逻辑

### Projectile 处理

`AssetProjectile` 虽然纳入列表，但当前阶段只保证：

- 能实例化展示
- 能被选中并显示资源信息

不要求：

- 支持统一动作切换
- 接入 `AnimatedSprite2D` 动画控制

如果某个 Projectile 资源未来改为 `AnimatedSprite2D`，可在后续迭代中自然纳入统一动作控制。

## UI 设计

左侧为可折叠调试面板，至少包含：

- 分类列表
- 动作列表
- 刷新按钮
- 全部显示 / 隐藏或重建入口
- 当前选中实体信息区域

当前选中信息至少显示：

- 名称
- 资源键
- 资源路径
- 资源分类
- 默认动作
- 当前动作
- 可用动作列表

UI 不需要做复杂美术设计，但需要做到：

- 面板折叠后不影响世界预览点击
- 展开时能清楚显示当前选择与筛选状态
- 空分类、无动作、加载失败时有明确文案

## 错误与空态处理

- 某资源加载失败：跳过实体生成，并在状态区记录失败项
- 某资源不是 `Node2D` / 不满足预览挂载要求：跳过并记录原因
- 某资源没有 `AnimatedSprite2D`：允许展示，但动作控制区标记为不支持
- 当前分类没有任何资源：显示空态，不报错
- 当前动作不存在于某些资源：按默认动作回退，回退失败则暂停

## 测试要点

至少需要验证：

- 独立场景运行后能收集全部 `Asset*` 分类
- 分类切换时只隐藏/显示，不反复重建全部实体
- 每个实体的 `DataKey.Name` 正确等于资源路径最后场景名
- 鼠标点击/框选后可以正确更新当前选中详情
- `AssetUnit*` 默认回退到 `idle`
- `AssetEffect` 默认回退到 `Effect`
- 其他 `AnimatedSprite2D` 资源回退到第一个动画
- `AssetProjectile` 能显示和选中，但不会错误参与 `AnimatedSprite2D` 动作控制
- 场景退出时，预览实体通过 `EntityManager.Destroy` 正确清理

## 文档与同步要求

实现完成后需要同步更新：

- `Docs/框架/项目索引.md`
- 视觉预览相关文档
- `test-system` Skill，说明视觉预览已迁出 `TestSystem`
- 若修改了资源分类解释或 `ResourceCatalog` 行为，同步更新相关 Skill / 文档

## 风险与取舍

### 风险 1：UnitAnimationComponent 的移动态回写会干扰预览

如果 `UnitAnimationComponent` 保持当前默认行为，预览实体静止时可能不断切回 `idle`。

结论：

- 需要给 `UnitAnimationComponent` 增加“预览模式”或等价运行时开关
- 预览模式下不根据 `_Process` 自动切换 idle/run，只响应显式播放请求

### 风险 2：全部 Asset* 分类混用时资源类型不一致

单位、特效、投射物的视觉结构并不完全统一。

结论：

- 用统一收集模型，但动作控制要按资源能力降级
- 预览系统本身要接受“能展示但不能播动作”的资源存在

### 风险 3：旧 TestSystem VisualPreview 代码继续残留

如果只是增量修改旧模块，后续会出现两个视觉预览入口并长期分叉。

结论：

- 主实现必须迁移到独立场景
- 旧模块要删除或明确废弃，不能继续作为等价入口保留
