/// <summary>
/// Timer owner 的粗粒度分类，后续可替换为更强 typed id。
/// </summary>
public enum TimerOwnerType
{
    None,
    Entity,
    Component,
    System,
    Ability,
    Feature,
    Tool,
    Test
}
