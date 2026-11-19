using Game.Core.Battle;
using Game.Core.Content;
using Game.Core.Domain;
using Game.Core.Heroes;
using Game.Core.Rng;

namespace Game.Core.Tests;

public class BattleResultTests
{
    private static ContentRegistry Registry =>
        RealContent.MergeWith().Registry ?? throw new InvalidOperationException("Контент не слит.");

    private static Stack MakeStack(string id, int initiative, int count, int speed = 12, int damage = 5)
    {
        return new Stack(
            TestContent.CreateUnit(
                id: id,
                initiative: initiative,
                speed: speed,
                damageMin: damage,
                damageMax: damage,
                critChancePercent: 0,
                attack: 10,
                defense: 10),
            count);
    }

    private static BattleState CreateState(IReadOnlyList<Stack> attackers, IReadOnlyList<Stack> defenders)
    {
        BattleRules rules = BattleContentAdapter.CreateRules(Registry);

        BattleState state = BattleState.Create(
            new BattleField(rules.FieldWidth, rules.FieldHeight),
            rules,
            new MoraleSystem(BattleContentAdapter.CreateMoraleRules(Registry)),
            new DamageCalculator(rules),
            new SeededRandom(42),
            new BattleSideSetup(attackers),
            new BattleSideSetup(defenders));

        state.Start();
        return state;
    }

    private static BattleState FinishedDuel(out Stack attacker, out Stack defender)
    {
        attacker = MakeStack("a0", 9, 5);
        defender = MakeStack("d0", 1, 1);
        BattleState state = CreateState(new[] { attacker }, new[] { defender });

        Assert.True(state.Apply(BattleCommand.Attack("A0", "D0")).IsApplied);
        return state;
    }

    private static BattleResult Result(BattleStatus status) =>
        new(status, 3, Array.Empty<BattleStackResult>());

    [Fact]
    public void Result_BeforeBattleEnds_IsUnavailable()
    {
        BattleState state = CreateState(new[] { MakeStack("a0", 9, 5) }, new[] { MakeStack("d0", 1, 5) });

        Assert.Throws<InvalidOperationException>(() => state.CreateResult());
        Assert.Throws<InvalidOperationException>(() => state.CommitResult());
        Assert.Throws<ArgumentException>(() => Result(BattleStatus.Ongoing));
    }

    [Fact]
    public void CreateResult_ReportsLossesVictoryAndKills()
    {
        BattleState state = FinishedDuel(out _, out _);

        BattleResult result = state.CreateResult();

        Assert.Equal(BattleStatus.AttackerWon, result.Status);
        Assert.True(result.IsVictory(BattleSide.Attacker));
        Assert.False(result.IsVictory(BattleSide.Defender));
        Assert.Equal(1, result.Rounds);
        Assert.Equal(0, result.UnitsLost(BattleSide.Attacker));
        Assert.Equal(1, result.UnitsLost(BattleSide.Defender));
        Assert.Equal(1, result.SurvivingStacks(BattleSide.Attacker));
        Assert.Equal(0, result.SurvivingStacks(BattleSide.Defender));

        BattleStackResult winner = result.GetStacks(BattleSide.Attacker).Single();
        Assert.Equal(1, winner.KilledStacksGained);
        Assert.True(winner.Survived);
    }

    [Fact]
    public void CommitResult_AppliesCasualtiesVeterancyAndRemovesEmptyStacks()
    {
        Army attackerArmy = new(1000);
        Army defenderArmy = new(1000);
        Stack attacker = MakeStack("a0", 9, 5);
        Stack defender = MakeStack("d0", 1, 1);
        Assert.True(attackerArmy.TryAdd(attacker));
        Assert.True(defenderArmy.TryAdd(defender));
        BattleState state = CreateState(attackerArmy.Slots, defenderArmy.Slots);

        Assert.True(state.Apply(BattleCommand.Attack("A0", "D0")).IsApplied);
        BattleResult result = state.CommitResult(attackerArmy, defenderArmy);

        Assert.Equal(BattleStatus.AttackerWon, result.Status);
        Assert.Single(attackerArmy.Slots);
        Assert.Equal(5, attackerArmy.Slots[0].Count);
        Assert.Equal(1, attackerArmy.Slots[0].KilledStacks);
        Assert.Empty(defenderArmy.Slots);
    }

    [Fact]
    public void CommitResult_PartialLosses_ReduceSourceCount()
    {
        Stack attacker = MakeStack("a0", 9, 5);
        Stack defender = MakeStack("d0", 1, 5);
        BattleState state = CreateState(new[] { attacker }, new[] { defender });

        state.FindUnit("A0")!.ApplyDamage(18);
        state.FindUnit("D0")!.ApplyDamage(10_000);
        Assert.True(state.Apply(BattleCommand.Defend("A0")).IsApplied);

        BattleResult result = state.CommitResult();

        Assert.Equal(BattleStatus.AttackerWon, result.Status);
        Assert.Equal(4, attacker.Count);
        Assert.Equal(0, defender.Count);
        Assert.Equal(1, result.UnitsLost(BattleSide.Attacker));
    }

    [Fact]
    public void CommitResult_TwiceOrAfterExternalChange_Throws()
    {
        BattleState state = FinishedDuel(out _, out _);

        state.CommitResult();

        Assert.Throws<InvalidOperationException>(() => state.CommitResult());

        Stack attacker = MakeStack("a0", 9, 5);
        Stack defender = MakeStack("d0", 1, 1);
        BattleState other = CreateState(new[] { attacker }, new[] { defender });
        other.Apply(BattleCommand.Attack("A0", "D0"));
        attacker.TakeCasualties(1);

        Assert.Throws<InvalidOperationException>(() => other.CommitResult());
    }

    [Fact]
    public void Aftermath_Victory_ConsumesNoRandom()
    {
        SeededRandom used = new(5);
        SeededRandom reference = new(5);

        BattleAftermath aftermath = BattleAftermathResolver.Resolve(
            Result(BattleStatus.AttackerWon), BattleSide.Attacker, true, 5, 3, false, false, used);

        Assert.Equal(BattleAftermathKind.Victory, aftermath.Kind);
        Assert.Equal(reference.NextUInt64(), used.NextUInt64());
    }

    [Fact]
    public void Aftermath_HerolessLoser_ArmyCeasesToExist()
    {
        SeededRandom used = new(5);
        SeededRandom reference = new(5);

        BattleAftermath aftermath = BattleAftermathResolver.Resolve(
            Result(BattleStatus.AttackerWon), BattleSide.Defender, false, 0, 0, false, false, used);

        Assert.Equal(BattleAftermathKind.ArmyDestroyed, aftermath.Kind);
        Assert.True(aftermath.ArmyDestroyed);
        Assert.Equal(-1, aftermath.RollPerMille);
        Assert.Equal(reference.NextUInt64(), used.NextUInt64());
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(3UL)]
    [InlineData(4UL)]
    [InlineData(5UL)]
    public void Aftermath_HeroLoser_UsesStageTwoEscapeFormula(ulong seed)
    {
        SeededRandom used = new(seed);
        SeededRandom reference = new(seed);
        int roll = reference.RollPerMille();
        EscapeResult expected = EscapeRules.TryEscape(5, 3, false, roll);

        BattleAftermath aftermath = BattleAftermathResolver.Resolve(
            Result(BattleStatus.AttackerWon), BattleSide.Defender, true, 5, 3, false, false, used);

        Assert.Equal(roll, aftermath.RollPerMille);
        Assert.Equal(EscapeRules.EscapeChancePercent(5, 3), aftermath.EscapeChancePercent);
        Assert.Equal(
            expected == EscapeResult.Escaped ? BattleAftermathKind.HeroEscaped : BattleAftermathKind.HeroCaptured,
            aftermath.Kind);
        Assert.Equal(reference.NextUInt64(), used.NextUInt64());
    }

    [Fact]
    public void Aftermath_RansomedHero_KeepsArmyWithoutRoll()
    {
        SeededRandom used = new(7);
        SeededRandom reference = new(7);

        BattleAftermath aftermath = BattleAftermathResolver.Resolve(
            Result(BattleStatus.AttackerWon), BattleSide.Defender, true, 5, 3, false, true, used);

        Assert.Equal(BattleAftermathKind.HeroRansomed, aftermath.Kind);
        Assert.False(aftermath.ArmyDestroyed);
        Assert.Equal(reference.NextUInt64(), used.NextUInt64());
    }

    [Fact]
    public void Aftermath_LostHeroWithoutRansom_DestroysArmy()
    {
        BattleAftermath aftermath = BattleAftermathResolver.Resolve(
            Result(BattleStatus.AttackerWon), BattleSide.Defender, true, 5, 3, false, false, new SeededRandom(1));

        Assert.True(aftermath.ArmyDestroyed);
        Assert.NotEqual(BattleAftermathKind.HeroRansomed, aftermath.Kind);
    }

    [Fact]
    public void Aftermath_SiegeDefender_CannotEscapeAndRollsNothing()
    {
        SeededRandom used = new(7);
        SeededRandom reference = new(7);

        BattleAftermath aftermath = BattleAftermathResolver.Resolve(
            Result(BattleStatus.AttackerWon), BattleSide.Defender, true, 5, 3, true, false, used);

        Assert.Equal(BattleAftermathKind.EscapeForbidden, aftermath.Kind);
        Assert.Equal(-1, aftermath.RollPerMille);
        Assert.Equal(reference.NextUInt64(), used.NextUInt64());
    }

    [Fact]
    public void Aftermath_Draw_IsDefeatForBothSides()
    {
        BattleResult draw = Result(BattleStatus.Draw);

        Assert.False(draw.IsVictory(BattleSide.Attacker));
        Assert.False(draw.IsVictory(BattleSide.Defender));

        BattleAftermath attacker = BattleAftermathResolver.Resolve(draw, BattleSide.Attacker, false, 0, 0, false, false, new SeededRandom(1));
        BattleAftermath defender = BattleAftermathResolver.Resolve(draw, BattleSide.Defender, false, 0, 0, false, false, new SeededRandom(1));

        Assert.Equal(BattleAftermathKind.ArmyDestroyed, attacker.Kind);
        Assert.Equal(BattleAftermathKind.ArmyDestroyed, defender.Kind);
    }

    [Fact]
    public void Aftermath_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => BattleAftermathResolver.Resolve(
            null!, BattleSide.Attacker, false, 0, 0, false, false, new SeededRandom(1)));
        Assert.Throws<ArgumentNullException>(() => BattleAftermathResolver.Resolve(
            Result(BattleStatus.AttackerWon), BattleSide.Attacker, false, 0, 0, false, false, null!));
    }
}
