using Game.Core.Battle;
using Game.Core.Content;
using Game.Core.Domain;
using Game.Core.Rng;

namespace Game.Core.Tests;

public class BattleStateTests
{
    private static ContentRegistry Registry =>
        RealContent.MergeWith().Registry ?? throw new InvalidOperationException("Контент не слит.");

    private static MoraleRules ForcedMorale() =>
        new(
            -1, 1, 100, 16, 1, new[] { "undead" },
            new[]
            {
                new MoraleLevelRule(-1, "k.minus", 100, -16),
                new MoraleLevelRule(0, "k.zero", 0, 0),
                new MoraleLevelRule(1, "k.plus", 100, 16)
            },
            1, -1, -2, 1, 1, -1, 1);

    private static Stack MakeStack(
        string id,
        int initiative,
        int speed = 3,
        string faction = "test.faction",
        MovementType movement = MovementType.Infantry,
        int count = 5,
        int damage = 5,
        int attack = 10,
        int defense = 10)
    {
        return new Stack(
            TestContent.CreateUnit(
                id: id,
                initiative: initiative,
                speed: speed,
                factionId: faction,
                movementType: movement,
                damageMin: damage,
                damageMax: damage,
                critChancePercent: 0,
                attack: attack,
                defense: defense),
            count);
    }

    private static BattleState Create(
        IReadOnlyList<Stack> attackers,
        IReadOnlyList<Stack> defenders,
        MoraleRules? moraleRules = null,
        string? attackerHeroFaction = null,
        int obstacleColumn = -1,
        int obstacleRow = -1,
        TestLogger? logger = null,
        bool start = true)
    {
        BattleRules rules = BattleContentAdapter.CreateRules(Registry);
        BattleField probe = new(rules.FieldWidth, rules.FieldHeight);
        List<Hex> obstacles = new();

        if (obstacleColumn >= 0)
        {
            obstacles.Add(probe.Grid.HexAt(obstacleColumn, obstacleRow));
        }

        BattleField field = new(rules.FieldWidth, rules.FieldHeight, obstacles);
        MoraleSystem morale = new(moraleRules ?? BattleContentAdapter.CreateMoraleRules(Registry));

        BattleState state = BattleState.Create(
            field,
            rules,
            morale,
            new DamageCalculator(rules),
            new SeededRandom(42),
            new BattleSideSetup(attackers, attackerHeroFaction),
            new BattleSideSetup(defenders),
            logger);

        if (start)
        {
            state.Start();
        }

        return state;
    }

    private static BattleState Duel(int attackerInitiative = 5, int defenderInitiative = 9)
    {
        return Create(
            new[] { MakeStack("a0", attackerInitiative) },
            new[] { MakeStack("d0", defenderInitiative) });
    }

    [Fact]
    public void Create_DeploysSlotsOnRowsAndEdgeColumns()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 5), MakeStack("a1", 5), MakeStack("a2", 5) },
            new[] { MakeStack("d0", 5), MakeStack("d1", 5) },
            start: false);
        BattleField field = state.Field;

        Assert.Equal(field.Grid.HexAt(0, 0), state.FindUnit("A0")!.Position);
        Assert.Equal(field.Grid.HexAt(0, 2), state.FindUnit("A2")!.Position);
        Assert.Equal(field.Grid.HexAt(12, 0), state.FindUnit("D0")!.Position);
        Assert.Equal(field.Grid.HexAt(12, 1), state.FindUnit("D1")!.Position);
        Assert.Equal(BattleSide.Defender, state.FindUnit("D1")!.Side);
        Assert.Null(state.ActiveUnit);
    }

    [Fact]
    public void Create_ExtraSlotsUseSecondColumn()
    {
        Stack[] many = Enumerable.Range(0, 10).Select(i => MakeStack($"a{i}", 5)).ToArray();

        BattleState state = Create(many, new[] { MakeStack("d0", 5) }, start: false);

        Assert.Equal(state.Field.Grid.HexAt(1, 0), state.FindUnit("A9")!.Position);
    }

    [Fact]
    public void Create_InvalidSides_Throw()
    {
        Stack[] tooMany = Enumerable.Range(0, 19).Select(i => MakeStack($"a{i}", 5)).ToArray();

        Assert.Throws<ArgumentException>(() => Create(Array.Empty<Stack>(), new[] { MakeStack("d0", 5) }, start: false));
        Assert.Throws<ArgumentException>(() => Create(tooMany, new[] { MakeStack("d0", 5) }, start: false));
        Assert.Throws<InvalidOperationException>(() =>
            Create(new[] { MakeStack("a0", 5) }, new[] { MakeStack("d0", 5) }, obstacleColumn: 0, obstacleRow: 0, start: false));
    }

    [Fact]
    public void Start_BuildsQueueAndActivatesFastestStack()
    {
        BattleState state = Duel();

        Assert.Equal(1, state.Round);
        Assert.Equal("D0", state.ActiveUnit!.Id);
        Assert.Equal(BattleStatus.Ongoing, state.Status);
        Assert.Throws<InvalidOperationException>(() => state.Start());
    }

    [Fact]
    public void Apply_BeforeStart_Throws()
    {
        BattleState state = Create(new[] { MakeStack("a0", 5) }, new[] { MakeStack("d0", 5) }, start: false);

        Assert.Throws<InvalidOperationException>(() => state.Apply(BattleCommand.Defend("A0")));
    }

    [Fact]
    public void Move_WithinSpeed_SpendsMovementAndKeepsTurn()
    {
        BattleState state = Duel();
        BattleUnit active = state.ActiveUnit!;
        Hex target = state.Field.Grid.HexAt(10, 0);

        BattleCommandResult result = state.Apply(BattleCommand.Move(active.Id, target));

        Assert.True(result.IsApplied, result.ToString());
        Assert.Equal(target, active.Position);
        Assert.Equal(1, active.MovementPointsRemaining);
        Assert.Equal("D0", state.ActiveUnit!.Id);
    }

    [Fact]
    public void Move_PoolIsSpentPiecemealAndExhausted()
    {
        BattleState state = Duel();
        BattleUnit active = state.ActiveUnit!;
        BattleField field = state.Field;

        Assert.True(state.Apply(BattleCommand.Move(active.Id, field.Grid.HexAt(10, 0))).IsApplied);
        Assert.True(state.Apply(BattleCommand.Move(active.Id, field.Grid.HexAt(9, 0))).IsApplied);
        Assert.Equal(0, active.MovementPointsRemaining);
        Assert.Equal(
            BattleCommandOutcome.NoPath,
            state.Apply(BattleCommand.Move(active.Id, field.Grid.HexAt(8, 0))).Outcome);
        Assert.Equal("D0", state.ActiveUnit!.Id);
    }

    [Fact]
    public void Move_ThenWait_IsForbidden()
    {
        BattleState state = Duel();
        string id = state.ActiveUnit!.Id;

        state.Apply(BattleCommand.Move(id, state.Field.Grid.HexAt(11, 0)));

        Assert.Equal(BattleCommandOutcome.WaitNotAllowed, state.Apply(BattleCommand.Wait(id)).Outcome);
    }

    [Fact]
    public void NextTurn_RestoresMovementPool()
    {
        BattleState state = Duel();
        BattleUnit first = state.ActiveUnit!;

        state.Apply(BattleCommand.Move(first.Id, state.Field.Grid.HexAt(10, 0)));
        state.Apply(BattleCommand.Defend(first.Id));
        state.Apply(BattleCommand.Defend("A0"));

        Assert.Equal(2, state.Round);
        Assert.Equal(first.Id, state.ActiveUnit!.Id);
        Assert.Equal(first.Unit.Speed, first.MovementPointsRemaining);
    }

    [Fact]
    public void Move_BeyondSpeed_ReturnsNoPath()
    {
        BattleState state = Duel();
        BattleUnit active = state.ActiveUnit!;
        Hex start = active.Position;

        BattleCommandResult result = state.Apply(BattleCommand.Move(active.Id, state.Field.Grid.HexAt(7, 0)));

        Assert.Equal(BattleCommandOutcome.NoPath, result.Outcome);
        Assert.Equal(start, active.Position);
        Assert.Equal(active.Id, state.ActiveUnit!.Id);
    }

    [Fact]
    public void Move_ToOccupiedObstacleOrOutsideField_IsRejected()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 5), MakeStack("a1", 5) },
            new[] { MakeStack("d0", 9), MakeStack("d1", 9) },
            obstacleColumn: 11,
            obstacleRow: 0);
        string activeId = state.ActiveUnit!.Id;

        Assert.Equal(
            BattleCommandOutcome.TargetBlocked,
            state.Apply(BattleCommand.Move(activeId, state.Field.Grid.HexAt(12, 1))).Outcome);
        Assert.Equal(
            BattleCommandOutcome.TargetBlocked,
            state.Apply(BattleCommand.Move(activeId, state.Field.Grid.HexAt(11, 0))).Outcome);
        Assert.Equal(
            BattleCommandOutcome.TargetOutOfField,
            state.Apply(BattleCommand.Move(activeId, new Hex(500, 500))).Outcome);
    }

    [Fact]
    public void Command_ForNonActiveUnit_IsRejected()
    {
        BattleState state = Duel();

        Assert.Equal(BattleCommandOutcome.NotActiveUnit, state.Apply(BattleCommand.Defend("A0")).Outcome);
        Assert.Equal(BattleCommandOutcome.NotActiveUnit, state.Apply(BattleCommand.Wait("missing")).Outcome);
    }

    [Fact]
    public void ComputeReachable_WalksAroundObstacle_FlyerCrossesIt()
    {
        BattleState walker = Create(
            new[] { MakeStack("a0", 5, speed: 5) },
            new[] { MakeStack("d0", 1) },
            obstacleColumn: 1,
            obstacleRow: 4);
        BattleUnit walkerUnit = walker.FindUnit("A0")!;
        walkerUnit.MoveTo(walker.Field.Grid.HexAt(0, 4));
        Hex beyond = walker.Field.Grid.HexAt(2, 4);

        IReadOnlyDictionary<Hex, int> walking = walker.ComputeReachable(walkerUnit, 5);

        Assert.Equal(3, walking[beyond]);
        Assert.False(walking.ContainsKey(walker.Field.Grid.HexAt(1, 4)));

        BattleState flying = Create(
            new[] { MakeStack("a0", 5, speed: 5, movement: MovementType.Flyer) },
            new[] { MakeStack("d0", 1) },
            obstacleColumn: 1,
            obstacleRow: 4);
        BattleUnit flyer = flying.FindUnit("A0")!;
        flyer.MoveTo(flying.Field.Grid.HexAt(0, 4));

        IReadOnlyDictionary<Hex, int> flightPaths = flying.ComputeReachable(flyer, 5);

        Assert.Equal(2, flightPaths[flying.Field.Grid.HexAt(2, 4)]);
        Assert.False(flightPaths.ContainsKey(flying.Field.Grid.HexAt(1, 4)));
    }

    [Fact]
    public void Wait_PassesTurnOnceThenIsForbidden()
    {
        BattleState state = Duel();

        Assert.True(state.Apply(BattleCommand.Wait("D0")).IsApplied);
        Assert.Equal("A0", state.ActiveUnit!.Id);

        Assert.True(state.Apply(BattleCommand.Defend("A0")).IsApplied);
        Assert.Equal("D0", state.ActiveUnit!.Id);
        Assert.Equal(BattleCommandOutcome.WaitNotAllowed, state.Apply(BattleCommand.Wait("D0")).Outcome);
    }

    [Fact]
    public void Wait_UnitActsAfterNonWaitingInNextRound()
    {
        BattleState state = Duel();

        state.Apply(BattleCommand.Wait("D0"));
        state.Apply(BattleCommand.Defend("A0"));
        state.Apply(BattleCommand.Defend("D0"));

        Assert.Equal(2, state.Round);
        Assert.Equal("A0", state.ActiveUnit!.Id);
    }

    [Fact]
    public void Defend_LastsUntilStacksNextTurn()
    {
        BattleState state = Duel();
        BattleUnit defender = state.FindUnit("D0")!;

        state.Apply(BattleCommand.Defend("D0"));
        Assert.True(defender.IsDefending);

        state.Apply(BattleCommand.Defend("A0"));
        Assert.Equal(2, state.Round);
        Assert.Equal("D0", state.ActiveUnit!.Id);
        Assert.False(defender.IsDefending);
    }

    [Fact]
    public void Battle_EndsWhenSideIsDestroyed()
    {
        BattleState state = Duel();
        state.FindUnit("A0")!.ApplyDamage(10_000);

        BattleCommandResult applied = state.Apply(BattleCommand.Defend("D0"));

        Assert.True(applied.IsApplied);
        Assert.Equal(BattleStatus.DefenderWon, state.Status);
        Assert.True(state.IsOver);
        Assert.Null(state.ActiveUnit);
        Assert.Equal(BattleCommandOutcome.BattleOver, state.Apply(BattleCommand.Defend("A0")).Outcome);
    }

    [Fact]
    public void Morale_PositiveLevel_AppendsExtraActionToQueue()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 9), MakeStack("a1", 8) },
            new[] { MakeStack("d0", 1) },
            ForcedMorale(),
            attackerHeroFaction: "test.faction");

        Assert.Equal(1, state.GetMoraleLevel(BattleSide.Attacker));
        Assert.Equal("A0", state.ActiveUnit!.Id);
        Assert.Contains(state.Queue.GetRemaining(), e => e.IsExtraAction && e.Unit.Id == "A0");
        Assert.Equal(1, state.FindUnit("A0")!.ExtraActionsUsedThisRound);
    }

    [Fact]
    public void Morale_NegativeLevel_SkipsTurn()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 9, faction: "f1"), MakeStack("a1", 8, faction: "f2") },
            new[] { MakeStack("d0", 1, faction: "f3") },
            ForcedMorale(),
            attackerHeroFaction: "f1");

        Assert.Equal(-1, state.GetMoraleLevel(BattleSide.Attacker));
        Assert.Equal("D0", state.ActiveUnit!.Id);

        state.Apply(BattleCommand.Defend("D0"));

        Assert.Equal(2, state.Round);
        Assert.Equal("D0", state.ActiveUnit!.Id);
    }

    [Fact]
    public void Morale_AllyDeathPenalty_LastsConfiguredRounds()
    {
        BattleState state = Duel();

        Assert.Equal(0, state.GetMoraleLevel(BattleSide.Defender));

        state.NotifyStackDestroyed(BattleSide.Defender);
        state.NotifyStackDestroyed(BattleSide.Defender);
        Assert.Equal(-2, state.GetMoraleLevel(BattleSide.Defender));
        Assert.Equal(0, state.GetMoraleLevel(BattleSide.Attacker));

        state.Apply(BattleCommand.Defend("D0"));
        state.Apply(BattleCommand.Defend("A0"));

        Assert.Equal(2, state.Round);
        Assert.Equal(0, state.GetMoraleLevel(BattleSide.Defender));
    }

    [Fact]
    public void Battle_LogsStartAndRejectedCommands()
    {
        TestLogger logger = new();
        BattleState state = Create(new[] { MakeStack("a0", 5) }, new[] { MakeStack("d0", 9) }, logger: logger);

        state.Apply(BattleCommand.Defend("A0"));

        Assert.Contains(logger.Events, e => e.Category == "Battle" && e.Message == "Бой начат.");
        Assert.Contains(logger.Events, e => e.Category == "Battle" && e.Fields.TryGetValue("outcome", out string? v) && v == "NotActiveUnit");
    }

    private static BattleState AttackScene(
        Stack attacker,
        Stack defender,
        int obstacleColumn = -1,
        int obstacleRow = -1)
    {
        return Create(
            new[] { attacker },
            new[] { defender },
            obstacleColumn: obstacleColumn,
            obstacleRow: obstacleRow);
    }

    [Fact]
    public void Attack_CombinedApproachAndStrike_EndsTurnAndTriggersRetaliation()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 9, speed: 4) },
            new[] { MakeStack("d0", 5) },
            start: false);
        BattleField field = state.Field;
        state.FindUnit("D0")!.MoveTo(field.Grid.HexAt(3, 0));
        state.Start();
        BattleUnit attacker = state.FindUnit("A0")!;
        BattleUnit defender = state.FindUnit("D0")!;

        BattleCommandResult result = state.Apply(BattleCommand.Attack("A0", "D0"));

        Assert.True(result.IsApplied, result.ToString());
        Assert.Equal(1, field.Distance(attacker.Position, defender.Position));
        Assert.Equal(2, attacker.MovementPointsRemaining);
        Assert.Equal(3, defender.Count);
        Assert.Equal(5, defender.TopUnitHealth);
        Assert.Equal(1, defender.RetaliationsUsedThisRound);
        Assert.Equal(4, attacker.Count);
        Assert.Equal(5, attacker.TopUnitHealth);
        Assert.Equal("D0", state.ActiveUnit!.Id);
    }

    [Fact]
    public void Attack_StepByStepMoveThenStrike_MatchesCombined()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 9, speed: 4) },
            new[] { MakeStack("d0", 5) },
            start: false);
        BattleField field = state.Field;
        state.FindUnit("D0")!.MoveTo(field.Grid.HexAt(3, 0));
        state.Start();
        BattleUnit defender = state.FindUnit("D0")!;

        Assert.True(state.Apply(BattleCommand.Move("A0", field.Grid.HexAt(2, 0))).IsApplied);
        Assert.Equal("A0", state.ActiveUnit!.Id);
        Assert.True(state.Apply(BattleCommand.Attack("A0", "D0")).IsApplied);

        Assert.Equal(3, defender.Count);
        Assert.Equal("D0", state.ActiveUnit!.Id);
    }

    [Fact]
    public void Attack_TargetBeyondSpeedMinusOne_IsOutOfReach()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 9, speed: 3) },
            new[] { MakeStack("d0", 5) },
            start: false);
        state.FindUnit("D0")!.MoveTo(state.Field.Grid.HexAt(4, 0));
        state.Start();
        BattleUnit attacker = state.FindUnit("A0")!;
        Hex before = attacker.Position;

        BattleCommandResult result = state.Apply(BattleCommand.Attack("A0", "D0"));

        Assert.Equal(BattleCommandOutcome.OutOfReach, result.Outcome);
        Assert.Equal(before, attacker.Position);
        Assert.Equal(3, attacker.MovementPointsRemaining);
        Assert.Equal("A0", state.ActiveUnit!.Id);
    }

    [Fact]
    public void Attack_WithNoMovementLeft_IsRejectedEvenWhenAdjacent()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 9, speed: 2) },
            new[] { MakeStack("d0", 5) },
            start: false);
        state.FindUnit("D0")!.MoveTo(state.Field.Grid.HexAt(3, 0));
        state.Start();

        Assert.True(state.Apply(BattleCommand.Move("A0", state.Field.Grid.HexAt(2, 0))).IsApplied);
        Assert.Equal(0, state.FindUnit("A0")!.MovementPointsRemaining);

        BattleCommandResult result = state.Apply(BattleCommand.Attack("A0", "D0"));

        Assert.Equal(BattleCommandOutcome.NotEnoughMovement, result.Outcome);
        Assert.Equal(50, state.FindUnit("D0")!.TotalHealth);
        Assert.Equal("A0", state.ActiveUnit!.Id);
    }

    [Fact]
    public void Attack_ShooterWithNoMovementLeft_CannotShoot()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 9, speed: 1, movement: MovementType.Ranged) },
            new[] { MakeStack("d0", 5) });

        Assert.True(state.Apply(BattleCommand.Move("A0", state.Field.Grid.HexAt(1, 0))).IsApplied);

        Assert.Equal(
            BattleCommandOutcome.NotEnoughMovement,
            state.Apply(BattleCommand.Attack("A0", "D0")).Outcome);
    }

    [Fact]
    public void Attack_InvalidTargets_AreRejected()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 9), MakeStack("a1", 8) },
            new[] { MakeStack("d0", 1) });

        Assert.Equal(BattleCommandOutcome.InvalidTarget, state.Apply(BattleCommand.Attack("A0", "A1")).Outcome);
        Assert.Equal(BattleCommandOutcome.InvalidTarget, state.Apply(BattleCommand.Attack("A0", "missing")).Outcome);

        state.FindUnit("D0")!.ApplyDamage(10_000);
        Assert.Equal(BattleCommandOutcome.InvalidTarget, state.Apply(BattleCommand.Attack("A0", "D0")).Outcome);
    }

    [Fact]
    public void Attack_Ranged_AtLongDistance_HasNoRetaliationAndKeepsPosition()
    {
        BattleState state = AttackScene(
            MakeStack("a0", 9, movement: MovementType.Ranged),
            MakeStack("d0", 5));
        BattleUnit attacker = state.FindUnit("A0")!;
        BattleUnit defender = state.FindUnit("D0")!;
        Hex before = attacker.Position;

        Assert.True(state.Apply(BattleCommand.Attack("A0", "D0")).IsApplied);

        Assert.Equal(before, attacker.Position);
        Assert.Equal(31, defender.TotalHealth);
        Assert.Equal(0, defender.RetaliationsUsedThisRound);
        Assert.Equal(50, attacker.TotalHealth);
    }

    [Fact]
    public void Attack_Ranged_ObstaclePenaltyStacksWithDistance()
    {
        BattleState state = AttackScene(
            MakeStack("a0", 9, movement: MovementType.Ranged),
            MakeStack("d0", 5),
            obstacleColumn: 5,
            obstacleRow: 0);

        state.Apply(BattleCommand.Attack("A0", "D0"));

        Assert.Equal(37, state.FindUnit("D0")!.TotalHealth);
    }

    [Fact]
    public void Attack_ShooterAtPointBlank_StrikesAsMeleeWithPenaltyAndIsRetaliated()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 9, movement: MovementType.Ranged) },
            new[] { MakeStack("d0", 5) },
            start: false);
        state.FindUnit("D0")!.MoveTo(state.Field.Grid.HexAt(1, 0));
        state.Start();

        state.Apply(BattleCommand.Attack("A0", "D0"));

        Assert.Equal(44, state.FindUnit("D0")!.TotalHealth);
        Assert.Equal(25, state.FindUnit("A0")!.TotalHealth);
        Assert.Equal(1, state.FindUnit("D0")!.RetaliationsUsedThisRound);
    }

    [Fact]
    public void Attack_ShooterRetaliatesInMeleeWithPenalty()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 9) },
            new[] { MakeStack("d0", 5, movement: MovementType.Ranged) },
            start: false);
        state.FindUnit("D0")!.MoveTo(state.Field.Grid.HexAt(1, 0));
        state.Start();

        state.Apply(BattleCommand.Attack("A0", "D0"));

        Assert.Equal(25, state.FindUnit("D0")!.TotalHealth);
        Assert.Equal(46, state.FindUnit("A0")!.TotalHealth);
    }

    [Fact]
    public void Attack_RetaliationIsLimitedToOncePerRound()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 9, speed: 6), MakeStack("a1", 8, speed: 6) },
            new[] { MakeStack("d0", 1, count: 20, damage: 1) },
            start: false);
        state.FindUnit("D0")!.MoveTo(state.Field.Grid.HexAt(3, 0));
        state.Start();

        Assert.True(state.Apply(BattleCommand.Attack("A0", "D0")).IsApplied);
        Assert.True(state.Apply(BattleCommand.Attack("A1", "D0")).IsApplied);

        Assert.Equal(1, state.FindUnit("D0")!.RetaliationsUsedThisRound);
        Assert.Equal(50, state.FindUnit("A1")!.TotalHealth);
    }

    [Fact]
    public void Attack_DefendingTargetTakesLessDamage()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 5, speed: 12) },
            new[] { MakeStack("d0", 9) });

        Assert.True(state.Apply(BattleCommand.Defend("D0")).IsApplied);
        Assert.True(state.Apply(BattleCommand.Attack("A0", "D0")).IsApplied);

        Assert.Equal(28, state.FindUnit("D0")!.TotalHealth);
    }

    [Fact]
    public void Attack_Kill_GrantsVeterancyMovesMoraleAndEndsBattle()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 9, speed: 12) },
            new[] { MakeStack("d0", 1, count: 1) });
        BattleUnit attacker = state.FindUnit("A0")!;

        Assert.True(state.Apply(BattleCommand.Attack("A0", "D0")).IsApplied);

        Assert.False(state.FindUnit("D0")!.IsAlive);
        Assert.Equal(1, attacker.KilledStacks);
        Assert.Equal(BattleStatus.AttackerWon, state.Status);
        Assert.Equal(0, attacker.RetaliationsUsedThisRound);
        Assert.Equal(50, attacker.TotalHealth);
    }

    [Fact]
    public void Attack_RetaliationKill_DestroysAttackerAndTurnStillEnds()
    {
        BattleState state = Create(
            new[] { MakeStack("a0", 9, speed: 12, count: 1, damage: 1) },
            new[] { MakeStack("d0", 5, count: 20, damage: 50) });

        Assert.True(state.Apply(BattleCommand.Attack("A0", "D0")).IsApplied);

        Assert.False(state.FindUnit("A0")!.IsAlive);
        Assert.Equal(1, state.FindUnit("D0")!.KilledStacks);
        Assert.Equal(BattleStatus.DefenderWon, state.Status);
    }

    [Fact]
    public void Attack_LogsStrikeAndRetaliation()
    {
        TestLogger logger = new();
        BattleState state = Create(
            new[] { MakeStack("a0", 9, speed: 12) },
            new[] { MakeStack("d0", 5) },
            logger: logger);

        state.Apply(BattleCommand.Attack("A0", "D0"));

        Assert.Contains(logger.Events, e => e.Category == "Battle" && e.Message == "Удар." && e.Fields["attackerId"] == "A0");
        Assert.Contains(logger.Events, e => e.Category == "Battle" && e.Message == "Контратака." && e.Fields["attackerId"] == "D0");
    }

    [Fact]
    public void Command_Factories_ValidateInput()
    {
        Assert.Throws<ArgumentException>(() => BattleCommand.Wait(" "));
        Assert.Equal(BattleCommandKind.Move, BattleCommand.Move("A0", Hex.Zero).Kind);
        Assert.Null(BattleCommand.Defend("A0").TargetHex);
        Assert.Equal("D0", BattleCommand.Attack("A0", "D0").TargetUnitId);
        Assert.Throws<ArgumentException>(() => BattleCommand.Attack("A0", ""));
    }
}
