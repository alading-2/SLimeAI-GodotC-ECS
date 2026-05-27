namespace slime.data.Test
{
    /// <summary>
    /// 视觉预览实体运行时配置（纯 POCO，不继承 Resource）。
    /// <para>由预览场景代码临时构造，不需要落成 .tres。</para>
    /// <para>旧 slime.config.Test.VisualPreviewEntityConfig (Resource) 已随 DataOld 排除编译。</para>
    /// </summary>
    public class VisualPreviewEntityConfigData
    {
        /// <summary>实体名称</summary>
        [DataKey(nameof(DataKey.Name))]
        public string Name { get; set; } = (string)DataKey.Name.DefaultValue!;

        /// <summary>阵营</summary>
        [DataKey(nameof(DataKey.Team))]
        public Team Team { get; set; } = Team.Neutral;

        /// <summary>实体类型</summary>
        [DataKey(nameof(DataKey.EntityType))]
        public EntityType EntityType { get; set; } = EntityType.Unit;

        /// <summary>预览默认动作名</summary>
        [DataKey(nameof(DataKey.PreviewDefaultAnimation))]
        public string PreviewDefaultAnimation { get; set; } = (string)DataKey.PreviewDefaultAnimation.DefaultValue!;

        /// <summary>视觉场景路径 (res://)</summary>
        [DataKey(nameof(DataKey.VisualScenePath))]
        public string VisualScenePath { get; set; } = (string)DataKey.VisualScenePath.DefaultValue!;
    }
}
