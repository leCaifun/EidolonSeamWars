using Game.Core.Content;
using Game.Core.Content.Dto;
using Game.Core.Map;

namespace Game.Core.Tests;

public class MapContentTests
{
    private static ContentRegistry Registry =>
        RealContent.MergeWith().Registry
        ?? throw new InvalidOperationException("Контент не слит.");

    [Fact]
    public void MapGenerationRules_AreLoadedFromContent()
    {
        MapGenerationSettings settings = MapContentAdapter.CreateSettings(Registry);

        Assert.Equal(48, settings.Width);
        Assert.Equal(48, settings.Height);
        Assert.Equal("core.biome.grass", settings.DefaultBiomeId);
        Assert.Equal(21, settings.MineCount);
        Assert.Equal(12, settings.DwellingCount);
        Assert.Equal(6, settings.EchoCacheCount);
        Assert.Equal(9, settings.PatrolCount);
        Assert.Equal(12, settings.ArmyMovementPointsPerDay);
        Assert.Equal(3, settings.FogVisibilityRadius);
        Assert.Equal(5, settings.FogExploredRadius);
    }

    [Fact]
    public void BiomeCatalog_ContainsRoadFlagOnlyForRoad()
    {
        BiomeCatalog biomes = MapContentAdapter.CreateBiomes(Registry);

        BiomeDefinition road = biomes.Get("core.biome.road");

        Assert.True(road.Road);
        Assert.Equal(1, road.MovementCostPerHex);
        Assert.All(
            biomes.All.Values.Where(biome => biome.Id != road.Id),
            biome => Assert.False(biome.Road));
    }

    [Fact]
    public void Biomes_TerrainCostsAtLeastTwoAndRoadCostsOne()
    {
        BiomeCatalog biomes = MapContentAdapter.CreateBiomes(Registry);

        Assert.All(
            biomes.All.Values.Where(biome => biome.Passable && !biome.Road),
            biome => Assert.True(biome.MovementCostPerHex >= 2, biome.Id));
        Assert.Equal(1, biomes.Get("core.biome.road").MovementCostPerHex);
        Assert.All(
            biomes.All.Values.Where(biome => biome.FlyerPassable),
            biome => Assert.True(biome.FlyerMovementCost >= 2, biome.Id));
    }

    [Fact]
    public void HandmadeMapLoader_LoadsAllThreeFixtures()
    {
        IReadOnlyList<HandmadeMapResult> maps = new HandmadeMapLoader(Registry).LoadAll();

        Assert.Equal(3, maps.Count);
        Assert.Equal(
            new[]
            {
                "core.test_map.full_48x48",
                "core.test_map.medium_16x16",
                "core.test_map.small_8x8"
            },
            maps.Select(map => map.Definition.Id));
    }

    [Fact]
    public void HandmadeMapLoader_BuildsTilesObjectsAndStarts()
    {
        HandmadeMapResult loaded = new HandmadeMapLoader(Registry)
            .Load("core.test_map.medium_16x16");

        Assert.Equal(16, loaded.Map.Grid.Width);
        Assert.Equal(16, loaded.Map.Grid.Height);
        Assert.Equal(256, loaded.Layout.Tiles.Count);
        Assert.Equal(loaded.Definition.Objects.Count, loaded.Map.Objects.Count);
        Assert.Equal(2, loaded.Layout.Starts.Count);
        Assert.Equal(PartyRole.Player, loaded.Layout.Starts[0].Role);
        Assert.Equal(PartyRole.Enemy, loaded.Layout.Starts[1].Role);
    }

    [Fact]
    public void CreateLayout_RejectsRowWithWrongLength()
    {
        TestMapDto dto = new()
        {
            SchemaVersion = 1,
            Id = "core.test_map.bad_row",
            NameKey = "test_map.bad.name",
            Width = 2,
            Height = 2,
            OffsetType = "odd-r",
            Legend = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["G"] = "core.biome.grass"
            },
            Layout = new[] { "GG", "G" }
        };

        Assert.Throws<InvalidOperationException>(
            () => MapContentAdapter.CreateLayout(Registry, dto));
    }

    [Fact]
    public void CreateLayout_RejectsUnknownLegendSymbol()
    {
        TestMapDto dto = new()
        {
            SchemaVersion = 1,
            Id = "core.test_map.bad_symbol",
            NameKey = "test_map.bad.name",
            Width = 2,
            Height = 2,
            OffsetType = "odd-r",
            Legend = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["G"] = "core.biome.grass"
            },
            Layout = new[] { "GG", "GX" }
        };

        Assert.Throws<InvalidOperationException>(
            () => MapContentAdapter.CreateLayout(Registry, dto));
    }

    [Fact]
    public void CreateLayout_RejectsUnknownBiome()
    {
        TestMapDto dto = new()
        {
            SchemaVersion = 1,
            Id = "core.test_map.bad_biome",
            NameKey = "test_map.bad.name",
            Width = 2,
            Height = 2,
            OffsetType = "odd-r",
            Legend = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["G"] = "core.biome.missing"
            },
            Layout = new[] { "GG", "GG" }
        };

        Assert.Throws<InvalidOperationException>(
            () => MapContentAdapter.CreateLayout(Registry, dto));
    }
}
