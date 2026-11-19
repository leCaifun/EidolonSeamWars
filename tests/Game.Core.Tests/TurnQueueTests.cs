using Game.Core.Battle;
using Game.Core.Domain;

namespace Game.Core.Tests;

public class TurnQueueTests
{
    private static BattleUnit Unit(string id, BattleSide side, int slot, int initiative)
    {
        UnitDef def = TestContent.CreateUnit(id: $"test.{id}", initiative: initiative);
        return new BattleUnit(id, side, slot, def, 3, Hex.Zero);
    }

    private static string Order(TurnQueue queue) =>
        string.Join(",", queue.GetRemaining().Select(entry => entry.Unit.Id + (entry.IsExtraAction ? "*" : string.Empty)));

    [Fact]
    public void StartRound_SortsByInitiativeDescending()
    {
        BattleUnit slow = Unit("slow", BattleSide.Attacker, 0, 3);
        BattleUnit fast = Unit("fast", BattleSide.Defender, 0, 9);
        BattleUnit mid = Unit("mid", BattleSide.Attacker, 1, 6);
        TurnQueue queue = new();

        queue.StartRound(new[] { slow, fast, mid });

        Assert.Equal(1, queue.Round);
        Assert.Equal("fast,mid,slow", Order(queue));
    }

    [Fact]
    public void StartRound_TieBreak_AlternatesSidesStartingWithAttackerThenSlot()
    {
        BattleUnit a1 = Unit("a1", BattleSide.Attacker, 1, 5);
        BattleUnit a0 = Unit("a0", BattleSide.Attacker, 0, 5);
        BattleUnit a2 = Unit("a2", BattleSide.Attacker, 2, 5);
        BattleUnit d1 = Unit("d1", BattleSide.Defender, 1, 5);
        BattleUnit d0 = Unit("d0", BattleSide.Defender, 0, 5);
        TurnQueue queue = new();

        queue.StartRound(new[] { d1, a2, a1, d0, a0 });

        Assert.Equal("a0,d0,a1,d1,a2", Order(queue));
    }

    [Fact]
    public void Wait_MovesTurnToEndAndForbidsSecondWait()
    {
        BattleUnit first = Unit("first", BattleSide.Attacker, 0, 9);
        BattleUnit second = Unit("second", BattleSide.Defender, 0, 5);
        TurnQueue queue = new();
        queue.StartRound(new[] { first, second });

        Assert.True(queue.CanWait());
        queue.Wait();

        Assert.Equal("second,first", Order(queue));

        queue.CompleteTurn();
        Assert.Equal("first", queue.Current!.Value.Unit.Id);
        Assert.False(queue.CanWait());
        Assert.Throws<InvalidOperationException>(() => queue.Wait());
    }

    [Fact]
    public void NextRound_WaitedUnitsGoToGroupBAfterNonWaiting()
    {
        BattleUnit fast = Unit("fast", BattleSide.Attacker, 0, 9);
        BattleUnit slow = Unit("slow", BattleSide.Defender, 0, 3);
        TurnQueue queue = new();
        List<BattleUnit> units = new() { fast, slow };

        queue.StartRound(units);
        queue.Wait();
        queue.CompleteTurn();
        queue.CompleteTurn();
        Assert.True(queue.IsRoundComplete);

        queue.StartRound(units);

        Assert.Equal(2, queue.Round);
        Assert.Equal("slow,fast", Order(queue));
        Assert.True(fast.WaitedLastRound);
    }

    [Fact]
    public void NextRound_WaitedLastRoundFlagIsConsumedOneRoundOnly()
    {
        BattleUnit fast = Unit("fast", BattleSide.Attacker, 0, 9);
        BattleUnit slow = Unit("slow", BattleSide.Defender, 0, 3);
        TurnQueue queue = new();
        List<BattleUnit> units = new() { fast, slow };

        queue.StartRound(units);
        queue.Wait();
        queue.CompleteTurn();
        queue.CompleteTurn();
        queue.StartRound(units);
        queue.CompleteTurn();
        queue.CompleteTurn();
        queue.StartRound(units);

        Assert.Equal("fast,slow", Order(queue));
    }

    [Fact]
    public void DeadUnits_AreSkippedAndExcluded()
    {
        BattleUnit a = Unit("a", BattleSide.Attacker, 0, 9);
        BattleUnit b = Unit("b", BattleSide.Defender, 0, 5);
        BattleUnit c = Unit("c", BattleSide.Attacker, 1, 1);
        TurnQueue queue = new();
        queue.StartRound(new[] { a, b, c });

        a.ApplyDamage(1000);
        Assert.Equal("b,c", Order(queue));
        Assert.Equal("b", queue.Current!.Value.Unit.Id);

        c.ApplyDamage(1000);
        queue.CompleteTurn();
        Assert.True(queue.IsRoundComplete);

        queue.StartRound(new[] { a, b, c });
        Assert.Equal("b", Order(queue));
    }

    [Fact]
    public void AppendExtraAction_AddsEntryAtEndAndForbidsWaiting()
    {
        BattleUnit a = Unit("a", BattleSide.Attacker, 0, 9);
        BattleUnit b = Unit("b", BattleSide.Defender, 0, 5);
        TurnQueue queue = new();
        queue.StartRound(new[] { a, b });

        queue.AppendExtraAction(a);

        Assert.Equal("a,b,a*", Order(queue));
        Assert.Equal(1, a.ExtraActionsUsedThisRound);

        queue.CompleteTurn();
        queue.CompleteTurn();
        Assert.True(queue.Current!.Value.IsExtraAction);
        Assert.False(queue.CanWait());

        a.ApplyDamage(1000);
        Assert.Throws<InvalidOperationException>(() => queue.AppendExtraAction(a));
    }

    [Fact]
    public void CompleteTurn_OnEmptyQueue_Throws()
    {
        TurnQueue queue = new();

        Assert.Null(queue.Current);
        Assert.True(queue.IsRoundComplete);
        Assert.Throws<InvalidOperationException>(() => queue.CompleteTurn());
        Assert.False(queue.CanWait());
    }

    [Fact]
    public void StartRound_DuplicateIds_Throws()
    {
        TurnQueue queue = new();

        Assert.Throws<ArgumentException>(() => queue.StartRound(new[]
        {
            Unit("x", BattleSide.Attacker, 0, 5),
            Unit("x", BattleSide.Defender, 0, 5)
        }));
        Assert.Throws<ArgumentNullException>(() => queue.StartRound(null!));
    }

    [Fact]
    public void StartRound_ResetsRetaliationAndExtraCounters()
    {
        BattleUnit a = Unit("a", BattleSide.Attacker, 0, 9);
        TurnQueue queue = new();
        queue.StartRound(new[] { a });
        a.SpendRetaliation();
        queue.AppendExtraAction(a);

        queue.StartRound(new[] { a });

        Assert.Equal(0, a.RetaliationsUsedThisRound);
        Assert.Equal(0, a.ExtraActionsUsedThisRound);
    }

    [Fact]
    public void Queue_LogsRoundOrder()
    {
        TestLogger logger = new();
        TurnQueue queue = new(logger);

        queue.StartRound(new[] { Unit("a", BattleSide.Attacker, 0, 9), Unit("b", BattleSide.Defender, 0, 5) });

        Assert.Contains(logger.Events, e => e.Category == "Battle" && e.Fields["order"] == "a,b");
    }
}
