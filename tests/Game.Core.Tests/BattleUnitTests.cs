using Game.Core.Battle;
using Game.Core.Domain;

namespace Game.Core.Tests;

public class BattleUnitTests
{
    private static BattleUnit Create(int count = 5, int health = 10, int killed = 0, int attack = 5)
    {
        UnitDef unit = TestContent.CreateUnit(health: health, attack: attack);
        return new BattleUnit("A0", BattleSide.Attacker, 0, unit, count, Hex.Zero, killed);
    }

    [Fact]
    public void ApplyDamage_CarriesRemainderToNextUnit()
    {
        BattleUnit unit = Create(count: 5, health: 10);

        CasualtyResult result = unit.ApplyDamage(25);

        Assert.Equal(25, result.DamageDealt);
        Assert.Equal(0, result.Overkill);
        Assert.Equal(2, result.UnitsKilled);
        Assert.Equal(3, unit.Count);
        Assert.Equal(5, unit.TopUnitHealth);
        Assert.Equal(25, unit.TotalHealth);
        Assert.True(unit.IsAlive);
    }

    [Fact]
    public void ApplyDamage_AccumulatesWoundsOnTopUnit()
    {
        BattleUnit unit = Create(count: 3, health: 10);

        unit.ApplyDamage(4);
        Assert.Equal(3, unit.Count);
        Assert.Equal(6, unit.TopUnitHealth);

        CasualtyResult second = unit.ApplyDamage(6);
        Assert.Equal(1, second.UnitsKilled);
        Assert.Equal(2, unit.Count);
        Assert.Equal(10, unit.TopUnitHealth);
        Assert.Equal(20, unit.TotalHealth);
    }

    [Fact]
    public void ApplyDamage_ExactTotal_DestroysStackWithoutOverkill()
    {
        BattleUnit unit = Create(count: 2, health: 10);

        CasualtyResult result = unit.ApplyDamage(20);

        Assert.True(result.StackDestroyed);
        Assert.Equal(0, result.Overkill);
        Assert.Equal(2, result.UnitsKilled);
        Assert.False(unit.IsAlive);
        Assert.Equal(0, unit.TotalHealth);
    }

    [Fact]
    public void ApplyDamage_Overkill_IsReported()
    {
        BattleUnit unit = Create(count: 2, health: 10);

        CasualtyResult result = unit.ApplyDamage(50);

        Assert.Equal(20, result.DamageDealt);
        Assert.Equal(30, result.Overkill);
        Assert.Equal(0, unit.Count);
    }

    [Fact]
    public void ApplyDamage_ZeroDamage_ChangesNothing()
    {
        BattleUnit unit = Create(count: 3, health: 10);
        unit.ApplyDamage(4);

        CasualtyResult result = unit.ApplyDamage(0);

        Assert.Equal(0, result.UnitsKilled);
        Assert.Equal(3, unit.Count);
        Assert.Equal(6, unit.TopUnitHealth);
    }

    [Fact]
    public void ApplyDamage_Invalid_Throws()
    {
        BattleUnit unit = Create();

        Assert.Throws<ArgumentOutOfRangeException>(() => unit.ApplyDamage(-1));

        unit.ApplyDamage(1000);
        Assert.Throws<InvalidOperationException>(() => unit.ApplyDamage(1));
    }

    [Fact]
    public void Veterancy_ThresholdsAndBonuses_FollowKills()
    {
        BattleUnit unit = Create(attack: 5);
        TestLogger logger = new();

        Assert.Equal(1, unit.VeterancyRank);
        Assert.Equal(5, unit.Attack);

        Assert.False(unit.RegisterKills(1, logger));
        Assert.Equal(1, unit.VeterancyRank);

        Assert.True(unit.RegisterKills(1, logger));
        Assert.Equal(2, unit.VeterancyRank);
        Assert.Equal(7, unit.Attack);

        Assert.True(unit.RegisterKills(3, logger));
        Assert.Equal(3, unit.VeterancyRank);

        Assert.Contains(
            logger.Events,
            e => e.Category == "Battle" && e.Fields["oldRank"] == "1" && e.Fields["newRank"] == "2");
    }

    [Fact]
    public void Veterancy_InitialKills_SetRank()
    {
        BattleUnit unit = Create(killed: 18);

        Assert.Equal(5, unit.VeterancyRank);
        Assert.Equal(5 + VeterancyRules.AttackBonus(5), unit.Attack);
        Assert.Equal(unit.Unit.Initiative, unit.Initiative);
    }

    [Fact]
    public void Flags_RoundAndTurnLifecycle()
    {
        BattleUnit unit = Create();

        unit.SetDefending();
        unit.MarkWaited();
        unit.SpendRetaliation();
        unit.RegisterExtraAction();

        Assert.True(unit.IsDefending);

        unit.BeginRound();
        Assert.True(unit.WaitedLastRound);
        Assert.False(unit.WaitedThisRound);
        Assert.Equal(0, unit.RetaliationsUsedThisRound);
        Assert.Equal(0, unit.ExtraActionsUsedThisRound);
        Assert.True(unit.IsDefending);

        unit.BeginTurn();
        Assert.False(unit.IsDefending);

        unit.BeginRound();
        Assert.False(unit.WaitedLastRound);
    }

    [Fact]
    public void MovementPool_ResetsOnTurnStartAndIsSpentDownToZero()
    {
        UnitDef def = TestContent.CreateUnit(speed: 4);
        BattleUnit unit = new("A0", BattleSide.Attacker, 0, def, 3, Hex.Zero);

        Assert.Equal(0, unit.MovementPointsRemaining);

        unit.BeginTurn();
        Assert.Equal(4, unit.MovementPointsRemaining);

        unit.SpendMovement(3);
        unit.SpendMovement(1);
        Assert.Equal(0, unit.MovementPointsRemaining);
        Assert.Throws<ArgumentOutOfRangeException>(() => unit.SpendMovement(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => unit.SpendMovement(-1));

        unit.BeginTurn();
        Assert.Equal(4, unit.MovementPointsRemaining);
    }

    [Fact]
    public void Heal_RestoresHealthUpToInitialMaximumAndRevivesUnits()
    {
        BattleUnit unit = Create(count: 5, health: 10);
        unit.ApplyDamage(18);
        Assert.Equal(32, unit.TotalHealth);
        Assert.Equal(4, unit.Count);

        Assert.Equal(8, unit.Heal(8));
        Assert.Equal(40, unit.TotalHealth);
        Assert.Equal(4, unit.Count);

        Assert.Equal(10, unit.Heal(100));
        Assert.Equal(50, unit.TotalHealth);
        Assert.Equal(5, unit.Count);
        Assert.Equal(10, unit.TopUnitHealth);
        Assert.Equal(0, unit.Heal(5));
    }

    [Fact]
    public void Heal_Invalid_Throws()
    {
        BattleUnit unit = Create();

        Assert.Throws<ArgumentOutOfRangeException>(() => unit.Heal(-1));

        unit.ApplyDamage(1000);
        Assert.Throws<InvalidOperationException>(() => unit.Heal(1));
    }

    [Fact]
    public void Effects_ModifyStatsAndExpireAfterDuration()
    {
        BattleUnit unit = Create(attack: 5);

        unit.AddEffect(BattleEffectTypes.AttackModifier, -2, 2, "test");
        unit.AddEffect(BattleEffectTypes.AttackModifier, 4, 1, "test");
        Assert.Equal(7, unit.Attack);
        Assert.True(unit.HasEffect(BattleEffectTypes.AttackModifier));

        Assert.Equal(1, unit.TickEffects());
        Assert.Equal(3, unit.Attack);

        Assert.Equal(1, unit.TickEffects());
        Assert.Equal(5, unit.Attack);
        Assert.False(unit.HasEffect(BattleEffectTypes.AttackModifier));
        Assert.Throws<ArgumentOutOfRangeException>(() => unit.AddEffect("x", 1, 0, "test"));
    }

    [Fact]
    public void Attack_NeverDropsBelowZero_AndHeroBonusesAreAdded()
    {
        BattleUnit unit = Create(attack: 5);

        unit.AddEffect(BattleEffectTypes.AttackModifier, -50, 1, "test");
        Assert.Equal(0, unit.Attack);

        unit.SetHeroBonuses(3, 2, 1);
        Assert.Equal(0, unit.Attack);
        Assert.Equal(unit.Unit.Defense + 2, unit.Defense);
        Assert.Equal(unit.Unit.Initiative + 1, unit.Initiative);
    }

    [Fact]
    public void Cooldowns_CountDownByRounds()
    {
        BattleUnit unit = Create();

        unit.StartCooldown("a", 2);
        unit.StartCooldown("b", 0);
        Assert.Equal(2, unit.GetCooldown("a"));
        Assert.Equal(0, unit.GetCooldown("b"));

        unit.TickCooldowns();
        Assert.Equal(1, unit.GetCooldown("a"));
        unit.TickCooldowns();
        Assert.Equal(0, unit.GetCooldown("a"));
    }

    [Fact]
    public void Constructor_InvalidInput_Throws()
    {
        UnitDef def = TestContent.CreateUnit();

        Assert.Throws<ArgumentException>(() => new BattleUnit(" ", BattleSide.Attacker, 0, def, 1, Hex.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BattleUnit("A", BattleSide.Attacker, 0, def, 0, Hex.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BattleUnit("A", BattleSide.Attacker, -1, def, 1, Hex.Zero));
        Assert.Throws<ArgumentNullException>(() => new BattleUnit("A", BattleSide.Attacker, 0, null!, 1, Hex.Zero));
    }
}
