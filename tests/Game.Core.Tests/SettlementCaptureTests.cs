using Game.Core.Domain;
using Game.Core.Diagnostics;

namespace Game.Core.Tests;

public class SettlementCaptureTests
{
    private static Settlement CreateSettlement(IGameLogger? logger = null)
    {
        return new Settlement(
            "test.settlement",
            "test.settlement.name",
            "test.faction.a",
            "test.faction.a",
            maxLevel: 3,
            level: 2,
            buildingSlotCount: 2,
            garrison: new Army(capacity: 50),
            logger: logger);
    }

    [Fact]
    public void Constructor_ValidatesLevelsAndSlots()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Settlement("test.settlement", "test.settlement.name", "test.faction", "test.faction", 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Settlement("test.settlement", "test.settlement.name", "test.faction", "test.faction", 5, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Settlement("test.settlement", "test.settlement.name", "test.faction", "test.faction", 3, 4, 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Settlement("test.settlement", "test.settlement.name", "test.faction", "test.faction", 3, 1, -1));
    }

    [Fact]
    public void Constructor_DefaultsToEmptyGarrisonWithoutHero()
    {
        Settlement settlement = new("test.settlement", "test.settlement.name", "test.faction", "test.faction", 3, 1, 1);

        Assert.Equal(Army.HeroLessCapacity, settlement.Garrison.Capacity);
        Assert.Empty(settlement.Garrison.Slots);
    }

    [Fact]
    public void TryAddBuilding_EnforcesSlotsAndUniqueness()
    {
        Settlement settlement = CreateSettlement();

        Assert.True(settlement.TryAddBuilding("test.building.a"));
        Assert.False(settlement.TryAddBuilding("test.building.a"));
        Assert.True(settlement.TryAddBuilding("test.building.b"));
        Assert.False(settlement.TryAddBuilding("test.building.c"));
        Assert.Equal(0, settlement.FreeBuildingSlots);
    }

    [Fact]
    public void TryAddBuilding_LogsSuccessfulConstruction()
    {
        TestLogger logger = new();
        Settlement settlement = CreateSettlement(logger);

        settlement.TryAddBuilding("test.building.a");

        LogEvent entry = Assert.Single(logger.Events);
        Assert.Equal("Economy", entry.Category);
        Assert.Contains("test.building.a", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TryAddBuilding_RejectsBlankIdentifier()
    {
        Settlement settlement = CreateSettlement();

        Assert.Throws<ArgumentException>(() => settlement.TryAddBuilding(" "));
    }

    [Fact]
    public void RemoveBuilding_RemovesOnlyExistingBuilding()
    {
        Settlement settlement = CreateSettlement();
        settlement.TryAddBuilding("test.building.a");

        Assert.True(settlement.RemoveBuilding("test.building.a"));
        Assert.False(settlement.RemoveBuilding("test.building.a"));
        Assert.Empty(settlement.BuildingIds);
    }

    [Fact]
    public void Capture_ResetsLevelRemovesBuildingsAndClearsGarrison()
    {
        TestLogger logger = new();
        Settlement settlement = CreateSettlement(logger);
        settlement.TryAddBuilding("test.building.a");
        settlement.TryAddBuilding("test.building.b");
        settlement.Garrison.TryAdd(new Stack(TestContent.CreateUnit(), 3));

        settlement.Capture("test.faction.b", newMaxLevel: 4);

        Assert.Equal("test.faction.b", settlement.OwnerFactionId);
        Assert.Equal("test.faction.b", settlement.TemplateFactionId);
        Assert.Equal(4, settlement.MaxLevel);
        Assert.Equal(Settlement.MinLevel, settlement.Level);
        Assert.Empty(settlement.BuildingIds);
        Assert.Equal(2, settlement.FreeBuildingSlots);
        Assert.Empty(settlement.Garrison.Slots);

        LogEvent entry = Assert.Single(logger.Events, e => e.Message.Contains("захвачено", StringComparison.Ordinal));
        Assert.Equal("Economy", entry.Category);
        Assert.Equal("test.faction.a", entry.Fields!["previousOwner"]);
        Assert.Equal("test.faction.b", entry.Fields["newOwner"]);
        Assert.Equal("2", entry.Fields["removedBuildings"]);
    }

    [Fact]
    public void Capture_ValidatesArgumentsAndKeepsStateOnFailure()
    {
        Settlement settlement = CreateSettlement();
        settlement.TryAddBuilding("test.building.a");

        Assert.Throws<ArgumentException>(() => settlement.Capture(" ", 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => settlement.Capture("test.faction.b", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => settlement.Capture("test.faction.b", 5));

        Assert.Equal("test.faction.a", settlement.OwnerFactionId);
        Assert.Single(settlement.BuildingIds);
        Assert.Equal(2, settlement.Level);
    }
}
