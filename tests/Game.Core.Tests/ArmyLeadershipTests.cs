using Game.Core.Domain;
using Game.Core.Diagnostics;

namespace Game.Core.Tests;

public class ArmyLeadershipTests
{
    [Fact]
    public void Constructor_RejectsNonPositiveCapacityAndInvalidSlotCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Army(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Army(10, slotCount: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Army(10, slotCount: Army.MaxSlotCount + 1));
    }

    [Fact]
    public void WithoutHero_UsesFixedCapacity()
    {
        Army army = Army.WithoutHero();

        Assert.Equal(Army.HeroLessCapacity, army.Capacity);
        Assert.Equal(Army.DefaultSlotCount, army.SlotCount);
        Assert.Equal(0, army.UsedLeadership);
        Assert.Equal(Army.HeroLessCapacity, army.RemainingLeadership);
    }

    [Fact]
    public void TryAdd_EnforcesLeadershipLimit()
    {
        Army army = new(capacity: 10);
        Stack fits = new(TestContent.CreateUnit("test.unit.a", leadershipCost: 1), 10);
        Stack overflows = new(TestContent.CreateUnit("test.unit.b", leadershipCost: 1), 1);

        Assert.True(army.TryAdd(fits));
        Assert.Equal(10, army.UsedLeadership);
        Assert.Equal(0, army.RemainingLeadership);
        Assert.False(army.TryAdd(overflows));
        Assert.Single(army.Slots);
    }

    [Fact]
    public void TryAdd_EnforcesSlotCount()
    {
        Army army = new(capacity: 100, slotCount: 1);

        Assert.True(army.TryAdd(new Stack(TestContent.CreateUnit("test.unit.a"), 1)));
        Assert.False(army.TryAdd(new Stack(TestContent.CreateUnit("test.unit.b"), 1)));
        Assert.Single(army.Slots);
    }

    [Fact]
    public void TryAdd_RejectsEmptyStack()
    {
        Army army = new(capacity: 10);
        Stack empty = new(TestContent.CreateUnit(), 0);

        Assert.False(army.CanAdd(empty));
        Assert.False(army.TryAdd(empty));
        Assert.Empty(army.Slots);
    }

    [Fact]
    public void RefusedAdd_LogsReasonWithCategoryEconomy()
    {
        TestLogger logger = new();
        Army army = new(capacity: 1, logger: logger);
        Stack tooLarge = new(TestContent.CreateUnit("test.unit", leadershipCost: 1), 2);

        Assert.False(army.TryAdd(tooLarge));

        LogEvent entry = Assert.Single(logger.Events);
        Assert.Equal("Economy", entry.Category);
        Assert.Contains("лидерство", entry.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryAdd_RejectsNullStack()
    {
        Army army = new(capacity: 10);

        Assert.Throws<ArgumentNullException>(() => army.TryAdd(null!));
        Assert.Throws<ArgumentNullException>(() => army.CanAdd(null!));
    }

    [Fact]
    public void RemoveAt_ReturnsAndRemovesStack()
    {
        Army army = new(capacity: 10);
        Stack stack = new(TestContent.CreateUnit(), 1);
        army.TryAdd(stack);

        Assert.Same(stack, army.RemoveAt(0));
        Assert.Empty(army.Slots);
        Assert.Throws<ArgumentOutOfRangeException>(() => army.RemoveAt(0));
    }

    [Fact]
    public void TryMerge_CombinesSameUnitAndFreesSlot()
    {
        Army army = new(capacity: 100);
        UnitDef unit = TestContent.CreateUnit();
        army.TryAdd(new Stack(unit, 2));
        army.TryAdd(new Stack(unit, 3));

        Assert.True(army.TryMerge(0, 1));

        Assert.Single(army.Slots);
        Assert.Equal(5, army.Slots[0].Count);
    }

    [Fact]
    public void TryMerge_RejectsDifferentUnitsWithoutChangingArmy()
    {
        Army army = new(capacity: 100);
        army.TryAdd(new Stack(TestContent.CreateUnit("test.unit.a"), 1));
        army.TryAdd(new Stack(TestContent.CreateUnit("test.unit.b"), 1));

        Assert.False(army.TryMerge(0, 1));
        Assert.Equal(2, army.Slots.Count);
    }

    [Fact]
    public void TryMerge_RejectsSameIndexAndInvalidIndexes()
    {
        Army army = new(capacity: 100);
        army.TryAdd(new Stack(TestContent.CreateUnit(), 1));

        Assert.Throws<ArgumentException>(() => army.TryMerge(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => army.TryMerge(0, 5));
    }

    [Fact]
    public void Clear_RemovesAllSlots()
    {
        Army army = new(capacity: 10);
        army.TryAdd(new Stack(TestContent.CreateUnit(), 1));

        army.Clear();

        Assert.Empty(army.Slots);
        Assert.Equal(0, army.UsedLeadership);
    }

    [Fact]
    public void Slots_IsReadOnlyViewOfArmy()
    {
        Army army = new(capacity: 10);
        army.TryAdd(new Stack(TestContent.CreateUnit(), 1));

        Assert.IsAssignableFrom<IReadOnlyList<Stack>>(army.Slots);
        Assert.Single(army.Slots);
    }
}
