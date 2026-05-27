public static partial class GameEventType
{
    /// <summary>
    /// 测试专用事件类型，用于运行时测试中的事件订阅/触发验证。
    /// </summary>
    public static class Test
    {
        /// <summary>迁移测试事件，用于验证事件订阅不会被复制到迁移后的实体</summary>
        public readonly record struct MigrationTestEvent();
    }
}
