using Game.Core.Content.Dto;
using Game.Core.Domain;
using Game.Core.Rng;

namespace Game.Core.Tests;

public class HeroProgressionTests
{
    private static HeroProgression CreateProgression(int softCap = 10, int skillPoints = 1)
    {
        return new HeroProgression(
            experienceCoefficient: 50,
            skillPointsPerLevel: skillPoints,
            scenarioSoftCap: softCap);
    }

    [Fact]
    public void ExperienceToNextLevelFollowsQuadraticCurve()
    {
        HeroProgression progression = CreateProgression();

        Assert.Equal(100, progression.ExperienceToNextLevel(1));
        Assert.Equal(300, progression.ExperienceToNextLevel(2));
        Assert.Equal(600, progression.ExperienceToNextLevel(3));
        Assert.Equal(1000, progression.ExperienceToNextLevel(4));
    }

    [Fact]
    public void TotalExperienceForLevelIsCumulative()
    {
        HeroProgression progression = CreateProgression();

        Assert.Equal(0, progression.TotalExperienceForLevel(1));
        Assert.Equal(100, progression.TotalExperienceForLevel(2));
        Assert.Equal(400, progression.TotalExperienceForLevel(3));
        Assert.Equal(1000, progression.TotalExperienceForLevel(4));
    }

    [Fact]
    public void TotalExperienceForLevelRejectsInvalidLevel()
    {
        HeroProgression progression = CreateProgression();

        Assert.Throws<ArgumentOutOfRangeException>(() => progression.TotalExperienceForLevel(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => progression.ExperienceToNextLevel(0));
    }

    [Fact]
    public void LevelForExperienceReturnsHighestReachedLevel()
    {
        HeroProgression progression = CreateProgression();

        Assert.Equal(1, progression.LevelForExperience(0));
        Assert.Equal(1, progression.LevelForExperience(99));
        Assert.Equal(2, progression.LevelForExperience(100));
        Assert.Equal(2, progression.LevelForExperience(399));
        Assert.Equal(3, progression.LevelForExperience(400));
    }

    [Fact]
    public void AwardExperienceGrantsMultipleLevelsAndSkillPoints()
    {
        HeroClassDefinition heroClass = TestContent.CreateHeroClass(
            weights: new Dictionary<HeroStatKind, int> { [HeroStatKind.Attack] = 1 });
        Hero hero = TestContent.CreateHero(heroClass);
        HeroProgression progression = CreateProgression();

        HeroProgressionResult result = progression.AwardExperience(hero, 400, new SeededRandom(11));

        Assert.Equal(2, result.LevelsGained);
        Assert.Equal(2, result.SkillPointsGained);
        Assert.Equal(3, hero.Level);
        Assert.Equal(400, hero.Experience);
        Assert.Equal(2, hero.SkillPoints);
        Assert.Equal(5, hero.Stats.Attack);
        Assert.Equal(2, result.GainedStats.Count);
        Assert.All(result.GainedStats, stat => Assert.Equal(HeroStatKind.Attack, stat));
    }

    [Fact]
    public void AwardExperienceBelowThresholdKeepsLevel()
    {
        Hero hero = TestContent.CreateHero();
        HeroProgression progression = CreateProgression();

        HeroProgressionResult result = progression.AwardExperience(hero, 99, new SeededRandom(3));

        Assert.Equal(0, result.LevelsGained);
        Assert.Equal(1, hero.Level);
        Assert.Equal(99, hero.Experience);
        Assert.Equal(0, hero.SkillPoints);
    }

    [Fact]
    public void AwardExperienceAccumulatesAcrossCalls()
    {
        Hero hero = TestContent.CreateHero();
        HeroProgression progression = CreateProgression();

        progression.AwardExperience(hero, 60, new SeededRandom(5));
        Assert.Equal(1, hero.Level);

        progression.AwardExperience(hero, 40, new SeededRandom(6));
        Assert.Equal(2, hero.Level);
        Assert.Equal(100, hero.Experience);
        Assert.Equal(1, hero.SkillPoints);
    }

    [Fact]
    public void AwardExperienceIsDeterministicForSameSeed()
    {
        HeroProgression progression = CreateProgression(softCap: 0);
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
        Hero first = TestContent.CreateHero(heroClass);
        Hero second = TestContent.CreateHero(heroClass);
        RngStreams firstStreams = new(4242);
        RngStreams secondStreams = new(4242);

        HeroProgressionResult firstResult = progression.AwardExperience(
            first,
            10_000,
            firstStreams.Get(RngStreamKind.LevelUp));
        HeroProgressionResult secondResult = progression.AwardExperience(
            second,
            10_000,
            secondStreams.Get(RngStreamKind.LevelUp));

        Assert.Equal(firstResult.GainedStats, secondResult.GainedStats);
        Assert.Equal(first.Level, second.Level);
        Assert.Equal(first.Stats, second.Stats);
    }

    [Fact]
    public void AwardExperienceStopsAtSoftCapButKeepsExperience()
    {
        Hero hero = TestContent.CreateHero();
        HeroProgression progression = CreateProgression(softCap: 3);

        HeroProgressionResult result = progression.AwardExperience(hero, 100_000, new SeededRandom(9));

        Assert.Equal(2, result.LevelsGained);
        Assert.True(result.StoppedBySoftCap);
        Assert.Equal(3, hero.Level);
        Assert.Equal(100_000, hero.Experience);
        Assert.Equal(2, hero.SkillPoints);
    }

    [Fact]
    public void ZeroSoftCapMeansUnlimitedLevels()
    {
        Hero hero = TestContent.CreateHero();
        HeroProgression progression = CreateProgression(softCap: 0);

        HeroProgressionResult result = progression.AwardExperience(hero, 1400, new SeededRandom(2));

        Assert.Equal(4, hero.Level);
        Assert.Equal(3, result.LevelsGained);
        Assert.False(result.StoppedBySoftCap);
    }

    [Fact]
    public void FromRulesReadsContentValues()
    {
        ProgressionRulesDto rules = new()
        {
            SchemaVersion = 1,
            Id = "test.rules.progression",
            UnlimitedLevel = true,
            XpToNextFormula = new ProgressionFormulaDto
            {
                Type = "quadratic",
                Coefficient = 50,
                Expression = "50 * level * (level + 1)"
            },
            SkillPointsPerLevel = 1,
            ScenarioSoftCap = 10
        };

        HeroProgression progression = HeroProgression.FromRules(rules);

        Assert.Equal(50, progression.ExperienceCoefficient);
        Assert.Equal(1, progression.SkillPointsPerLevel);
        Assert.Equal(10, progression.ScenarioSoftCap);
        Assert.Equal(100, progression.ExperienceToNextLevel(1));
    }

    [Fact]
    public void FromRulesRejectsUnsupportedFormula()
    {
        ProgressionRulesDto rules = new()
        {
            XpToNextFormula = new ProgressionFormulaDto
            {
                Type = "linear",
                Coefficient = 50,
                Expression = "50 * level"
            }
        };

        Assert.Throws<InvalidOperationException>(() => HeroProgression.FromRules(rules));
    }

    [Fact]
    public void ConstructorRejectsInvalidParameters()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HeroProgression(0, 1, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HeroProgression(50, -1, 10));
    }

    [Fact]
    public void AwardExperienceRejectsNegativeAmount()
    {
        Hero hero = TestContent.CreateHero();
        HeroProgression progression = CreateProgression();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => progression.AwardExperience(hero, -1, new SeededRandom(1)));
    }
}
