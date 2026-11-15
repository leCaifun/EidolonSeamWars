using Game.Core.Domain;

namespace Game.Core.Tests;

public class VeterancyTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 2)]
    [InlineData(3, 5)]
    [InlineData(4, 10)]
    [InlineData(5, 18)]
    public void ThresholdForRank_ReturnsAdr014Thresholds(int rank, int expectedThreshold)
    {
        Assert.Equal(expectedThreshold, VeterancyRules.ThresholdForRank(rank));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void ThresholdForRank_RejectsRankOutsideOneToFive(int rank)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => VeterancyRules.ThresholdForRank(rank));
    }

    [Fact]
    public void RankForKilledStacks_RejectsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => VeterancyRules.RankForKilledStacks(-1));
    }

    [Theory]
    [InlineData(1, 0, 0, 0, 0)]
    [InlineData(2, 2, 2, 1, 1)]
    [InlineData(3, 4, 4, 2, 2)]
    [InlineData(5, 8, 8, 4, 4)]
    public void Bonuses_GrowByOnePerRankAboveFirst(
        int rank,
        int attack,
        int defense,
        int critChance,
        int critDamage)
    {
        Assert.Equal(attack, VeterancyRules.AttackBonus(rank));
        Assert.Equal(defense, VeterancyRules.DefenseBonus(rank));
        Assert.Equal(critChance, VeterancyRules.CritChanceBonus(rank));
        Assert.Equal(critDamage, VeterancyRules.CritDamageBonus(rank));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void NameKey_IsStableLocalizationKey(int rank)
    {
        Assert.Equal($"core.veterancy.rank.{rank}", VeterancyRules.NameKey(rank));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Bonuses_RejectRankOutsideRange(int rank)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => VeterancyRules.AttackBonus(rank));
        Assert.Throws<ArgumentOutOfRangeException>(() => VeterancyRules.NameKey(rank));
    }
}
