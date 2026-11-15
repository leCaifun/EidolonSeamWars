using Game.Core.Domain;
using Game.Core.Heroes;

namespace Game.Core.Tests;

public class EscapeRansomTests
{
    [Theory]
    [InlineData(0, 0, 65)]
    [InlineData(2, 1, 69)]
    [InlineData(10, 0, 90)]
    [InlineData(0, 5, 55)]
    [InlineData(0, 30, 20)]
    [InlineData(4, 6, 65)]
    public void EscapeChance_FollowsClampedFormula(int heroLogistics, int enemyLogistics, int expected)
    {
        Assert.Equal(expected, EscapeRules.EscapeChancePercent(heroLogistics, enemyLogistics));
    }

    [Fact]
    public void EscapeChance_RejectsNegativeLogistics()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EscapeRules.EscapeChancePercent(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => EscapeRules.EscapeChancePercent(0, -1));
    }

    [Theory]
    [InlineData(0, 0, 649, EscapeResult.Escaped)]
    [InlineData(0, 0, 650, EscapeResult.Captured)]
    [InlineData(10, 0, 899, EscapeResult.Escaped)]
    [InlineData(10, 0, 900, EscapeResult.Captured)]
    [InlineData(0, 30, 199, EscapeResult.Escaped)]
    [InlineData(0, 30, 200, EscapeResult.Captured)]
    public void TryEscape_UsesPerMilleBoundary(
        int heroLogistics,
        int enemyLogistics,
        int roll,
        EscapeResult expected)
    {
        Assert.Equal(
            expected,
            EscapeRules.TryEscape(heroLogistics, enemyLogistics, isSiegeDefender: false, roll));
    }

    [Fact]
    public void TryEscape_ForbidsSiegeDefenderEvenWithLowRoll()
    {
        Assert.Equal(
            EscapeResult.Forbidden,
            EscapeRules.TryEscape(10, 10, isSiegeDefender: true, rollPerMille: 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000)]
    public void TryEscape_RejectsRollOutsidePerMille(int roll)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => EscapeRules.TryEscape(0, 0, isSiegeDefender: false, roll));
    }

    [Fact]
    public void CreateRansomQuote_SumsUnitsLevelAndArtifacts()
    {
        Stack[] stacks =
        {
            new(TestContent.CreateUnit("test.unit.a"), 2),
            new(TestContent.CreateUnit("test.unit.b"), 3)
        };

        RansomQuote quote = EscapeRules.CreateRansomQuote(
            heroLevel: 3,
            artifactCount: 2,
            isSiegeDefender: false,
            stacks,
            unitPrice: id => id == "test.unit.a" ? 100 : 50);

        Assert.True(quote.IsAllowed);
        Assert.Equal(350, quote.UnitsCost);
        Assert.Equal(3000, quote.HeroLevelCost);
        Assert.Equal(200, quote.ArtifactsCost);
        Assert.Equal(3550, quote.GoldCost);
    }

    [Fact]
    public void CreateRansomQuote_ForbidsSiegeDefenderWithoutPricingUnits()
    {
        RansomQuote quote = EscapeRules.CreateRansomQuote(
            heroLevel: 1,
            artifactCount: 0,
            isSiegeDefender: true,
            new[] { new Stack(TestContent.CreateUnit(), 1) },
            unitPrice: _ => throw new InvalidOperationException("Цена не должна запрашиваться."));

        Assert.False(quote.IsAllowed);
        Assert.Equal(0, quote.GoldCost);
    }

    [Fact]
    public void CreateRansomQuote_ValidatesArguments()
    {
        Stack[] stacks = { new(TestContent.CreateUnit(), 1) };

        Assert.Throws<ArgumentNullException>(
            () => EscapeRules.CreateRansomQuote(1, 0, false, null!, _ => 1));
        Assert.Throws<ArgumentNullException>(
            () => EscapeRules.CreateRansomQuote(1, 0, false, stacks, null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => EscapeRules.CreateRansomQuote(0, 0, false, stacks, _ => 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => EscapeRules.CreateRansomQuote(1, -1, false, stacks, _ => 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => EscapeRules.CreateRansomQuote(1, 0, false, stacks, _ => -1));
    }

    [Fact]
    public void RansomQuote_CanPayRequiresAllowedQuoteAndEnoughGold()
    {
        RansomQuote quote = new(goldCost: 100, unitsCost: 100, heroLevelCost: 0, artifactsCost: 0, isAllowed: true);

        Assert.True(quote.CanPay(new ResourcePool(100, 0, 0, 0, 0, 0, 0)));
        Assert.False(quote.CanPay(new ResourcePool(99, 0, 0, 0, 0, 0, 0)));
        Assert.False(RansomQuote.Forbidden.CanPay(new ResourcePool(9999, 0, 0, 0, 0, 0, 0)));
    }

    [Fact]
    public void CaptivityState_CountsDownToFreedom()
    {
        CaptivityState captivity = new("test.hero");

        Assert.False(captivity.IsFreed);
        Assert.Equal(EscapeRules.CaptivityDays, captivity.DaysRemaining);

        for (int day = 1; day < EscapeRules.CaptivityDays; day++)
        {
            Assert.False(captivity.AdvanceDay());
        }

        Assert.True(captivity.AdvanceDay());
        Assert.True(captivity.IsFreed);
        Assert.False(captivity.AdvanceDay());
    }

    [Fact]
    public void CaptivityState_ValidatesIdentifierAndDays()
    {
        Assert.Throws<ArgumentException>(() => new CaptivityState(" "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CaptivityState("test.hero", -1));
    }
}
