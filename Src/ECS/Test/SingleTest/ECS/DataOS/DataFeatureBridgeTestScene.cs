using System.Collections.Generic;

namespace Slime.Test.DataOS;

/// <summary>
/// 验证 Feature/Modifier 通过 Data runtime 影响 computed 字段。
/// </summary>
public partial class DataFeatureBridgeTestScene : DataSceneTestBase
{
    protected override void RunTests()
    {
        FeatureModifiers_ShouldBeRepresentedAsAuthoringBlobDefinition();
        Data_DefaultResolvers_ShouldComputeAttributeBonusPercentAndAttackInterval();
        FeatureModifierChange_ShouldInvalidateComputedDependents();
        RepositoryFeatureModifier_ShouldAffectComputedField();
    }

    private void FeatureModifiers_ShouldBeRepresentedAsAuthoringBlobDefinition()
    {
        var definition = Bootstrap.Catalog.GetRequired(GeneratedDataKey.FeatureModifiers.StableKey);
        AssertEqual("feature modifiers value type", DataValueType.ModifierList, definition.ValueType);
        AssertEqual("feature modifiers storage", DataStoragePolicy.AuthoringBlob, definition.StoragePolicy);
        AssertEqual("feature modifiers write policy", DataWritePolicy.LoaderOnly, definition.WritePolicy);
        AssertEqual("feature modifiers modifier policy", DataModifierPolicy.None, definition.ModifierPolicy);
    }

    private void Data_DefaultResolvers_ShouldComputeAttributeBonusPercentAndAttackInterval()
    {
        var registry = CreateRegistry(
            new AttributeBonusComputeResolver(),
            new PercentComputeResolver(),
            new AttackIntervalComputeResolver());
        var data = CreateRuntimeDataFromDescriptors(registry, new[]
        {
            Descriptor("Attribute.BaseAttack", "float", "80"),
            Descriptor("Attribute.AttackBonus", "float", "25"),
            Descriptor("Attribute.FinalAttack", "float", "0") with
            {
                StoragePolicy = "computed",
                WritePolicy = "computed_readonly",
                ComputeId = "AttributeBonus",
                Dependencies = new List<string> { "Attribute.BaseAttack", "Attribute.AttackBonus" }
            },
            Descriptor("Attribute.CurrentHp", "float", "30"),
            Descriptor("Attribute.FinalHp", "float", "60"),
            Descriptor("Attribute.HpPercent", "float", "0") with
            {
                StoragePolicy = "computed",
                WritePolicy = "computed_readonly",
                ComputeId = "Percent",
                Dependencies = new List<string> { "Attribute.CurrentHp", "Attribute.FinalHp" }
            },
            Descriptor("Attribute.FinalAttackSpeed", "float", "200"),
            Descriptor("Attribute.AttackInterval", "float", "1") with
            {
                StoragePolicy = "computed",
                WritePolicy = "computed_readonly",
                ComputeId = "AttackInterval",
                Dependencies = new List<string> { "Attribute.FinalAttackSpeed" }
            }
        });

        AssertEqual("attribute bonus resolver", 100f, data.Get<float>("Attribute.FinalAttack"));
        AssertEqual("percent resolver", 50f, data.Get<float>("Attribute.HpPercent"));
        AssertEqual("attack interval resolver", 0.5f, data.Get<float>("Attribute.AttackInterval"));
    }

    private void FeatureModifierChange_ShouldInvalidateComputedDependents()
    {
        var data = new Data(Bootstrap.Catalog);
        data.Set(GeneratedDataKey.BaseAttack, 80f);
        data.Set(GeneratedDataKey.AttackBonus, 25f);
        AssertEqual("computed before modifier", 100f, data.Get<float>(GeneratedDataKey.FinalAttack));

        AssertTrue("modifier accepted", data.TryAddModifier(GeneratedDataKey.BaseAttack, new DataModifier(ModifierType.Additive, 20f, id: "scene_bonus")));
        AssertEqual("modifier dirties computed value", 125f, data.Get<float>(GeneratedDataKey.FinalAttack));
    }

    private void RepositoryFeatureModifier_ShouldAffectComputedField()
    {
        var data = new Data(Bootstrap.Catalog);
        data.Set(GeneratedDataKey.BaseAttack, 80f);
        data.Set(GeneratedDataKey.AttackBonus, 25f);
        AssertEqual("computed attack bonus", 100f, data.Get<float>(GeneratedDataKey.FinalAttack));

        var source = new object();
        AssertTrue("feature grants modifier", data.TryAddModifier(GeneratedDataKey.BaseAttack, new DataModifier(ModifierType.Additive, 20f, id: "feature_bonus", source: source)));
        AssertEqual("feature modifier changes computed", 125f, data.Get<float>(GeneratedDataKey.FinalAttack));
        data.RemoveModifiersBySource(source);
        AssertEqual("feature removal rolls back computed", 100f, data.Get<float>(GeneratedDataKey.FinalAttack));
    }
}
