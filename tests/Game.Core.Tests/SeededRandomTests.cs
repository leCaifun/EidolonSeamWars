using Game.Core.Rng;

namespace Game.Core.Tests;

public class SeededRandomTests
{
    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        SeededRandom first = new(123456789UL);
        SeededRandom second = new(123456789UL);

        for (int i = 0; i < 256; i++)
        {
            Assert.Equal(first.NextUInt64(), second.NextUInt64());
        }
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentFirstValues()
    {
        SeededRandom first = new(1UL);
        SeededRandom second = new(2UL);

        Assert.NotEqual(first.NextUInt64(), second.NextUInt64());
    }

    [Fact]
    public void Seed_IsExposed()
    {
        SeededRandom random = new(42UL);

        Assert.Equal(42UL, random.Seed);
    }

    [Fact]
    public void Next_WithUpperBound_StaysInRange()
    {
        SeededRandom random = new(7UL);

        for (int i = 0; i < 10_000; i++)
        {
            int value = random.Next(5);

            Assert.InRange(value, 0, 4);
        }
    }

    [Fact]
    public void Next_WithRange_StaysInRange()
    {
        SeededRandom random = new(11UL);

        for (int i = 0; i < 10_000; i++)
        {
            int value = random.Next(-7, 13);

            Assert.InRange(value, -7, 12);
        }
    }

    [Fact]
    public void RollPerMille_StaysInPermilleRange()
    {
        SeededRandom random = new(2026UL);

        for (int i = 0; i < 10_000; i++)
        {
            int roll = random.RollPerMille();

            Assert.InRange(roll, 0, 999);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Next_WithInvalidUpperBound_Throws(int upperBound)
    {
        SeededRandom random = new(1UL);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => random.Next(upperBound));

        Assert.Equal("maxExclusive", exception.ParamName);
    }

    [Fact]
    public void Next_WithInvalidRange_Throws()
    {
        SeededRandom random = new(1UL);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => random.Next(10, 10));

        Assert.Equal("maxExclusive", exception.ParamName);
    }

    [Fact]
    public void Next_CoversFullRangeOverManyRolls()
    {
        SeededRandom random = new(999UL);
        HashSet<int> seen = new();

        for (int i = 0; i < 5_000; i++)
        {
            seen.Add(random.Next(0, 6));
        }

        Assert.Equal(6, seen.Count);
    }
}
