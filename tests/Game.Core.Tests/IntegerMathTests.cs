using Game.Core.Numerics;

namespace Game.Core.Tests;

public class IntegerMathTests
{
    [Theory]
    [InlineData(0, 1, 1, 0)]
    [InlineData(5, 1000, 2, 2500)]
    [InlineData(1, 1, 2, 1)]
    [InlineData(3, 1, 2, 2)]
    [InlineData(-1, 1, 2, -1)]
    [InlineData(-3, 1, 2, -2)]
    [InlineData(1, 1, -2, -1)]
    [InlineData(-1, 1, -2, 1)]
    [InlineData(-4, 1, 2, -2)]
    public void MulDiv_RoundsHalfAwayFromZero(long value, long multiplier, long divisor, long expected)
    {
        Assert.Equal(expected, IntegerMath.MulDiv(value, multiplier, divisor));
    }

    [Fact]
    public void MulDiv_ThrowsOnZeroDivisorWithContext()
    {
        DivideByZeroException exception = Assert.Throws<DivideByZeroException>(
            () => IntegerMath.MulDiv(10, 10, 0));

        Assert.Contains("делитель", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MulDiv_ThrowsOnMinValueDivisor()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => IntegerMath.MulDiv(10, 10, long.MinValue));

        Assert.Contains("long.MinValue", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MulDiv_ThrowsOnIntermediateOverflowWithContext()
    {
        OverflowException exception = Assert.Throws<OverflowException>(
            () => IntegerMath.MulDiv(long.MaxValue, 2, 1));

        Assert.Contains("Переполнение MulDiv", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MulDivToInt_ThrowsWhenResultDoesNotFitInt()
    {
        Assert.Throws<OverflowException>(() => IntegerMath.MulDivToInt(int.MaxValue, 1000, 1));
    }

    [Fact]
    public void MulDivToInt_ReturnsIntResult()
    {
        Assert.Equal(1500, IntegerMath.MulDivToInt(15, 100, 1));
    }

    [Theory]
    [InlineData(0, 5, 0)]
    [InlineData(1, 5, 1)]
    [InlineData(5, 5, 1)]
    [InlineData(6, 5, 2)]
    [InlineData(14, 5, 3)]
    public void CeilDiv_RoundsUp(long value, long divisor, long expected)
    {
        Assert.Equal(expected, IntegerMath.CeilDiv(value, divisor));
    }

    [Fact]
    public void CeilDiv_RejectsNegativeValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => IntegerMath.CeilDiv(-1, 5));
    }

    [Fact]
    public void CeilDiv_RejectsNonPositiveDivisor()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => IntegerMath.CeilDiv(1, 0));
    }
}
