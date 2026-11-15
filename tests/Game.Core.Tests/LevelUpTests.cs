using Game.Core.Diagnostics;
using Game.Core.Domain;
using Game.Core.Rng;

namespace Game.Core.Tests;

public class LevelUpTests
{
    [Fact]
    public void RollStat_ReturnsOnlyWeightedStat()
    {
        HeroClassDefinition heroClass = TestContent.CreateHeroClass(
            weights: new Dictionary<HeroStatKind, int> { [HeroStatKind.Knowledge] = 1 });

        for (ulong seed = 0; seed < 20; seed++)
        {
            Assert.Equal(HeroStatKind.Knowledge, LevelUp.RollStat(heroClass, new SeededRandom(seed)));
        }
    }

    [Fact]
    public void RollStat_IsDeterministicForSameSeed()
    {
        HeroClassDefinition heroClass = TestContent.CreateHeroClass(
            weights: new Dictionary<HeroStatKind, int>
            {
                [HeroStatKind.Attack] = 1,
                [HeroStatKind.Defense] = 1,
                [HeroStatKind.Spellpower] = 1,
                [HeroStatKind.Knowledge] = 1,
                [HeroStatKind.Leadership] = 1,
                [HeroStatKind.Logistics] = 1
            });

        SeededRandom first = new(12345);
        SeededRandom second = new(12345);

        for (int i = 0; i < 50; i++)
        {
            Assert.Equal(LevelUp.RollStat(heroClass, first), LevelUp.RollStat(heroClass, second));
        }
    }

    [Fact]
    public void RollStat_RejectsNullArguments()
    {
        HeroClassDefinition heroClass = TestContent.CreateHeroClass();

        Assert.Throws<ArgumentNullException>(() => LevelUp.RollStat(null!, new SeededRandom(1)));
        Assert.Throws<ArgumentNullException>(() => LevelUp.RollStat(heroClass, null!));
    }

    [Fact]
    public void HeroClass_ExposesWeightsAndTotal()
    {
        HeroClassDefinition heroClass = TestContent.CreateHeroClass(
            weights: new Dictionary<HeroStatKind, int>
            {
                [HeroStatKind.Attack] = 3,
                [HeroStatKind.Defense] = 2
            });

        Assert.Equal(5, heroClass.TotalLevelUpWeight);
        Assert.Equal(3, heroClass.GetWeight(HeroStatKind.Attack));
        Assert.Equal(2, heroClass.GetWeight(HeroStatKind.Defense));
        Assert.Equal(0, heroClass.GetWeight(HeroStatKind.Logistics));
    }

    [Fact]
    public void HeroClass_RejectsNegativeOrZeroTotalWeights()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestContent.CreateHeroClass(
                weights: new Dictionary<HeroStatKind, int> { [HeroStatKind.Attack] = -1 }));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestContent.CreateHeroClass(
                weights: new Dictionary<HeroStatKind, int> { [HeroStatKind.Attack] = 0 }));
    }

    [Fact]
    public void HeroStats_RejectsNegativeAndNonPositiveLeadership()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HeroStats(-1, 0, 0, 0, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HeroStats(0, 0, 0, 0, 0, 0));
    }

    [Fact]
    public void HeroStats_WithChangesOneStatAndKeepsOthers()
    {
        HeroStats stats = new(1, 2, 3, 4, 5, 6);

        HeroStats changed = stats.With(HeroStatKind.Spellpower, 2);

        Assert.Equal(5, changed.Spellpower);
        Assert.Equal(stats.Attack, changed.Attack);
        Assert.Equal(stats.Leadership, changed.Leadership);
    }

    [Fact]
    public void HeroStats_GetAndWithRejectUnknownKind()
    {
        HeroStats stats = new(1, 2, 3, 4, 5, 6);

        Assert.Throws<ArgumentOutOfRangeException>(() => stats.Get((HeroStatKind)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => stats.With((HeroStatKind)99, 1));
    }

    [Fact]
    public void Hero_DerivedStatsFollowGddFormulas()
    {
        HeroClassDefinition heroClass = TestContent.CreateHeroClass(
            startingStats: new HeroStats(3, 4, 2, 3, 70, 5));
        Hero hero = new("test.hero", "test.hero.name", "test.faction", heroClass);

        Assert.Equal(70, hero.Leadership);
        Assert.Equal(25, hero.Mana);
        Assert.Equal(8, hero.MovementPoints);
        Assert.Equal(0, hero.SkillPoints);
    }

    [Fact]
    public void Hero_AwardExperienceIncrementsStatAndLevel()
    {
        TestLogger logger = new();
        HeroClassDefinition heroClass = TestContent.CreateHeroClass(
            startingStats: new HeroStats(3, 4, 2, 3, 70, 5),
            weights: new Dictionary<HeroStatKind, int> { [HeroStatKind.Attack] = 1 });
        Hero hero = TestContent.CreateHero(heroClass);
        HeroProgression progression = new(50, 1, 10, logger);

        HeroProgressionResult result = progression.AwardExperience(
            hero,
            progression.ExperienceToNextLevel(1),
            new SeededRandom(7));

        Assert.Equal(1, result.LevelsGained);
        Assert.Equal(HeroStatKind.Attack, Assert.Single(result.GainedStats));
        Assert.Equal(2, hero.Level);
        Assert.Equal(4, hero.Stats.Attack);
        Assert.Equal(4, hero.Stats.Defense);
        Assert.Equal(1, hero.SkillPoints);

        LogEvent entry = Assert.Single(
            logger.Events,
            item => item.Message.Contains("уровень 2", StringComparison.Ordinal));
        Assert.Equal("Hero", entry.Category);
    }

    [Fact]
    public void HeroProgression_StopsAtScenarioSoftCap()
    {
        HeroClassDefinition heroClass = TestContent.CreateHeroClass();
        Hero hero = new(
            "test.hero",
            "test.hero.name",
            "test.faction",
            heroClass,
            level: 10);
        HeroProgression progression = new(50, 1, 10);

        HeroProgressionResult result = progression.AwardExperience(hero, 100_000, new SeededRandom(1));

        Assert.True(progression.IsAtSoftCap(hero.Level));
        Assert.True(result.StoppedBySoftCap);
        Assert.Equal(0, result.LevelsGained);
        Assert.Equal(10, hero.Level);
        Assert.Equal(100_000, hero.Experience);
    }

    [Fact]
    public void Hero_AddExperienceAccumulatesAndRejectsNegative()
    {
        Hero hero = TestContent.CreateHero();

        hero.AddExperience(10);
        hero.AddExperience(5);

        Assert.Equal(15, hero.Experience);
        Assert.Throws<ArgumentOutOfRangeException>(() => hero.AddExperience(-1));
    }

    [Fact]
    public void Hero_ConstructorValidatesIdentity()
    {
        HeroClassDefinition heroClass = TestContent.CreateHeroClass();

        Assert.Throws<ArgumentException>(
            () => new Hero(" ", "test.hero.name", "test.faction", heroClass));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Hero("test.hero", "test.hero.name", "test.faction", heroClass, level: 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Hero("test.hero", "test.hero.name", "test.faction", heroClass, experience: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Hero("test.hero", "test.hero.name", "test.faction", heroClass, artifactCount: -1));
    }
}
