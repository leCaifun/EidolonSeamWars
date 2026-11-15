using Game.Core.Domain;

namespace Game.Core.Tests;

public class StackTests
{
    [Fact]
    public void Constructor_RejectsNullUnit()
    {
        Assert.Throws<ArgumentNullException>(() => new Stack(null!, 1));
    }

    [Fact]
    public void Constructor_RejectsNegativeCountAndKills()
    {
        UnitDef unit = TestContent.CreateUnit();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Stack(unit, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Stack(unit, 1, -1));
    }

    [Fact]
    public void Totals_MultiplyByCount()
    {
        UnitDef unit = TestContent.CreateUnit(leadershipCost: 2, health: 12);
        Stack stack = new(unit, 5);

        Assert.Equal(10, stack.LeadershipCostTotal);
        Assert.Equal(60, stack.TotalHealth);
        Assert.False(stack.IsEmpty);
    }

    [Fact]
    public void EmptyStack_IsEmptyAndHasZeroTotals()
    {
        Stack stack = new(TestContent.CreateUnit(), 0);

        Assert.True(stack.IsEmpty);
        Assert.Equal(0, stack.LeadershipCostTotal);
        Assert.Equal(0, stack.TotalHealth);
    }

    [Fact]
    public void TakeCasualties_RemovesAtMostCount()
    {
        Stack stack = new(TestContent.CreateUnit(), 5);

        Assert.Equal(3, stack.TakeCasualties(3));
        Assert.Equal(2, stack.Count);
        Assert.Equal(2, stack.TakeCasualties(10));
        Assert.Equal(0, stack.Count);
        Assert.Equal(0, stack.TakeCasualties(1));
    }

    [Fact]
    public void TakeCasualties_RejectsNegativeLosses()
    {
        Stack stack = new(TestContent.CreateUnit(), 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => stack.TakeCasualties(-1));
    }

    [Fact]
    public void Merge_AddsCountAndKillsOfSameUnit()
    {
        UnitDef unit = TestContent.CreateUnit();
        Stack target = new(unit, 4, 2);
        Stack source = new(unit, 3, 1);

        target.Merge(source);

        Assert.Equal(7, target.Count);
        Assert.Equal(3, target.KilledStacks);
    }

    [Fact]
    public void Merge_RejectsDifferentUnitsWithContext()
    {
        Stack target = new(TestContent.CreateUnit("test.unit.a"), 1);
        Stack source = new(TestContent.CreateUnit("test.unit.b"), 1);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => target.Merge(source));

        Assert.Contains("test.unit.a", exception.Message, StringComparison.Ordinal);
        Assert.Contains("test.unit.b", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Split_MovesProportionalKillsAndKeepsTotalKills()
    {
        UnitDef unit = TestContent.CreateUnit();
        Stack original = new(unit, 10, 10);

        Stack detached = original.Split(3);

        Assert.Equal(7, original.Count);
        Assert.Equal(3, detached.Count);
        Assert.Equal(3, detached.KilledStacks);
        Assert.Equal(10, original.KilledStacks + detached.KilledStacks);
    }

    [Fact]
    public void Split_DoesNotMoveMoreKillsThanAvailable()
    {
        UnitDef unit = TestContent.CreateUnit();
        Stack original = new(unit, 10, 1);

        Stack detached = original.Split(5);

        Assert.Equal(1, detached.KilledStacks);
        Assert.Equal(0, original.KilledStacks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Split_RejectsCountOutsideStack(int count)
    {
        Stack stack = new(TestContent.CreateUnit(), 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => stack.Split(count));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(9, 3)]
    [InlineData(10, 4)]
    [InlineData(17, 4)]
    [InlineData(18, 5)]
    [InlineData(100, 5)]
    public void VeterancyRank_FollowsThresholds(int killedStacks, int expectedRank)
    {
        Stack stack = new(TestContent.CreateUnit(), 1, killedStacks);

        Assert.Equal(expectedRank, stack.VeterancyRank);
    }

    [Fact]
    public void GetterStats_AddVeterancyBonuses()
    {
        UnitDef unit = TestContent.CreateUnit(attack: 5, defense: 5, critChancePercent: 5, critDamagePercent: 150);
        Stack stack = new(unit, 1, killedStacks: 5);

        Assert.Equal(3, stack.VeterancyRank);
        Assert.Equal(9, stack.Attack);
        Assert.Equal(9, stack.Defense);
        Assert.Equal(7, stack.CritChancePercent);
        Assert.Equal(152, stack.CritDamagePercent);
    }

    [Fact]
    public void GetterStats_ClampCritChanceAndCritDamage()
    {
        UnitDef unit = TestContent.CreateUnit(critChancePercent: 75, critDamagePercent: 300);
        Stack stack = new(unit, 1, killedStacks: 18);

        Assert.Equal(5, stack.VeterancyRank);
        Assert.Equal(75, stack.CritChancePercent);
        Assert.Equal(300, stack.CritDamagePercent);
    }

    [Fact]
    public void RegisterKilledStacks_ReturnsNewRankAndRejectsNegative()
    {
        Stack stack = new(TestContent.CreateUnit(), 1);

        Assert.Equal(2, stack.RegisterKilledStacks(2));
        Assert.Equal(3, stack.RegisterKilledStacks(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => stack.RegisterKilledStacks(-1));
    }
}
