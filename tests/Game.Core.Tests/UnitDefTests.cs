using Game.Core.Domain;

namespace Game.Core.Tests;

public class UnitDefTests
{
    [Fact]
    public void Constructor_CopiesAllFields()
    {
        Resistances resistances = new(10, 20, 30, 40, 50);

        UnitDef unit = new(
            "test.unit",
            1,
            "test.unit.name",
            "test.unit.description",
            "test.faction",
            3,
            2,
            7,
            8,
            9,
            4,
            5,
            6,
            15,
            200,
            25,
            MovementType.Ranged,
            resistances,
            new[] { "test.ability" },
            new[] { "test.promotion" },
            12);

        Assert.Equal("test.unit", unit.Id);
        Assert.Equal(1, unit.SchemaVersion);
        Assert.Equal("test.unit.name", unit.NameKey);
        Assert.Equal("test.unit.description", unit.DescriptionKey);
        Assert.Equal("test.faction", unit.FactionId);
        Assert.Equal(3, unit.Tier);
        Assert.Equal(2, unit.LeadershipCost);
        Assert.Equal(7, unit.Attack);
        Assert.Equal(8, unit.Defense);
        Assert.Equal(9, unit.Initiative);
        Assert.Equal(4, unit.Speed);
        Assert.Equal(5, unit.DamageMin);
        Assert.Equal(6, unit.DamageMax);
        Assert.Equal(15, unit.CritChancePercent);
        Assert.Equal(200, unit.CritDamagePercent);
        Assert.Equal(25, unit.Health);
        Assert.Equal(MovementType.Ranged, unit.MovementType);
        Assert.Equal(resistances, unit.Resistances);
        Assert.Equal(new[] { "test.ability" }, unit.AbilityIds);
        Assert.Equal(new[] { "test.promotion" }, unit.PromotionIds);
        Assert.Equal(12, unit.Ammo);
    }

    [Fact]
    public void Constructor_DefaultsAbilityAndPromotionListsToEmpty()
    {
        UnitDef unit = TestContent.CreateUnit();

        Assert.Empty(unit.AbilityIds);
        Assert.Empty(unit.PromotionIds);
        Assert.Equal(0, unit.Ammo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Constructor_RejectsTierOutsideOneToFive(int tier)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new UnitDef(
                "test.unit",
                1,
                "test.unit.name",
                "test.unit.description",
                "test.faction",
                tier,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                5,
                150,
                10,
                MovementType.Infantry,
                Resistances.None));
    }

    [Fact]
    public void Constructor_RejectsZeroLeadershipCost()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestContent.CreateUnit(leadershipCost: 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(76)]
    public void Constructor_RejectsCritChanceOutsideZeroToSeventyFive(int critChance)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestContent.CreateUnit(critChancePercent: critChance));
    }

    [Theory]
    [InlineData(99)]
    [InlineData(301)]
    public void Constructor_RejectsCritDamageOutsideOneHundredToThreeHundred(int critDamage)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestContent.CreateUnit(critDamagePercent: critDamage));
    }

    [Fact]
    public void Constructor_RejectsDamageMaxBelowDamageMin()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => TestContent.CreateUnit(damageMin: 10, damageMax: 9));

        Assert.Contains("Максимальный урон", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_RejectsZeroHealth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestContent.CreateUnit(health: 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyIdentifier(string id)
    {
        Assert.Throws<ArgumentException>(() => TestContent.CreateUnit(id: id));
    }

    [Fact]
    public void Constructor_RejectsBlankAbilityId()
    {
        Assert.Throws<ArgumentException>(
            () => new UnitDef(
                "test.unit",
                1,
                "test.unit.name",
                "test.unit.description",
                "test.faction",
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                5,
                150,
                10,
                MovementType.Infantry,
                Resistances.None,
                new[] { "test.ability", " " }));
    }

    [Fact]
    public void CreateStack_UsesUnitAndCount()
    {
        UnitDef unit = TestContent.CreateUnit();

        Stack stack = unit.CreateStack(7, 3);

        Assert.Same(unit, stack.Unit);
        Assert.Equal(7, stack.Count);
        Assert.Equal(3, stack.KilledStacks);
    }
}
