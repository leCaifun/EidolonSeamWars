using System.Text.Json;
using Game.Core.Battle;
using Game.Core.Content;
using Game.Core.Domain;
using Game.Core.Rng;

namespace Game.Core.Tests;

public class BattleReplayTests
{
    private static ContentRegistry Registry =>
        RealContent.MergeWith().Registry ?? throw new InvalidOperationException("Контент не слит.");

    private static Stack RandomStack(string id, int initiative, int count, MovementType movement = MovementType.Infantry)
    {
        return new Stack(
            TestContent.CreateUnit(
                id: id,
                initiative: initiative,
                speed: 5,
                movementType: movement,
                damageMin: 1,
                damageMax: 9,
                critChancePercent: 30),
            count);
    }

    private static BattleState CreateState(ulong seed)
    {
        BattleRules rules = BattleContentAdapter.CreateRules(Registry);

        BattleState state = BattleState.Create(
            new BattleField(rules.FieldWidth, rules.FieldHeight),
            rules,
            new MoraleSystem(BattleContentAdapter.CreateMoraleRules(Registry)),
            new DamageCalculator(rules),
            new SeededRandom(seed),
            new BattleSideSetup(new[] { RandomStack("a0", 9, 12), RandomStack("a1", 6, 8, MovementType.Ranged) }),
            new BattleSideSetup(new[] { RandomStack("d0", 8, 10), RandomStack("d1", 4, 9) }));

        state.Start();
        return state;
    }

    /// <summary>
    /// Детерминированная политика: атаковать ближайшего врага; если не достать — подойти на
    /// ближайший к нему достижимый гекс и встать в защиту.
    /// </summary>
    private static BattleCommandJournal Play(BattleState state, ulong seed, int maxCommands = 3000)
    {
        BattleCommandJournal journal = new(seed);

        for (int i = 0; i < maxCommands && !state.IsOver; i++)
        {
            BattleUnit active = state.ActiveUnit!;
            BattleUnit enemy = state.Units
                .Where(unit => unit.IsAlive && unit.Side != active.Side)
                .OrderBy(unit => state.Field.Distance(active.Position, unit.Position))
                .ThenBy(unit => unit.Id, StringComparer.Ordinal)
                .First();

            if (journal.Apply(state, BattleCommand.Attack(active.Id, enemy.Id)).IsApplied)
            {
                continue;
            }

            KeyValuePair<Hex, int>[] options = state.ComputeReachable(active)
                .OrderBy(pair => state.Field.Distance(pair.Key, enemy.Position))
                .ThenBy(pair => pair.Key.Q)
                .ThenBy(pair => pair.Key.R)
                .ToArray();

            if (options.Length > 0)
            {
                Assert.True(journal.Apply(state, BattleCommand.Move(active.Id, options[0].Key)).IsApplied);
            }

            Assert.True(journal.Apply(state, BattleCommand.Defend(active.Id)).IsApplied);
        }

        return journal;
    }

    [Fact]
    public void Battle_PlaysToCompletion()
    {
        BattleState state = CreateState(7);

        Play(state, 7);

        Assert.True(state.IsOver);
        Assert.NotEqual(BattleStatus.Ongoing, state.Status);
        Assert.Null(state.ActiveUnit);
    }

    [Fact]
    public void Replay_SameSeedAndJournal_ReproducesIdenticalBattle()
    {
        BattleState original = CreateState(2026);
        BattleCommandJournal journal = Play(original, 2026);

        BattleState replayed = CreateState(2026);
        IReadOnlyList<BattleCommandResult> results = BattleCommandJournal.Replay(replayed, journal.Commands);

        Assert.Equal(original.CreateSnapshot(), replayed.CreateSnapshot());
        Assert.Equal(
            journal.Results.Select(r => r.Outcome),
            results.Select(r => r.Outcome));
    }

    [Fact]
    public void Replay_ThroughJsonRoundTrip_ReproducesIdenticalBattle()
    {
        BattleState original = CreateState(99);
        BattleCommandJournal journal = Play(original, 99);

        string json = BattleCommandCodec.Serialize(journal.Commands);
        IReadOnlyList<BattleCommand> restored = BattleCommandCodec.Deserialize(json);

        BattleState replayed = CreateState(99);
        BattleCommandJournal.Replay(replayed, restored);

        Assert.Equal(original.CreateSnapshot(), replayed.CreateSnapshot());
        Assert.Equal(journal.Commands.Count, restored.Count);
    }

    [Fact]
    public void Play_SameSeedTwice_IsDeterministic()
    {
        BattleState first = CreateState(5);
        BattleState second = CreateState(5);

        Play(first, 5);
        Play(second, 5);

        Assert.Equal(first.CreateSnapshot(), second.CreateSnapshot());
    }

    [Fact]
    public void Codec_RoundTripsEveryCommandKind()
    {
        BattleCommand[] commands =
        {
            BattleCommand.Move("A0", new Hex(3, -2)),
            BattleCommand.Wait("A1"),
            BattleCommand.Defend("D0"),
            BattleCommand.Attack("D1", "A0"),
            BattleCommand.UseAbility("A0", "core.ability.ash.curse", "D0"),
            BattleCommand.UseAbility("A1", "core.ability.brass.banner"),
            BattleCommand.CastSpell(BattleSide.Defender, "A0"),
            BattleCommand.UseCommandAbility(BattleSide.Attacker)
        };

        IReadOnlyList<BattleCommand> restored = BattleCommandCodec.Deserialize(BattleCommandCodec.Serialize(commands));

        Assert.Equal(commands.Length, restored.Count);

        for (int i = 0; i < commands.Length; i++)
        {
            Assert.Equal(commands[i].Kind, restored[i].Kind);
            Assert.Equal(commands[i].UnitId, restored[i].UnitId);
            Assert.Equal(commands[i].TargetHex, restored[i].TargetHex);
            Assert.Equal(commands[i].TargetUnitId, restored[i].TargetUnitId);
            Assert.Equal(commands[i].AbilityId, restored[i].AbilityId);
            Assert.Equal(commands[i].Side, restored[i].Side);
        }
    }

    [Fact]
    public void Codec_InvalidJson_Throws()
    {
        Assert.Throws<ArgumentException>(() => BattleCommandCodec.Deserialize(" "));
        Assert.Throws<JsonException>(() => BattleCommandCodec.Deserialize("[{\"kind\":\"Teleport\",\"unitId\":\"A0\"}]"));
        Assert.Throws<JsonException>(() => BattleCommandCodec.Deserialize("[{\"kind\":\"Move\",\"unitId\":\"A0\"}]"));
        Assert.Throws<JsonException>(() => BattleCommandCodec.Deserialize("[{\"kind\":\"Attack\",\"unitId\":\"A0\"}]"));
        Assert.Throws<JsonException>(() => BattleCommandCodec.Deserialize("[{\"kind\":\"Wait\"}]"));
        Assert.Throws<JsonException>(() => BattleCommandCodec.Deserialize("[{\"kind\":\"Wait\",\"unitId\":\"A0\",\"extra\":1}]"));
        Assert.Throws<JsonException>(() => BattleCommandCodec.Deserialize("[{\"kind\":\"CastSpell\",\"unitId\":\"hero:Attacker\",\"targetUnitId\":\"D0\"}]"));
        Assert.Throws<JsonException>(() => BattleCommandCodec.Deserialize("[{\"kind\":\"UseAbility\",\"unitId\":\"A0\"}]"));
        Assert.Throws<ArgumentNullException>(() => BattleCommandCodec.Serialize(null!));
    }

    [Fact]
    public void Journal_RecordsRejectedCommandsToo()
    {
        BattleState state = CreateState(1);
        BattleCommandJournal journal = new(1);

        BattleCommandResult result = journal.Apply(state, BattleCommand.Defend("not-active"));

        Assert.False(result.IsApplied);
        Assert.Single(journal.Commands);
        Assert.Single(journal.Results);
        Assert.Equal(1UL, journal.BattleSeed);
    }

    [Fact]
    public void Snapshot_ChangesAfterCommand()
    {
        BattleState state = CreateState(3);
        string before = state.CreateSnapshot();

        state.Apply(BattleCommand.Defend(state.ActiveUnit!.Id));

        Assert.NotEqual(before, state.CreateSnapshot());
    }
}
