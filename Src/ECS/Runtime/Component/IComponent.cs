using Godot;
using System.Linq;

/// <summary>
/// Component 标记接口
/// 所有 Component 应实现此接口，以便 EntityManager 自动识别和注册
///
/// Component 是 SlimeAI 自定义生命周期节点，不使用 Godot _EnterTree/_Ready 作为注册入口。
/// 初始化、事件订阅、Entity/Data 缓存都必须放在 OnComponentRegistered。
/// 需要固定结构参数时，由代码化 composition 在注册前注入，不使用 Inspector 导出参数作为默认配置来源。
/// 
/// 标准实现模式：
/// <code>
/// public partial class MyComponent : Node, IComponent
/// {
///     private Data? _data;
///     private IEntity? _entity;
///     
///     public void OnComponentRegistered(Node entity)
///     {
///         if (entity is IEntity iEntity)
///         {
///             _data = iEntity.Data;
///             _entity = iEntity;
///         }
///     }
///     
///     public void OnComponentUnregistered()
///     {
///         _data = null;
///         _entity = null;
///     }
/// }
/// </code>
/// </summary>
public interface IComponent
{
    /// <summary>
    /// Component 注册到 Entity 时的回调
    /// 在此方法中缓存 Entity 引用和 Data 容器,并订阅事件
    /// 
    /// 数据访问时序说明:
    ///     - 此时可以访问 EntitySpawnPipeline 已经应用的 runtime snapshot 数据。
    ///     - 运行期后续覆盖或临时目标仍可能在注册后写入。
    ///     - 如需响应后续数据设置,业务组件应在此方法中监听 GameEventType.Data.Changed&lt;T&gt; 事件。
    ///
    /// 参数注入说明:
    ///     - 组件结构参数应在注册前由代码化 composer 调用 Configure/构造方法注入。
    ///     - OnComponentRegistered 不额外扩签名，避免所有组件被参数协议污染。
    /// 
    /// 注意：此时 Entity-Component 关系已由 EntityManager 自动建立
    /// </summary>
    /// <param name="entity">所属的 Entity 节点</param>
    void OnComponentRegistered(Node entity);

    /// <summary>
    /// Component 从 Entity 注销时的回调
    /// 可用于清理资源等
    /// </summary>
    void OnComponentUnregistered();
}
