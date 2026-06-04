using Godot;

/// <summary>
/// 玩家实体类（Scene 即 Entity）。
/// <para>
/// 职责：输入处理、升级系统、技能管理。
/// 架构：单例常驻，与 Enemy 逻辑分离，通过组件（Component）复用共享行为。
/// </para>
/// </summary>
public partial class PlayerEntity : CharacterBody2D, IUnit, IComponentCompositionProvider
{
	private static readonly Log _log = new(nameof(PlayerEntity));

	// ================= IEntity 实现 =================

	/// <summary>
	/// 动态数据容器
	/// </summary>
	public Data Data { get; private set; }

	public PlayerEntity()
	{
		Data = new Data(this);
	}

	/// <summary>
	/// 实体局部事件总线
	/// </summary>
	public EventBus Events { get; } = new EventBus();

	public override void _Ready()
	{
		base._Ready();
	}

	public override void _ExitTree()
	{
		base._ExitTree();
	}

	// PlayerEntity 不使用对象池；DefaultMoveMode 必须由 unit.player runtime snapshot record 提前写入。

	public ComponentCompositionProfile GetComponentCompositionProfile()
	{
		return UnitComponentCompositionProfiles.Player();
	}
}
