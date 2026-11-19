using Game.Core.Battle;
using Game.Core.Domain;
using Game.Core.Rng;

namespace Game.Core.Tests;

public class DamageCalculatorTests
{
    private static BattleRules CreateRules(int critChanceCap = 75, int critDamageCap = 300)
    {
        return new BattleRules(
            13, 9, false, 25, 0, 0, 1, false, true, true, 50, 300, 4000, critChanceCap, critDamageCap, 80, 25, 25, 75, 1);
    }

    private static DamageRequest Request(
        int count = 10,
        int min = 3,
        int max = 3,
        int attack = 10,
        int defense = 10,
        DamageType type = DamageType.Physical,
        Resistances resistances = default,
        int critChance = 0,
        int critDamage = 150,
        int attackPercent = 0,
        int defensePercent = 0,
        int modifier = 1000)
    {
        return new DamageRequest(
            count, min, max, attack, defense, type, resistances, critChance, critDamage,
            attackPercent, defensePercent, modifier);
    }

    private static Resistances ResistanceFor(DamageType type, int percent)
    {
        return type switch
        {
            DamageType.Physical => new Resistances(percent, 0, 0, 0, 0),
            DamageType.Magic => new Resistances(0, percent, 0, 0, 0),
            DamageType.Poison => new Resistances(0, 0, percent, 0, 0),
            DamageType.Fire => new Resistances(0, 0, 0, percent, 0),
            _ => new Resistances(0, 0, 0, 0, percent)
        };
    }

    [Fact]
    public void Calculate_EqualStats_ReturnsBaseDamage()
    {
        DamageResult result = new DamageCalculator(CreateRules()).Calculate(Request(), new SeededRandom(1));

        Assert.Equal(30, result.FinalDamage);
        Assert.Equal(1000, result.AttackDefenseModifierPerMille);
        Assert.False(result.IsCritical);
    }

    [Fact]
    public void Calculate_AttackAboveDefense_AddsFiftyPerMillePerPoint()
    {
        DamageResult result = new DamageCalculator(CreateRules())
            .Calculate(Request(attack: 14), new SeededRandom(1));

        Assert.Equal(1200, result.AttackDefenseModifierPerMille);
        Assert.Equal(36, result.FinalDamage);
    }

    [Fact]
    public void Calculate_ModifierIsClampedToMaximum()
    {
        DamageResult result = new DamageCalculator(CreateRules())
            .Calculate(Request(attack: 100, defense: 0), new SeededRandom(1));

        Assert.Equal(4000, result.AttackDefenseModifierPerMille);
        Assert.Equal(120, result.FinalDamage);
    }

    [Fact]
    public void Calculate_ModifierIsClampedToMinimum()
    {
        DamageResult result = new DamageCalculator(CreateRules())
            .Calculate(Request(attack: 0, defense: 100), new SeededRandom(1));

        Assert.Equal(300, result.AttackDefenseModifierPerMille);
        Assert.Equal(9, result.FinalDamage);
    }

    [Theory]
    [InlineData(DamageType.Physical)]
    [InlineData(DamageType.Magic)]
    [InlineData(DamageType.Poison)]
    [InlineData(DamageType.Fire)]
    [InlineData(DamageType.Gunpowder)]
    public void Calculate_EachOfFiveResists_ReducesMatchingDamageType(DamageType type)
    {
        DamageCalculator calculator = new(CreateRules());

        DamageResult resisted = calculator.Calculate(
            Request(type: type, resistances: ResistanceFor(type, 50)), new SeededRandom(1));

        Assert.Equal(50, resisted.ResistPercent);
        Assert.Equal(15, resisted.FinalDamage);

        DamageType other = type == DamageType.Physical ? DamageType.Fire : DamageType.Physical;
        DamageResult unaffected = calculator.Calculate(
            Request(type: other, resistances: ResistanceFor(type, 50)), new SeededRandom(1));

        Assert.Equal(30, unaffected.FinalDamage);
    }

    [Fact]
    public void Calculate_FullResist_GivesZeroWithoutMinimumFloor()
    {
        DamageResult result = new DamageCalculator(CreateRules()).Calculate(
            Request(resistances: ResistanceFor(DamageType.Physical, 100)), new SeededRandom(1));

        Assert.Equal(0, result.FinalDamage);
    }

    [Fact]
    public void Calculate_MinimumDamageFloorApplies()
    {
        DamageResult result = new DamageCalculator(CreateRules()).Calculate(
            Request(count: 1, min: 1, max: 1, attack: 0, defense: 100), new SeededRandom(1));

        Assert.Equal(0, result.ModifiedDamage);
        Assert.Equal(DamageCalculator.MinimumDamage, result.FinalDamage);
    }

    [Fact]
    public void Calculate_Crit_MultipliesDamage()
    {
        DamageResult result = new DamageCalculator(CreateRules(critChanceCap: 100))
            .Calculate(Request(critChance: 100), new SeededRandom(1));

        Assert.True(result.IsCritical);
        Assert.Equal(45, result.FinalDamage);
    }

    [Fact]
    public void Calculate_CritDamageIsCapped()
    {
        DamageResult result = new DamageCalculator(CreateRules(critChanceCap: 100))
            .Calculate(Request(critChance: 100, critDamage: 500), new SeededRandom(1));

        Assert.Equal(90, result.FinalDamage);
    }

    [Fact]
    public void Calculate_CritChanceIsCappedAtSeventyFivePercent()
    {
        DamageCalculator calculator = new(CreateRules());
        SeededRandom random = new(12345);
        int crits = 0;

        for (int i = 0; i < 2000; i++)
        {
            if (calculator.Calculate(Request(count: 1, min: 1, max: 1, critChance: 100), random).IsCritical)
            {
                crits++;
            }
        }

        Assert.InRange(crits, 1400, 1600);
    }

    [Fact]
    public void Calculate_ZeroCritChance_NeverCrits()
    {
        DamageCalculator calculator = new(CreateRules());
        SeededRandom random = new(99);

        for (int i = 0; i < 1000; i++)
        {
            Assert.False(calculator.Calculate(Request(), random).IsCritical);
        }
    }

    [Fact]
    public void Calculate_DefenseAndMoralePercents_AreAppliedWithoutStatRounding()
    {
        DamageCalculator calculator = new(CreateRules());

        DamageResult defend = calculator.Calculate(
            Request(count: 100, min: 10, max: 10, defensePercent: 25), new SeededRandom(1));
        Assert.Equal(875, defend.AttackDefenseModifierPerMille);
        Assert.Equal(875, defend.FinalDamage);

        DamageResult morale = calculator.Calculate(
            Request(count: 100, min: 10, max: 10, attackPercent: -16), new SeededRandom(1));
        Assert.Equal(920, morale.AttackDefenseModifierPerMille);
        Assert.Equal(920, morale.FinalDamage);
    }

    [Fact]
    public void Calculate_DefendBonusDoesNotTouchResists()
    {
        DamageCalculator calculator = new(CreateRules());

        DamageResult result = calculator.Calculate(
            Request(defensePercent: CreateRules().DefendDefenseBonusPercent), new SeededRandom(1));

        Assert.Equal(0, result.ResistPercent);
    }

    [Fact]
    public void Calculate_ExternalModifierScalesDamageAfterResists()
    {
        DamageResult result = new DamageCalculator(CreateRules())
            .Calculate(Request(modifier: 500), new SeededRandom(1));

        Assert.Equal(15, result.FinalDamage);
    }

    [Fact]
    public void Calculate_ConsumesExactlyTwoRollsInFixedOrder()
    {
        SeededRandom used = new(777);
        SeededRandom reference = new(777);

        DamageResult result = new DamageCalculator(CreateRules())
            .Calculate(Request(min: 1, max: 20, critChance: 50), used);

        Assert.Equal(reference.Next(1, 21), result.UnitRoll);
        Assert.Equal(reference.RollPerMille(), result.CritRoll);
        Assert.Equal(reference.NextUInt64(), used.NextUInt64());
    }

    [Fact]
    public void Calculate_EmptyStack_DoesNotConsumeRandom()
    {
        SeededRandom used = new(5);
        SeededRandom reference = new(5);

        DamageResult result = new DamageCalculator(CreateRules()).Calculate(Request(count: 0), used);

        Assert.Equal(0, result.FinalDamage);
        Assert.Equal(-1, result.CritRoll);
        Assert.Equal(reference.NextUInt64(), used.NextUInt64());
    }

    [Fact]
    public void Calculate_SameSeed_GivesSameSequence()
    {
        DamageCalculator calculator = new(CreateRules());
        SeededRandom first = new(2026);
        SeededRandom second = new(2026);

        for (int i = 0; i < 50; i++)
        {
            DamageRequest request = Request(min: 1, max: 20, critChance: 40);
            Assert.Equal(calculator.Calculate(request, first), calculator.Calculate(request, second));
        }
    }

    [Fact]
    public void Calculate_InvalidRequest_Throws()
    {
        DamageCalculator calculator = new(CreateRules());

        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(Request(count: -1), new SeededRandom(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(Request(min: 5, max: 4), new SeededRandom(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(Request(defensePercent: -101), new SeededRandom(1)));
        Assert.Throws<ArgumentNullException>(() => calculator.Calculate(Request(), null!));
        Assert.Throws<ArgumentNullException>(() => new DamageCalculator(null!));
    }

    [Fact]
    public void Calculate_LogsBattleDebugEvent()
    {
        TestLogger logger = new();

        new DamageCalculator(CreateRules(), logger).Calculate(Request(), new SeededRandom(1));

        Assert.Contains(logger.Events, e => e.Category == "Battle" && e.Fields["finalDamage"] == "30");
    }

    [Theory]
    [InlineData(10, 8)]
    [InlineData(13, 11)]
    [InlineData(15, 12)]
    public void LongDistanceThreshold_ScalesWithFieldLength(int width, int expected)
    {
        BattleRules rules = new(
            width, 9, false, 25, 0, 0, 1, false, true, true, 50, 300, 4000, 75, 300, 80, 25, 25, 75, 1);

        Assert.Equal(expected, rules.LongDistanceThreshold);
    }

    [Theory]
    [InlineData(1, false, 1000)]
    [InlineData(10, false, 1000)]
    [InlineData(11, false, 750)]
    [InlineData(12, false, 750)]
    [InlineData(3, true, 750)]
    [InlineData(11, true, 500)]
    public void GetRangedModifierPerMille_StacksPenaltiesAdditively(int distance, bool obstacle, int expected)
    {
        Assert.Equal(expected, new DamageCalculator(CreateRules()).GetRangedModifierPerMille(distance, obstacle));
    }

    [Fact]
    public void GetShooterMeleeModifierPerMille_AppliesSeventyFivePercentPenalty()
    {
        Assert.Equal(250, new DamageCalculator(CreateRules()).GetShooterMeleeModifierPerMille());
    }

    [Fact]
    public void RangedModifier_FeedsIntoDamage()
    {
        DamageCalculator calculator = new(CreateRules());
        int modifier = calculator.GetRangedModifierPerMille(11, true);

        DamageResult result = calculator.Calculate(Request(modifier: modifier), new SeededRandom(1));

        Assert.Equal(15, result.FinalDamage);
    }

    [Fact]
    public void GetRangedModifierPerMille_NegativeDistance_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DamageCalculator(CreateRules()).GetRangedModifierPerMille(-1, false));
    }
}
