using Godot;

/// <summary>
/// Unit 运动相关事件定义。
/// </summary>
public static partial class GameEventType
{
    public static partial class Unit
    {
        /// <summary>运动开始或切换事件。</summary>
        public readonly record struct MovementStarted(global::MoveMode Mode, global::MovementParams Params);

        /// <summary>运动完成事件。</summary>
        public readonly record struct MovementCompleted(
            global::MoveMode Mode,
            float ElapsedTime,
            float TraveledDistance,
            global::MovementStopReason Reason,
            Node2D? CollisionTarget = null);

        /// <summary>运动中有效碰撞事件。</summary>
        public readonly record struct MovementCollision(
            global::MoveMode Mode,
            Node2D? Target,
            IEntity? TargetEntity = null,
            int CollisionCount = 0,
            bool WillStop = false);

        /// <summary>移动停止请求事件。</summary>
        public readonly record struct MovementStopRequested
        {
            /// <summary>
            /// 带默认值的 struct 需要显式无参构造函数。
            /// </summary>
            public MovementStopRequested()
            {
            }

            /// <summary>停止原因。</summary>
            public global::MovementStopReason Reason { get; init; } = global::MovementStopReason.Requested;

            /// <summary>是否发出 MovementCompleted 事件。</summary>
            public bool EmitCompletedEvent { get; init; } = true;

            /// <summary>
            /// 停止后切换的下一模式。
            /// <para><c>MoveMode.None</c> 表示沿用默认回退逻辑。</para>
            /// </summary>
            public global::MoveMode NextMode { get; init; } = global::MoveMode.None;

            /// <summary>若由碰撞引发停止，则为碰撞目标。</summary>
            public Node2D? CollisionTarget { get; init; } = null;

            /// <summary>是否在停止后直接销毁实体。</summary>
            public bool DestroyEntity { get; init; } = false;
        }

        /// <summary>朝向控制启动事件。</summary>
        public readonly record struct OrientationStarted
        {
            /// <summary>
            /// 带默认值的 struct 需要显式无参构造函数。
            /// </summary>
            public OrientationStarted()
            {
            }

            /// <summary>当前朝向配置来源。</summary>
            public global::OrientationSource Source { get; init; } = global::OrientationSource.Standalone;

            /// <summary>朝向控制参数。</summary>
            public global::OrientationParams Params { get; init; } = new global::OrientationParams();

            /// <summary>是否随 Movement 生命周期自动停止。</summary>
            public bool StopWithMovement { get; init; } = false;
        }

        /// <summary>朝向控制停止事件。</summary>
        public readonly record struct OrientationStopped
        {
            /// <summary>
            /// 带默认值的 struct 需要显式无参构造函数。
            /// </summary>
            public OrientationStopped()
            {
            }

            /// <summary>当前停止请求的来源。</summary>
            public global::OrientationSource Source { get; init; } = global::OrientationSource.Standalone;

            /// <summary>停止原因。</summary>
            public global::MovementStopReason Reason { get; init; } = global::MovementStopReason.Requested;
        }
    }
}
