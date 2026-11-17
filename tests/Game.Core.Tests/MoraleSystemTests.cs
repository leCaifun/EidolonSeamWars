using Game.Core.Battle;
using Game.Core.Content;
using Game.Core.Domain;
using Game.Core.Rng;

namespace Game.Core.Tests;

public class MoraleSystemTests
{
    private const string Brass = "core.faction.brass";
    private static readonly Lazy<IReadOnlyDictionary<string, UnitDef>> Units =
        new(() => BattleContentAdapter.CreateUnits(
            RealContent.MergeWith().Registry ?? throw new InvalidOperationException("Контент не слит.")));

    private static UnitDef Unit(string id) => Units.Value[id];

    private static MoraleRules Rules() =>
        BattleContentAdapter.CreateMoraleRules(
            RealContent.MergeWith().Registry ?? throw new InvalidOperationException("Контент не слит."));

    private static MoraleSystem CreateSystem(TestLogger? logger = null) => new(Rules(), logger);

    [Theory]
    [InlineData(-4, 32, -16)]
    [InlineData(-3, 16, -8)]
    [InlineData(-2, 8, -4)]
    [InlineData(-1, 4, -2)]
    [InlineData(0, 0, 0)]
    [InlineData(1, 4, 2)]
    [InlineData(2, 8, 4)]
    [InlineData(3, 16, 8)]
    [InlineData(4, 32, 16)]
    public void Levels_MatchGddTable(int level, int chance, int stat)
    {
        MoraleSystem morale = CreateSystem();

        Assert.Equal(chance, morale.GetChancePercent(level));
        Assert.Equal(stat, morale.GetStatModifierPercent(level));
    }

    [Fact]
    public void Levels_FollowExponentialFormulaWithCaps()
    {
        MoraleSystem morale = CreateSystem();

        for (int level = -4; level <= 4; level++)
        {
            if (level == 0)
            {
                continue;
            }

            int power = 1 << (Math.Abs(level) - 1);
            int sign = Math.Sign(level);

            Assert.Equal(Math.Min(35, 4 * power), morale.GetChancePercent(level));
            Assert.Equal(sign * Math.Min(16, 2 * power), morale.GetStatModifierPercent(level));
        }
    }

    [Fact]
    public void Caps_AreEnforcedEvenForOversizedTableValues()
    {
        MoraleRules rules = new(
            -1, 1, 35, 16, 1, new[] { "undead" },
            new[]
            {
                new MoraleLevelRule(-1, "k.minus", 90, -50),
                new MoraleLevelRule(0, "k.zero", 0, 0),
                new MoraleLevelRule(1, "k.plus", 90, 50)
            },
            1, -1, -2, 1, 1, -1, 1);
        MoraleSystem morale = new(rules);

        Assert.Equal(35, morale.GetChancePercent(1));
        Assert.Equal(16, morale.GetStatModifierPercent(1));
        Assert.Equal(-16, morale.GetStatModifierPercent(-1));
    }

    [Fact]
    public void LevelNames_AreDescriptiveKeys()
    {
        MoraleSystem morale = CreateSystem();

        Assert.Equal("morale.level.zero", morale.GetLevelNameKey(0));
        Assert.Equal("morale.level.plus4", morale.GetLevelNameKey(4));
        Assert.Equal("morale.level.minus4", morale.GetLevelNameKey(-9));
    }

    [Fact]
    public void ComputeLevel_SumsSourcesAndClamps()
    {
        MoraleSystem morale = CreateSystem();

        Assert.Equal(1, morale.ComputeLevel(new MoraleContext(true, 0, false, false, false, 0)));
        Assert.Equal(-2, morale.ComputeLevel(new MoraleContext(false, 2, false, false, false, 0)));
        Assert.Equal(-2, morale.ComputeLevel(new MoraleContext(false, 0, true, false, false, 0)));
        Assert.Equal(3, morale.ComputeLevel(new MoraleContext(true, 0, false, true, true, 0)));
        Assert.Equal(-3, morale.ComputeLevel(new MoraleContext(false, 0, false, false, false, 3)));
        Assert.Equal(-4, morale.ComputeLevel(new MoraleContext(false, 3, true, false, false, 5)));
    }

    [Fact]
    public void ComputeLevel_EachAllyDeathCountsSeparately()
    {
        MoraleSystem morale = CreateSystem();

        int one = morale.ComputeLevel(new MoraleContext(false, 0, false, false, false, 1));
        int two = morale.ComputeLevel(new MoraleContext(false, 0, false, false, false, 2));

        Assert.Equal(-1, one);
        Assert.Equal(-2, two);
    }

    [Fact]
    public void BuildContext_SameFactionBonusOnlyWhenArmyIsHeroFactionOnly()
    {
        MoraleSystem morale = CreateSystem();
        UnitDef militia = Unit("core.unit.brass.militia");
        UnitDef reitar = Unit("core.unit.brass.reitar");
        UnitDef ghoul = Unit("core.unit.ash.ghoul");

        MoraleContext pure = morale.BuildContext(new[] { militia, reitar }, Brass, false, false, 0);
        Assert.True(pure.HeroFactionOnly);
        Assert.Equal(0, pure.ForeignFactionCount);

        MoraleContext mixed = morale.BuildContext(new[] { militia, ghoul }, Brass, false, false, 0);
        Assert.False(mixed.HeroFactionOnly);
        Assert.Equal(1, mixed.ForeignFactionCount);

        MoraleContext heroless = morale.BuildContext(new[] { militia, ghoul }, null, false, false, 0);
        Assert.False(heroless.HeroFactionOnly);
        Assert.Equal(1, heroless.ForeignFactionCount);
    }

    [Fact]
    public void BuildContext_UndeadAmongLivingNeedsLivingUnits()
    {
        MoraleSystem morale = CreateSystem();
        UnitDef militia = Unit("core.unit.brass.militia");
        UnitDef bone = Unit("core.unit.ash.bone_servant");
        UnitDef golem = Unit("core.unit.brass.brass_golem");

        Assert.True(morale.BuildContext(new[] { bone, militia }, Brass, false, false, 0).UndeadAmongLiving);
        Assert.False(morale.BuildContext(new[] { bone }, Brass, false, false, 0).UndeadAmongLiving);
        Assert.False(morale.BuildContext(new[] { bone, golem }, Brass, false, false, 0).UndeadAmongLiving);
        Assert.False(morale.BuildContext(new[] { militia }, Brass, false, false, 0).UndeadAmongLiving);
    }

    [Fact]
    public void Immunity_UndeadAndConstructsAlwaysZero()
    {
        MoraleSystem morale = CreateSystem();
        UnitDef bone = Unit("core.unit.ash.bone_servant");
        UnitDef golem = Unit("core.unit.brass.brass_golem");
        UnitDef militia = Unit("core.unit.brass.militia");

        Assert.True(morale.IsImmune(bone));
        Assert.True(morale.IsImmune(golem));
        Assert.False(morale.IsImmune(militia));
        Assert.Equal(0, morale.GetEffectiveLevel(bone, 4));
        Assert.Equal(0, morale.GetEffectiveLevel(golem, -4));
        Assert.Equal(4, morale.GetEffectiveLevel(militia, 9));
    }

    [Fact]
    public void RollAction_ImmuneOrNeutral_DoesNotConsumeRandom()
    {
        MoraleSystem morale = CreateSystem();
        SeededRandom used = new(11);
        SeededRandom reference = new(11);

        Assert.Equal(MoraleRoll.None, morale.RollAction(Unit("core.unit.ash.bone_servant"), 4, 0, false, used));
        Assert.Equal(MoraleRoll.None, morale.RollAction(Unit("core.unit.brass.militia"), 0, 0, false, used));
        Assert.Equal(reference.NextUInt64(), used.NextUInt64());
    }

    [Fact]
    public void RollAction_ExtraActionLimitAndChains_DoNotConsumeRandom()
    {
        MoraleSystem morale = CreateSystem();
        UnitDef militia = Unit("core.unit.brass.militia");
        SeededRandom used = new(12);
        SeededRandom reference = new(12);

        Assert.Equal(MoraleRoll.None, morale.RollAction(militia, 4, 1, false, used));
        Assert.Equal(MoraleRoll.None, morale.RollAction(militia, 4, 0, true, used));
        Assert.Equal(reference.NextUInt64(), used.NextUInt64());
    }

    [Theory]
    [InlineData(4, 320, MoraleRoll.ExtraAction)]
    [InlineData(2, 80, MoraleRoll.ExtraAction)]
    [InlineData(-2, 80, MoraleRoll.Skip)]
    [InlineData(-4, 320, MoraleRoll.Skip)]
    public void RollAction_UsesOneRollAgainstPerMilleChance(int level, int threshold, MoraleRoll triggered)
    {
        MoraleSystem morale = CreateSystem();
        UnitDef militia = Unit("core.unit.brass.militia");

        for (ulong seed = 1; seed <= 40; seed++)
        {
            SeededRandom used = new(seed);
            SeededRandom reference = new(seed);
            int roll = reference.RollPerMille();

            MoraleRoll result = morale.RollAction(militia, level, 0, false, used);

            Assert.Equal(roll < threshold ? triggered : MoraleRoll.None, result);
            Assert.Equal(reference.NextUInt64(), used.NextUInt64());
        }
    }

    [Fact]
    public void RollAction_LevelFourFrequencyIsAboutThirtyTwoPercent()
    {
        MoraleSystem morale = CreateSystem();
        UnitDef militia = Unit("core.unit.brass.militia");
        SeededRandom random = new(2026);
        int extras = 0;

        for (int i = 0; i < 3000; i++)
        {
            if (morale.RollAction(militia, 4, 0, false, random) == MoraleRoll.ExtraAction)
            {
                extras++;
            }
        }

        Assert.InRange(extras, 840, 1080);
    }

    [Fact]
    public void RollAction_LogsMoraleRoll()
    {
        TestLogger logger = new();

        CreateSystem(logger).RollAction(Unit("core.unit.brass.militia"), 3, 0, false, new SeededRandom(5));

        Assert.Contains(
            logger.Events,
            e => e.Category == "Battle" && e.Fields["moraleLevel"] == "3" && e.Fields["unitId"] == "core.unit.brass.militia");
    }

    [Fact]
    public void Rules_FromRealContent_MatchGdd()
    {
        MoraleRules rules = Rules();

        Assert.Equal(-4, rules.MinLevel);
        Assert.Equal(4, rules.MaxLevel);
        Assert.Equal(35, rules.ExtraActionChanceCapPercent);
        Assert.Equal(16, rules.StatModifierCapPercent);
        Assert.Equal(1, rules.MaxExtraActionsPerUnitPerRound);
        Assert.True(rules.IsImmuneClass("undead"));
        Assert.True(rules.IsImmuneClass("construct"));
        Assert.False(rules.IsImmuneClass("infantry"));
        Assert.Equal(1, rules.SameFaction);
        Assert.Equal(-1, rules.PerExtraFaction);
        Assert.Equal(-2, rules.UndeadAmongLiving);
        Assert.Equal(1, rules.MarshalAura);
        Assert.Equal(1, rules.DefendingSettlement);
        Assert.Equal(-1, rules.AllyStackDeath);
        Assert.Equal(1, rules.AllyStackDeathDurationRounds);
    }

    [Fact]
    public void Rules_IncompleteTable_Throws()
    {
        Assert.Throws<ArgumentException>(() => new MoraleRules(
            -1, 1, 35, 16, 1, Array.Empty<string>(),
            new[] { new MoraleLevelRule(0, "k.zero", 0, 0) },
            1, -1, -2, 1, 1, -1, 1));
    }
}
