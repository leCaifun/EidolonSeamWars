using Game.Core.Content;
using Game.Core.Domain;
using Game.Core.Map;

namespace Game.Core.Tests;

public class MapTests
{
    private const string SmallMap = "core.test_map.small_8x8";
    private const string MediumMap = "core.test_map.medium_16x16";
    private const string FullMap = "core.test_map.full_48x48";

    private static ContentRegistry Registry =>
        RealContent.MergeWith().Registry
        ?? throw new InvalidOperationException("Контент не слит.");

    private static HandmadeMapResult Load(string id)
    {
        return new HandmadeMapLoader(Registry).Load(id);
    }

    private static Hex At(HandmadeMapResult map, int column, int row)
    {
        return Hex.FromOffsetCoordinates(column, row, map.Layout.Layout);
    }

    private static MapState CreateState(
        HandmadeMapResult loaded,
        string partyId,
        Hex position,
        int movementPoints = 8)
    {
        MapGenerationSettings settings = MapContentAdapter.CreateSettings(Registry);
        Party party = new(
            partyId,
            PartyRole.Player,
            "core.faction.brass",
            null,
            Army.HeroLessCapacity,
            position,
            movementPoints);

        return new MapState(
            loaded.Map,
            new[] { party },
            new FogOfWar(loaded.Map.Grid),
            settings);
    }

    [Fact]
    public void HexGrid_OffsetRoundTrip_IsStableForAllHexes()
    {
        HexGrid grid = new(48, 48);

        foreach (Hex hex in grid.AllHexes())
        {
            Assert.True(grid.TryGetOffset(hex, out int column, out int row));
            Assert.Equal(hex, grid.HexAt(column, row));
            Assert.Equal((row * 48) + column, grid.IndexOf(hex));
        }
    }

    [Fact]
    public void HexGrid_KnownOddROffsetConversion_IsCorrect()
    {
        Hex hex = Hex.FromOffsetCoordinates(3, 4, HexOffsetLayout.OddR);

        Assert.Equal(1, hex.Q);
        Assert.Equal(4, hex.R);
        Assert.Equal((3, 4), hex.ToOffsetCoordinates(HexOffsetLayout.OddR));
    }

    [Theory]
    [InlineData(SmallMap, 1, 5, 2, 5, 2)]
    [InlineData(SmallMap, 1, 5, 4, 5, 10)]
    [InlineData(SmallMap, 1, 5, 4, 1, 18)]
    [InlineData(SmallMap, 1, 5, 0, 7, 10)]
    [InlineData(MediumMap, 2, 13, 5, 8, 6)]
    [InlineData(MediumMap, 2, 13, 8, 4, 11)]
    [InlineData(MediumMap, 2, 13, 13, 2, 17)]
    [InlineData(FullMap, 6, 40, 41, 7, 68)]
    [InlineData(FullMap, 6, 40, 40, 40, 35)]
    public void Pathfinder_MatchesFixtureCosts(
        string mapId,
        int fromColumn,
        int fromRow,
        int toColumn,
        int toRow,
        int expectedCost)
    {
        HandmadeMapResult loaded = Load(mapId);
        Hex start = At(loaded, fromColumn, fromRow);
        Hex goal = At(loaded, toColumn, toRow);

        PathResult path = new Pathfinder().FindPath(loaded.Map, start, goal);

        Assert.True(path.Success, path.Failure.ToString());
        Assert.Equal(expectedCost, path.Cost);
    }

    [Theory]
    [InlineData(SmallMap, 1, 5, 4, 3)]
    [InlineData(MediumMap, 2, 13, 14, 6)]
    public void Pathfinder_UnreachableGoal_Fails(
        string mapId,
        int fromColumn,
        int fromRow,
        int toColumn,
        int toRow)
    {
        HandmadeMapResult loaded = Load(mapId);
        Hex start = At(loaded, fromColumn, fromRow);
        Hex goal = At(loaded, toColumn, toRow);

        PathResult path = new Pathfinder().FindPath(loaded.Map, start, goal);

        Assert.False(path.Success);
        Assert.NotEqual(PathFailure.None, path.Failure);
    }

    [Theory]
    [InlineData(SmallMap, 1, 5, 5, 7, 10, "core.map_object.patrol_ash")]
    [InlineData(MediumMap, 2, 13, 7, 4, 10, "core.map_object.patrol_ash")]
    public void MoveParty_IntoBlockingObject_ReturnsCollision(
        string mapId,
        int fromColumn,
        int fromRow,
        int toColumn,
        int toRow,
        int expectedCost,
        string blockingDefinitionId)
    {
        HandmadeMapResult loaded = Load(mapId);
        Hex start = At(loaded, fromColumn, fromRow);
        Hex goal = At(loaded, toColumn, toRow);
        MapState state = CreateState(loaded, "party.player", start, movementPoints: 100);

        MapCommandResult result = state.Apply(MapCommand.MoveParty("party.player", goal));

        Assert.Equal(MapCommandOutcome.Collision, result.Outcome);
        Assert.Equal(expectedCost, result.Cost);
        Assert.Equal(blockingDefinitionId, result.BlockingDefinitionId);
        Assert.Equal(goal, state.FindParty("party.player")!.Position);
    }

    [Fact]
    public void FogOfWar_RevealsExpectedRadiusAroundParty()
    {
        HandmadeMapResult loaded = Load(MediumMap);
        Hex start = At(loaded, 2, 13);
        MapState state = CreateState(loaded, "party.player", start);

        int expectedVisible = loaded.Map.Grid.WithinRange(start, 3).Count();

        Assert.Equal(expectedVisible, state.Fog.VisibleCount("core.faction.brass"));
        Assert.True(state.Fog.IsVisible("core.faction.brass", start));

        Hex atDistanceFour = loaded.Map.Grid
            .WithinRange(start, 4)
            .First(hex => Hex.Distance(start, hex) == 4);

        Assert.False(state.Fog.IsVisible("core.faction.brass", atDistanceFour));
        Assert.True(state.Fog.IsExplored("core.faction.brass", atDistanceFour));
    }

    [Fact]
    public void FogOfWar_TwoPartiesOfOneFaction_KeepBothVisionAreas()
    {
        HandmadeMapResult loaded = Load(FullMap);
        List<Hex> passable = loaded.Map.Grid.AllHexes().Where(loaded.Map.IsPassable).ToList();
        Hex first = passable[0];
        Hex last = passable[^1];

        Assert.True(Hex.Distance(first, last) > 8, "Партии должны быть далеко друг от друга.");

        MapGenerationSettings settings = MapContentAdapter.CreateSettings(Registry);
        Party a = new("party.a", PartyRole.Player, "core.faction.brass", null, Army.HeroLessCapacity, first, 8);
        Party b = new("party.b", PartyRole.Player, "core.faction.brass", null, Army.HeroLessCapacity, last, 8);

        MapState state = new(loaded.Map, new[] { a, b }, new FogOfWar(loaded.Map.Grid), settings);

        Assert.True(state.Fog.IsVisible("core.faction.brass", first));
        Assert.True(state.Fog.IsVisible("core.faction.brass", last));

        state.Apply(MapCommand.EndDay());

        Assert.True(state.Fog.IsVisible("core.faction.brass", first));
        Assert.True(state.Fog.IsVisible("core.faction.brass", last));
    }

    [Fact]
    public void FullMap_HasExpectedReachableCount()
    {
        HandmadeMapResult loaded = Load(FullMap);
        Hex start = At(loaded, 6, 40);

        IReadOnlyDictionary<Hex, int> reachable =
            new Pathfinder().ComputeReachable(loaded.Map, start, int.MaxValue);

        Assert.Equal(2067, reachable.Count);
    }

    [Fact]
    public void FullMap_HasExpectedObjectCount()
    {
        HandmadeMapResult loaded = Load(FullMap);

        Assert.Equal(51, loaded.Map.Objects.Count);
    }

    [Fact]
    public void FullMap_StartsAreFarEnoughApart()
    {
        HandmadeMapResult loaded = Load(FullMap);
        IReadOnlyList<MapStartDefinition> starts = loaded.Layout.Starts;

        for (int i = 0; i < starts.Count; i++)
        {
            for (int j = i + 1; j < starts.Count; j++)
            {
                Assert.True(
                    Hex.Distance(starts[i].Position, starts[j].Position) >= 20,
                    $"Старты {starts[i].Role} и {starts[j].Role} слишком близко.");
            }
        }
    }

    [Fact]
    public void MoveParty_DeductsActualCostAndMovesParty()
    {
        HandmadeMapResult loaded = Load(SmallMap);
        Hex start = At(loaded, 1, 5);
        Hex goal = At(loaded, 2, 5);
        MapState state = CreateState(loaded, "party.player", start);

        MapCommandResult result = state.Apply(MapCommand.MoveParty("party.player", goal));

        Assert.Equal(MapCommandOutcome.Applied, result.Outcome);
        Assert.Equal(2, result.Cost);
        Assert.Equal(goal, state.FindParty("party.player")!.Position);
        Assert.Equal(6, state.FindParty("party.player")!.MovementPoints);
    }

    [Fact]
    public void MoveParty_WithoutEnoughMovement_IsRejectedAndDoesNotMove()
    {
        HandmadeMapResult loaded = Load(FullMap);
        Hex start = At(loaded, 6, 40);
        Hex goal = At(loaded, 41, 7);
        MapState state = CreateState(loaded, "party.player", start, movementPoints: 8);

        MapCommandResult result = state.Apply(MapCommand.MoveParty("party.player", goal));

        Assert.Equal(MapCommandOutcome.NotEnoughMovement, result.Outcome);
        Assert.Equal(start, state.FindParty("party.player")!.Position);
        Assert.Equal(8, state.FindParty("party.player")!.MovementPoints);
    }

    [Fact]
    public void MoveParty_IntoImpassableTerrain_IsRejected()
    {
        HandmadeMapResult loaded = Load(SmallMap);
        Hex start = At(loaded, 1, 5);
        Hex voidHex = At(loaded, 4, 3);
        MapState state = CreateState(loaded, "party.player", start);

        MapCommandResult result = state.Apply(MapCommand.MoveParty("party.player", voidHex));

        Assert.Equal(MapCommandOutcome.Impassable, result.Outcome);
        Assert.Equal(start, state.FindParty("party.player")!.Position);
    }

    [Fact]
    public void MoveParty_OutsideGrid_IsRejected()
    {
        HandmadeMapResult loaded = Load(SmallMap);
        Hex start = At(loaded, 1, 5);
        MapState state = CreateState(loaded, "party.player", start);

        MapCommandResult result = state.Apply(MapCommand.MoveParty("party.player", new Hex(999, 999)));

        Assert.Equal(MapCommandOutcome.OutOfBounds, result.Outcome);
    }

    [Fact]
    public void MoveParty_IntoAnotherParty_ReturnsCollision()
    {
        HandmadeMapResult loaded = Load(SmallMap);
        Hex start = At(loaded, 1, 5);
        Hex goal = At(loaded, 2, 5);
        MapGenerationSettings settings = MapContentAdapter.CreateSettings(Registry);

        Party player = new(
            "party.player",
            PartyRole.Player,
            "core.faction.brass",
            null,
            Army.HeroLessCapacity,
            start,
            8);

        Party enemy = new(
            "party.enemy",
            PartyRole.Enemy,
            "core.faction.ash",
            null,
            Army.HeroLessCapacity,
            goal,
            8);

        MapState state = new(
            loaded.Map,
            new[] { player, enemy },
            new FogOfWar(loaded.Map.Grid),
            settings);

        MapCommandResult result = state.Apply(MapCommand.MoveParty("party.player", goal));

        Assert.Equal(MapCommandOutcome.Collision, result.Outcome);
        Assert.Equal("party.enemy", result.EncounterPartyId);
        Assert.Equal(goal, player.Position);
    }

    [Fact]
    public void EnterObject_NonBlockingObject_IsVisited()
    {
        HandmadeMapResult loaded = Load(SmallMap);
        Hex cacheHex = At(loaded, 7, 5);
        MapState state = CreateState(loaded, "party.player", cacheHex);

        MapObjectInstance cache = loaded.Map.GetObjectsAt(cacheHex).Single();
        MapCommandResult result = state.Apply(MapCommand.EnterObject("party.player", cache.InstanceId));

        Assert.Equal(MapCommandOutcome.ObjectEntered, result.Outcome);
        Assert.True(cache.Visited);
    }

    [Fact]
    public void EnterObject_GuardedObject_ReturnsCollisionWithoutAutoBattle()
    {
        HandmadeMapResult loaded = Load(SmallMap);
        Hex dwellingHex = At(loaded, 0, 7);
        MapState state = CreateState(loaded, "party.player", dwellingHex);

        MapObjectInstance dwelling = loaded.Map.GetObjectsAt(dwellingHex).Single();
        MapCommandResult result = state.Apply(MapCommand.EnterObject("party.player", dwelling.InstanceId));

        Assert.Equal(MapCommandOutcome.Collision, result.Outcome);
        Assert.Equal("core.map_object.dwelling_brass", result.BlockingDefinitionId);
        Assert.False(dwelling.Visited);
    }

    [Fact]
    public void EndDay_IncrementsDayAndRestoresMovement()
    {
        HandmadeMapResult loaded = Load(SmallMap);
        Hex start = At(loaded, 1, 5);
        MapState state = CreateState(loaded, "party.player", start);

        state.Apply(MapCommand.MoveParty("party.player", At(loaded, 2, 5)));
        MapCommandResult result = state.Apply(MapCommand.EndDay());

        Assert.Equal(MapCommandOutcome.DayEnded, result.Outcome);
        Assert.Equal(2, state.Day);
        Assert.Equal(8, state.FindParty("party.player")!.MovementPoints);
    }

    [Fact]
    public void CommandCodec_RoundTripsAllCommandKinds()
    {
        MapCommand[] commands =
        {
            MapCommand.MoveParty("party.player", new Hex(3, -1)),
            MapCommand.EndDay(),
            MapCommand.EnterObject("party.player", "core.test_map.small_8x8.obj.1")
        };

        string json = MapCommandCodec.Serialize(commands);
        IReadOnlyList<MapCommand> restored = MapCommandCodec.Deserialize(json);

        Assert.Equal(commands.Length, restored.Count);
        Assert.Equal(MapCommandKind.MoveParty, restored[0].Kind);
        Assert.Equal("party.player", restored[0].PartyId);
        Assert.Equal(new Hex(3, -1), restored[0].Target);
        Assert.Equal(MapCommandKind.EndDay, restored[1].Kind);
        Assert.Equal(MapCommandKind.EnterObject, restored[2].Kind);
        Assert.Equal("core.test_map.small_8x8.obj.1", restored[2].ObjectInstanceId);
    }

    [Fact]
    public void CommandReplay_ProducesSameFinalState()
    {
        HandmadeMapResult loaded = Load(SmallMap);
        Hex start = At(loaded, 1, 5);

        MapState original = CreateState(loaded, "party.player", start);
        MapCommandJournal journal = new(42);

        journal.Apply(original, MapCommand.MoveParty("party.player", At(loaded, 2, 5)));
        journal.Apply(original, MapCommand.EndDay());
        journal.Apply(original, MapCommand.MoveParty("party.player", At(loaded, 3, 5)));

        string json = MapCommandCodec.Serialize(journal.Commands);

        MapState replayed = CreateState(loaded, "party.player", start);

        foreach (MapCommand command in MapCommandCodec.Deserialize(json))
        {
            replayed.Apply(command);
        }

        Assert.Equal(original.Day, replayed.Day);
        Assert.Equal(
            original.FindParty("party.player")!.Position,
            replayed.FindParty("party.player")!.Position);
        Assert.Equal(
            original.FindParty("party.player")!.MovementPoints,
            replayed.FindParty("party.player")!.MovementPoints);
    }

    [Fact]
    public void Generator_SameSeed_ProducesSameMap()
    {
        ContentRegistry registry = Registry;
        MapGenerationSettings settings = MapContentAdapter.CreateSettings(registry);
        BiomeCatalog biomes = MapContentAdapter.CreateBiomes(registry);
        MapObjectCatalog objects = MapContentAdapter.CreateMapObjects(registry);

        IReadOnlyList<MapStartDefinition> starts = MapGenerator.CreateDefaultStarts(
            settings,
            new[] { "core.faction.brass", "core.faction.ash", "core.faction.root" });

        MapStartRequest request = new(123456789UL, settings, starts);
        MapGenerator generator = new(biomes, objects);

        MapLayoutDefinition first = generator.Generate(request);
        MapLayoutDefinition second = generator.Generate(request);

        Assert.Equal(first.Tiles.Count, second.Tiles.Count);
        Assert.Equal(
            first.Tiles.Select(tile => $"{tile.Position}|{tile.BiomeId}"),
            second.Tiles.Select(tile => $"{tile.Position}|{tile.BiomeId}"));
        Assert.Equal(
            first.Objects.Select(item => $"{item.InstanceId}|{item.DefinitionId}|{item.Position}"),
            second.Objects.Select(item => $"{item.InstanceId}|{item.DefinitionId}|{item.Position}"));
        Assert.Equal(51, first.Objects.Count);
    }

    [Fact]
    public void Generator_DifferentSeed_ChangesMap()
    {
        ContentRegistry registry = Registry;
        MapGenerationSettings settings = MapContentAdapter.CreateSettings(registry);
        BiomeCatalog biomes = MapContentAdapter.CreateBiomes(registry);
        MapObjectCatalog objects = MapContentAdapter.CreateMapObjects(registry);

        IReadOnlyList<MapStartDefinition> starts = MapGenerator.CreateDefaultStarts(
            settings,
            new[] { "core.faction.brass", "core.faction.ash", "core.faction.root" });

        MapGenerator generator = new(biomes, objects);

        MapLayoutDefinition first = generator.Generate(new MapStartRequest(1UL, settings, starts));
        MapLayoutDefinition second = generator.Generate(new MapStartRequest(2UL, settings, starts));

        Assert.NotEqual(
            first.Tiles.Select(tile => tile.BiomeId),
            second.Tiles.Select(tile => tile.BiomeId));
    }

    [Fact]
    public void Generator_ProducesPlayableStateWithVisibleStarts()
    {
        ContentRegistry registry = Registry;
        MapGenerationSettings settings = MapContentAdapter.CreateSettings(registry);
        BiomeCatalog biomes = MapContentAdapter.CreateBiomes(registry);
        MapObjectCatalog objects = MapContentAdapter.CreateMapObjects(registry);

        IReadOnlyList<MapStartDefinition> starts = MapGenerator.CreateDefaultStarts(
            settings,
            new[] { "core.faction.brass", "core.faction.ash", "core.faction.root" });

        MapLayoutDefinition layout = new MapGenerator(biomes, objects)
            .Generate(new MapStartRequest(777UL, settings, starts));

        GameMap map = MapContentAdapter.CreateMap(
            layout,
            biomes,
            objects);
        IReadOnlyList<Party> parties = MapContentAdapter.CreateParties(layout, settings, registry);

        MapState state = new(map, parties, new FogOfWar(map.Grid), settings);

        Assert.Equal(3, state.Parties.Count);

        foreach (Party party in state.Parties.Values)
        {
            Assert.True(map.IsPassable(party.Position));
            Assert.True(state.Fog.IsVisible(party.FactionId, party.Position));

            if (party.HeroId is not null)
            {
                int logistics = registry.Heroes[party.HeroId].Value.Stats.Logistics;
                Assert.Equal(Hero.BaseMovementPoints + logistics, party.MovementPointsPerDay);
            }
        }
    }

    [Fact]
    public void RoadBiome_CostsExactlyOnePoint()
    {
        HandmadeMapResult loaded = Load(MediumMap);
        Hex road = loaded.Map.Grid
            .AllHexes()
            .First(hex => loaded.Map.GetBiome(hex).Road);

        Assert.Equal(1, loaded.Map.GetMovementCost(road));
        Assert.Equal(1, loaded.Map.GetBiome(road).MovementCostPerHex);
    }
}
