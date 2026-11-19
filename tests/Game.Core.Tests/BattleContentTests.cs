using Game.Core.Battle;
using Game.Core.Content;
using Game.Core.Domain;

namespace Game.Core.Tests;

public class BattleContentTests
{
    private static ContentRegistry Registry =>
        RealContent.MergeWith().Registry
        ?? throw new InvalidOperationException("Контент не слит.");

    [Fact]
    public void CreateRules_FromRealContent_MatchesGdd()
    {
        BattleRules rules = BattleContentAdapter.CreateRules(Registry);

        Assert.Equal(13, rules.FieldWidth);
        Assert.Equal(9, rules.FieldHeight);
        Assert.False(rules.HeroOnBattlefield);
        Assert.Equal(25, rules.DefendDefenseBonusPercent);
        Assert.Equal(0, rules.DefendResistBonusPercent);
        Assert.Equal(0, rules.DefendInitiativePenalty);
        Assert.Equal(1, rules.RetaliationPerRound);
        Assert.False(rules.RetaliationIsAction);
        Assert.Equal(50, rules.AttackDefenseModifierPerPoint);
        Assert.Equal(300, rules.AttackDefenseModifierMin);
        Assert.Equal(4000, rules.AttackDefenseModifierMax);
        Assert.Equal(75, rules.CritChanceCapPercent);
        Assert.Equal(300, rules.CritDamageCapPercent);
        Assert.Equal(80, rules.RangedLongDistanceFieldPercent);
        Assert.Equal(25, rules.RangedLongDistancePenaltyPercent);
        Assert.Equal(25, rules.RangedObstaclePenaltyPercent);
        Assert.Equal(75, rules.RangedMeleePenaltyPercent);
        Assert.Equal(1, rules.AttackMovementCost);
        Assert.Equal(11, rules.LongDistanceThreshold);
    }

    [Fact]
    public void CreateUnits_BuildsAllUnitsWithClassAndDamageType()
    {
        IReadOnlyDictionary<string, UnitDef> units = BattleContentAdapter.CreateUnits(Registry);

        Assert.Equal(15, units.Count);

        UnitDef bone = units["core.unit.ash.bone_servant"];
        Assert.Equal("undead", bone.UnitClass);
        Assert.Equal(DamageType.Physical, bone.DamageType);
        Assert.Equal(100, bone.Resistances.PoisonPercent);

        Assert.Equal(DamageType.Magic, units["core.unit.ash.corpse_whisperer"].DamageType);
        Assert.Equal(DamageType.Gunpowder, units["core.unit.brass.arquebusier"].DamageType);
        Assert.Equal(MovementType.Ranged, units["core.unit.brass.arquebusier"].MovementType);
        Assert.Equal("construct", units["core.unit.brass.brass_golem"].UnitClass);
    }

    [Fact]
    public void ParseTokens_Unknown_Throws()
    {
        Assert.Throws<ArgumentException>(() => BattleContentAdapter.ParseDamageType("holy", "x"));
        Assert.Throws<ArgumentException>(() => BattleContentAdapter.ParseMovementType("swim", "x"));
    }

    [Fact]
    public void BattleRules_InvalidValues_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BattleRules(0, 9, false, 25, 0, 0, 1, false, true, true, 50, 300, 4000, 75, 300, 80, 25, 25, 75, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BattleRules(13, 9, false, 25, 0, 0, 1, false, true, true, 50, 300, 200, 75, 300, 80, 25, 25, 75, 1));
    }
}
