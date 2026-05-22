using Godot;
using slime.data;
using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// Runtime Data typed contract 验证场景。
/// </summary>
public partial class RuntimeDataValidationScene : Node
{
    private int _total;
    private int _failed;

    public override void _Ready()
    {
        RunAll();
        var passed = _failed == 0;
        SceneTestArtifact.Write(
            "RuntimeDataValidationScene",
            passed,
            _total - _failed,
            _failed,
            [
                "typed DataKey set/get/remove operations",
                "inline typed runtime snapshot JSON samples"
            ],
            [
                "typed DataKey access preserves values and categories",
                "numeric modifiers are accepted and string modifiers are rejected",
                "computed DataKey cache invalidates on dependency changes",
                "typed snapshot apply reports expected structured errors"
            ],
            [
                "all in-scene assertions pass",
                "Godot process exits with code 0",
                "scene-artifact.json status is pass"
            ],
            [
                "any Runtime Data assertion fails",
                "Godot process exits non-zero",
                "scene-artifact.json is missing or status is fail"
            ],
            [
                "typed-data-key-access",
                "modifier-constraints",
                "computed-cache",
                "snapshot-apply-validation"
            ]);

        if (passed)
        {
            GD.Print($"[PASS] RuntimeDataValidationScene total={_total}");
            GetTree().Quit(0);
            return;
        }

        GD.PrintErr($"[FAIL] RuntimeDataValidationScene failed={_failed} total={_total}");
        GetTree().Quit(1);
    }

    private void RunAll()
    {
        TestTypedSetGetTryHasRemove();
        TestModifierConstraints();
        TestComputedDirtyCache();
        TestCategoryReset();
        TestTypedSnapshotApply();
        TestSnapshotInvalidCases();
    }

    private void TestTypedSetGetTryHasRemove()
    {
        var data = new Data();
        data.Set(DataKey.BaseHp, 100f);

        AssertEqual("typed.get", data.Get(DataKey.BaseHp), 100f);
        AssertTrue("typed.tryget", data.TryGet(DataKey.BaseHp, out var hp) && Math.Abs(hp - 100f) < 0.001f);
        AssertTrue("typed.has", data.Has(DataKey.BaseHp));
        AssertTrue("typed.remove", data.Remove(DataKey.BaseHp));
        AssertFalse("typed.has.after.remove", data.Has(DataKey.BaseHp));
    }

    private void TestModifierConstraints()
    {
        var data = new Data();
        data.Set(DataKey.BaseHp, 100f);
        AssertTrue("modifier.numeric.allowed", data.AddModifier(DataKey.BaseHp, new DataModifier(ModifierType.Additive, 25f, id: "hp-bonus")));
        AssertEqual("modifier.numeric.value", data.Get(DataKey.BaseHp), 125f);

        data.Set(DataKey.Name, "hero");
        AssertFalse("modifier.string.rejected", data.AddModifier(DataKey.Name, new DataModifier(ModifierType.Additive, 1f, id: "bad")));
        AssertEqual("modifier.string.unchanged", data.Get(DataKey.Name), "hero");
    }

    private void TestComputedDirtyCache()
    {
        var data = new Data();
        data.Set(DataKey.BaseHp, 100f);
        data.Set(DataKey.HpBonus, 25f);
        AssertEqual("computed.initial", data.Get(DataKey.FinalHp), 125f);

        data.Set(DataKey.HpBonus, 50f);
        AssertEqual("computed.dirty.after.dependency", data.Get(DataKey.FinalHp), 150f);
    }

    private void TestCategoryReset()
    {
        var data = new Data();
        data.Set(DataKey.BaseHp, 777f);
        data.Set(DataKey.Name, "keep");

        data.ResetByCategory(DataCategory_Attribute.Health);
        AssertEqual("category.reset.health", data.Get(DataKey.BaseHp), DataKey.BaseHp.DefaultValue);
        AssertEqual("category.reset.other.category.kept", data.Get(DataKey.Name), "keep");
    }

    private void TestTypedSnapshotApply()
    {
        var document = RuntimeDataSnapshot.ParseTypedSnapshot(BuildValidSnapshot());
        var data = new Data();
        var report = RuntimeDataSnapshot.ApplyRecordToData(document, "unit.player", "player.validation", data);

        AssertTrue("snapshot.apply.success", report.Success);
        AssertEqual("snapshot.apply.name", data.Get(DataKey.Name), "验证玩家");
        AssertEqual("snapshot.apply.team", data.Get(DataKey.Team), Team.Player);
        AssertEqual("snapshot.apply.hp", data.Get(DataKey.BaseHp), 120f);
    }

    private void TestSnapshotInvalidCases()
    {
        AssertSnapshotError("snapshot.unknown_key", BuildSnapshotWithField("Unknown.Key", "float", 1f));
        AssertSnapshotError("snapshot.missing_descriptor", BuildSnapshotWithMissingDescriptor());
        AssertSnapshotError("snapshot.wrong_type", BuildSnapshotWithField(DataKey.BaseHp.Key, "float", "bad"));
        AssertSnapshotError("snapshot.default_drift", BuildSnapshotWithDefaultDrift());
        AssertSnapshotError("snapshot.resource_disabled_capability", BuildSnapshotWithDisabledResource());
    }

    private void AssertSnapshotError(string expectedCode, string json)
    {
        var document = RuntimeDataSnapshot.ParseTypedSnapshot(json);
        var report = RuntimeDataSnapshot.ApplyRecordToData(document, "unit.player", "player.validation", new Data());
        AssertFalse(expectedCode, report.Success);
        AssertTrue(expectedCode + ".reported", report.Errors.Exists(error => error.Code == expectedCode));
    }

    private static string BuildValidSnapshot()
    {
        return """
        {
          "schemaVersion": 1,
          "generatedAtUtc": "2026-05-22T00:00:00Z",
          "manifest": { "profile": "slimeainew", "catalogId": "slimeainew", "enabledCapabilities": ["unit"] },
          "descriptors": [
            { "key": "Name", "type": "string", "defaultValue": "" },
            { "key": "Team", "type": "enum", "defaultValue": "Neutral" },
            { "key": "BaseHp", "type": "float", "defaultValue": 10 }
          ],
          "records": [
            {
              "table": "unit.player",
              "id": "player.validation",
              "name": "验证玩家",
              "fields": {
                "Name": { "type": "string", "value": "验证玩家" },
                "Team": { "type": "enum", "value": "Player" },
                "BaseHp": { "type": "float", "value": 120 }
              }
            }
          ],
          "resources": []
        }
        """;
    }

    private static string BuildSnapshotWithField(string key, string type, object? value)
    {
        using var doc = JsonDocument.Parse(BuildValidSnapshot());
        var defaultValue = key == DataKey.BaseHp.Key
            ? "10"
            : "0";
        var valueLiteral = value is string text ? $"\"{text}\"" : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        return $$"""
        {
          "schemaVersion": 1,
          "generatedAtUtc": "2026-05-22T00:00:00Z",
          "manifest": { "profile": "slimeainew", "catalogId": "slimeainew", "enabledCapabilities": ["unit"] },
          "descriptors": [
            { "key": "{{key}}", "type": "{{type}}", "defaultValue": {{defaultValue}} }
          ],
          "records": [
            { "table": "unit.player", "id": "player.validation", "name": "验证玩家", "fields": { "{{key}}": { "type": "{{type}}", "value": {{valueLiteral}} } } }
          ],
          "resources": []
        }
        """;
    }

    private static string BuildSnapshotWithMissingDescriptor()
    {
        return """
        {
          "schemaVersion": 1,
          "generatedAtUtc": "2026-05-22T00:00:00Z",
          "manifest": { "profile": "slimeainew", "catalogId": "slimeainew", "enabledCapabilities": ["unit"] },
          "descriptors": [],
          "records": [
            { "table": "unit.player", "id": "player.validation", "name": "验证玩家", "fields": { "BaseHp": { "type": "float", "value": 120 } } }
          ],
          "resources": []
        }
        """;
    }

    private static string BuildSnapshotWithDefaultDrift()
    {
        return """
        {
          "schemaVersion": 1,
          "generatedAtUtc": "2026-05-22T00:00:00Z",
          "manifest": { "profile": "slimeainew", "catalogId": "slimeainew", "enabledCapabilities": ["unit"] },
          "descriptors": [
            { "key": "BaseHp", "type": "float", "defaultValue": 999 }
          ],
          "records": [
            { "table": "unit.player", "id": "player.validation", "name": "验证玩家", "fields": { "BaseHp": { "type": "float", "value": 120 } } }
          ],
          "resources": []
        }
        """;
    }

    private static string BuildSnapshotWithDisabledResource()
    {
        return """
        {
          "schemaVersion": 1,
          "generatedAtUtc": "2026-05-22T00:00:00Z",
          "manifest": { "profile": "slimeainew", "catalogId": "slimeainew", "enabledCapabilities": [] },
          "descriptors": [
            { "key": "Name", "type": "string", "defaultValue": "" }
          ],
          "records": [
            { "table": "unit.player", "id": "player.validation", "name": "验证玩家", "fields": { "Name": { "type": "string", "value": "验证玩家" } } }
          ],
          "resources": [
            { "category": "entity", "key": "Validation", "path": "res://Validation.tscn", "ownerCapability": "unit", "legacyStatus": "active" }
          ]
        }
        """;
    }

    private void AssertEqual<T>(string name, T actual, T expected)
    {
        _total++;
        if (EqualityComparer<T>.Default.Equals(actual, expected))
        {
            GD.Print($"[PASS] {name}");
            return;
        }

        _failed++;
        GD.PrintErr($"[FAIL] {name}: expected={expected} actual={actual}");
    }

    private void AssertTrue(string name, bool condition)
    {
        AssertEqual(name, condition, true);
    }

    private void AssertFalse(string name, bool condition)
    {
        AssertEqual(name, condition, false);
    }

}
