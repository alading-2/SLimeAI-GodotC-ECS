/// <summary>
/// Feature 生命周期实体事件。
/// </summary>
public static class FeatureEvents
{
    public readonly record struct Granted(IEntity Feature, IEntity Owner) : IEntityEvent;

    public readonly record struct Enabled(IEntity Feature, IEntity Owner) : IEntityEvent;

    public readonly record struct Disabled(IEntity Feature, IEntity Owner) : IEntityEvent;

    public readonly record struct Activated(FeatureContext Context) : IEntityEvent;

    public readonly record struct Executed(FeatureContext Context) : IEntityEvent;

    public readonly record struct Ended(FeatureContext Context, FeatureEndReason Reason) : IEntityEvent;

    public readonly record struct Removed(string FeatureName, IEntity Owner) : IEntityEvent;
}
