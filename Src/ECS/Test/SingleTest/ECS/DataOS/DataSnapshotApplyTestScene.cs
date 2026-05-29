using System;
using System.Collections.Generic;

namespace Slime.Test.DataOS;

/// <summary>
/// 验证 runtime snapshot record apply。
/// </summary>
public partial class DataSnapshotApplyTestScene : DataSceneTestBase
{
    protected override void RunTests()
    {
        RuntimeSnapshot_LoadFromJson_ShouldDeserializeDescriptorsAndRecords();
        RuntimeSnapshot_ApplyRecord_ShouldWriteTypedFields();
        RuntimeSnapshot_ApplyRecord_ShouldAggregateSnapshotErrors();
        DataApplyReport_ShouldSummarizeStructuredErrors();
        DataRuntimeBootstrap_ShouldBuildCatalogFindRecordAndCreateData();
        DataRuntimeBootstrap_ShouldBindExistingDataAndApplyRecord();
        RepositorySnapshot_RecordApply_ShouldUseGeneratedHandles();
        RuntimeDataRecordQuery_ShouldQueryByTableIdAndName();
        RuntimeDataRecordProjection_ShouldBuildTypedViews();
        RuntimeDataRecordProjection_ShouldReportMissingFields();
    }

    private void RuntimeSnapshot_LoadFromJson_ShouldDeserializeDescriptorsAndRecords()
    {
        const string json = """
        {
          "schemaVersion": 2,
          "descriptors": [
            { "stableKey": "Attribute.BaseHp", "valueType": "float", "defaultValue": "10", "storagePolicy": "persisted", "writePolicy": "read_write", "rangePolicy": "none", "modifierPolicy": "none", "migrationPolicy": "default" }
          ],
          "records": [
            {
              "table": "unit",
              "id": "unit.slime",
              "name": "Slime",
              "fields": {
                "Attribute.BaseHp": { "type": "float", "value": "25" }
              }
            }
          ]
        }
        """;

        var snapshot = CreateLoader().LoadFromJson(json);
        AssertEqual("snapshot descriptor count", 1, snapshot.Descriptors.Count);
        AssertEqual("snapshot record count", 1, snapshot.Records.Count);
        AssertEqual("snapshot record lookup", "unit.slime", snapshot.FindRecord("unit", "unit.slime").Id);
    }

    private void RuntimeSnapshot_ApplyRecord_ShouldWriteTypedFields()
    {
        var loader = CreateLoader();
        var catalog = loader.BuildCatalog(new[]
        {
            Descriptor("Attribute.BaseHp", "float", "10"),
            Descriptor("Unit.Team", "enum", "Neutral") with
            {
                WritePolicy = "loader_only",
                AllowedValues = new List<DataAllowedValueDto>
                {
                    new() { Value = "Player", Label = "Player" },
                    new() { Value = "Enemy", Label = "Enemy" },
                    new() { Value = "Neutral", Label = "Neutral" }
                }
            }
        });
        var data = new Data(catalog);
        var record = new RuntimeDataRecordDto
        {
            Table = "unit",
            Id = "unit.enemy",
            Fields = new Dictionary<string, RuntimeDataFieldDto>
            {
                ["Attribute.BaseHp"] = new() { Type = "float", Value = "25.5" },
                ["Unit.Team"] = new() { Type = "enum", Value = "Enemy" }
            }
        };

        var report = loader.ApplyRecord(data, catalog, record);

        AssertFalse("apply report has no errors", report.HasErrors);
        AssertEqual("applied field count", 2, report.AppliedFieldCount);
        AssertEqual("applied float value", 25.5f, data.Get<float>("Attribute.BaseHp"));
        AssertEqual("applied enum value", "Enemy", data.Get<string>("Unit.Team"));
    }

    private void RuntimeSnapshot_ApplyRecord_ShouldAggregateSnapshotErrors()
    {
        var loader = CreateLoader(new FixedComputeResolver("Computed"));
        var catalog = loader.BuildCatalog(new[]
        {
            Descriptor("Attribute.BaseHp", "float", "10"),
            Descriptor("Attribute.Convert", "float", "10"),
            Descriptor("Attribute.RuntimeOnly", "float", "0") with { StoragePolicy = "runtime_only" },
            Descriptor("Attribute.Computed", "float", "0") with
            {
                StoragePolicy = "computed",
                WritePolicy = "computed_readonly",
                ComputeId = "Computed",
                Dependencies = new List<string> { "Attribute.BaseHp" }
            }
        });
        var data = new Data(catalog);
        var record = new RuntimeDataRecordDto
        {
            Table = "unit",
            Id = "unit.invalid",
            Fields = new Dictionary<string, RuntimeDataFieldDto>
            {
                ["Attribute.Unknown"] = new() { Type = "float", Value = "1" },
                ["Attribute.BaseHp"] = new() { Type = "int", Value = "1" },
                ["Attribute.Convert"] = new() { Type = "float", Value = "not-float" },
                ["Attribute.RuntimeOnly"] = new() { Type = "float", Value = "1" },
                ["Attribute.Computed"] = new() { Type = "float", Value = "1" }
            }
        };

        var report = loader.ApplyRecord(data, catalog, record);

        AssertTrue("apply report has errors", report.HasErrors);
        AssertEqual("apply error count", 5, report.Errors.Count);
        AssertEqual("unknown key error", "snapshot.unknown_key", report.Errors[0].Code);
        AssertEqual("type mismatch error", "snapshot.type_mismatch", report.Errors[1].Code);
        AssertEqual("conversion error", "snapshot.conversion_failed", report.Errors[2].Code);
        AssertEqual("runtime only rejected", "snapshot.apply_rejected", report.Errors[3].Code);
        AssertEqual("computed rejected", "snapshot.apply_rejected", report.Errors[4].Code);
        AssertEqual("applied field count after errors", 0, report.AppliedFieldCount);
        AssertEqual("invalid field did not overwrite default", 10f, data.Get<float>("Attribute.BaseHp"));
    }

    private void DataApplyReport_ShouldSummarizeStructuredErrors()
    {
        var report = new DataApplyReport("unit", "unit.invalid");
        report.AddError("snapshot.unknown_key", "Attribute.Unknown", "record field 没有对应 descriptor。");

        AssertTrue("report has errors", report.HasErrors);
        AssertEqual("report table", "unit", report.Table);
        AssertEqual("report record id", "unit.invalid", report.RecordId);
        AssertEqual("report error key", "Attribute.Unknown", report.Errors[0].StableKey);
        AssertContains("report summary code", report.ToSummary(), "snapshot.unknown_key");
        AssertContains("report summary key", report.ToSummary(), "Attribute.Unknown");
    }

    private void DataRuntimeBootstrap_ShouldBuildCatalogFindRecordAndCreateData()
    {
        const string json = """
        {
          "schemaVersion": 2,
          "descriptors": [
            { "stableKey": "Attribute.BaseHp", "valueType": "float", "defaultValue": "10", "storagePolicy": "persisted", "writePolicy": "read_write", "rangePolicy": "none", "modifierPolicy": "none", "migrationPolicy": "default" }
          ],
          "records": [
            {
              "table": "unit",
              "id": "unit.enemy",
              "name": "Enemy",
              "fields": {
                "Attribute.BaseHp": { "type": "float", "value": "35" }
              }
            }
          ]
        }
        """;
        var bootstrap = new DataRuntimeBootstrap(CreateRegistry());
        bootstrap.Initialize(json);
        var record = bootstrap.FindRecord("unit", "unit.enemy");
        var data = bootstrap.CreateData(record);

        AssertEqual("bootstrap catalog count", 1, bootstrap.Catalog.Count);
        AssertEqual("bootstrap record id", "unit.enemy", record.Id);
        AssertEqual("bootstrap data value", 35f, data.Get<float>("Attribute.BaseHp"));
    }

    private void DataRuntimeBootstrap_ShouldBindExistingDataAndApplyRecord()
    {
        const string json = """
        {
          "schemaVersion": 2,
          "descriptors": [
            { "stableKey": "Attribute.BaseHp", "valueType": "float", "defaultValue": "10", "storagePolicy": "persisted", "writePolicy": "read_write", "rangePolicy": "none", "modifierPolicy": "none", "migrationPolicy": "default" }
          ],
          "records": [
            {
              "table": "unit",
              "id": "unit.enemy",
              "name": "Enemy",
              "fields": {
                "Attribute.BaseHp": { "type": "float", "value": "45" }
              }
            }
          ]
        }
        """;
        var bootstrap = new DataRuntimeBootstrap(CreateRegistry());
        bootstrap.Initialize(json);
        var data = new Data();
        var report = bootstrap.ApplyToData(data, bootstrap.FindRecord("unit", "unit.enemy"));

        AssertFalse("bind existing data report has no errors", report.HasErrors);
        AssertEqual("bound existing data value", 45f, data.Get<float>("Attribute.BaseHp"));
    }

    private void RepositorySnapshot_RecordApply_ShouldUseGeneratedHandles()
    {
        var enemy = Bootstrap.FindRecord("unit.enemy", "enemy.yuren");
        var data = Bootstrap.CreateData(enemy);
        AssertEqual("snapshot name", "鱼人", data.Get<string>(GeneratedDataKey.Name));
        AssertEqual("snapshot team", Team.Enemy, data.Get<Team>(GeneratedDataKey.Team));
        AssertEqual("snapshot exp reward", 2, data.Get<int>(GeneratedDataKey.ExpReward));
    }

    private void RuntimeDataRecordQuery_ShouldQueryByTableIdAndName()
    {
        var query = new RuntimeDataRecordQuery(Bootstrap.Snapshot);

        AssertEqual("query enemy count", 2, query.GetRecords("unit.enemy").Count);
        AssertEqual("query by id", "enemy.yuren", query.GetRequired("unit.enemy", "enemy.yuren").Id);
        AssertEqual("query by name", "enemy.yuren", query.GetRequiredByName("unit.enemy", "鱼人").Id);
        AssertThrowsMessage<KeyNotFoundException>(
            "query missing fails fast",
            () => query.GetRequired("unit.enemy", "enemy.missing"),
            "unit.enemy/enemy.missing");
    }

    private void RuntimeDataRecordProjection_ShouldBuildTypedViews()
    {
        var query = new RuntimeDataRecordQuery(Bootstrap.Snapshot);
        var enemy = RuntimeDataRecordProjection.ToUnitSpawnDefinition(query.GetRequired("unit.enemy", "enemy.yuren"));
        var ability = RuntimeDataRecordProjection.ToAbilityDefinitionView(query.GetRequired("ability", "ability.slam"));
        var system = RuntimeDataRecordProjection.ToSystemConfigDefinition(query.GetRequired("system.config", "system.spawn"));
        var preset = RuntimeDataRecordProjection.ToSystemPresetDefinition(query.GetRequired("system.preset", "system.preset.default"));

        AssertEqual("enemy record id", "enemy.yuren", enemy.RecordId);
        AssertEqual("enemy spawn interval", 2.0f, enemy.SpawnInterval);
        AssertEqual("enemy strategy", SpawnPositionStrategy.Rectangle, enemy.SpawnStrategy);
        AssertEqual("ability name", "猛击", ability.Name);
        AssertEqual("ability trigger", AbilityTriggerMode.Manual, ability.TriggerMode);
        AssertEqual("system group", SystemGroup.Gameplay, system.MountGroup);
        AssertEqual("system flow state", GameFlowState.SessionPlaying, system.AllowedFlowStates);
        AssertTrue("preset enabled tags", (preset.EnabledTags & SystemTag.Gameplay) != 0);
    }

    private void RuntimeDataRecordProjection_ShouldReportMissingFields()
    {
        var record = new RuntimeDataRecordDto
        {
            Table = "unit.enemy",
            Id = "enemy.invalid",
            Name = "Invalid",
            Fields = new Dictionary<string, RuntimeDataFieldDto>()
        };

        AssertThrowsMessage<InvalidOperationException>(
            "projection missing field",
            () => RuntimeDataRecordProjection.ToUnitSpawnDefinition(record),
            "unit.enemy/enemy.invalid");
    }

}
