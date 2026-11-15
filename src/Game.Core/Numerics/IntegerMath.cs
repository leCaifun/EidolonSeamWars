namespace Game.Core.Numerics;

/// <summary>
/// Целочисленная математика домена с округлением «половина от нуля» (ADR-022).
/// Промежуточные вычисления выполняются в <see cref="long"/>, результат
/// приводится к <see cref="int"/> только проверяемо.
/// </summary>
public static class IntegerMath
{
    /// <summary>
    /// Умножает <paramref name="value"/> на <paramref name="multiplier"/> и делит
    /// на <paramref name="divisor"/> с округлением половины от нуля.
    /// </summary>
    /// <param name="value">Исходное значение.</param>
    /// <param name="multiplier">Множитель (например, 1000 для промилле).</param>
    /// <param name="divisor">Делитель; не может быть нулём или <see cref="long.MinValue"/>.</param>
    /// <exception cref="DivideByZeroException">Делитель равен нулю.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Делитель равен <see cref="long.MinValue"/>.</exception>
    /// <exception cref="OverflowException">Промежуточное произведение не помещается в <see cref="long"/>.</exception>
    public static long MulDiv(long value, long multiplier, long divisor)
    {
        if (divisor == 0)
        {
            throw new DivideByZeroException("Деление в MulDiv: делитель не может быть нулём.");
        }

        if (divisor == long.MinValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(divisor),
                divisor,
                "Делитель long.MinValue не поддерживается: модуль не помещается в long.");
        }

        long product;
        try
        {
            product = checked(value * multiplier);
        }
        catch (OverflowException exception)
        {
            throw new OverflowException(
                $"Переполнение MulDiv: {value} × {multiplier} не помещается в long.",
                exception);
        }

        long quotient = product / divisor;
        long remainder = product % divisor;
        if (remainder == 0)
        {
            return quotient;
        }

        long absoluteRemainder = remainder < 0 ? -remainder : remainder;
        long absoluteDivisor = divisor < 0 ? -divisor : divisor;

        // Эквивалент absoluteRemainder * 2 >= absoluteDivisor без переполнения.
        if (absoluteRemainder >= absoluteDivisor - absoluteRemainder)
        {
            bool sameSign = (product < 0) == (divisor < 0);
            quotient += sameSign ? 1 : -1;
        }

        return quotient;
    }

    /// <summary>
    /// Выполняет <see cref="MulDiv(long, long, long)"/> и проверяемо приводит
    /// результат к <see cref="int"/>. Это способ расчёта игровых значений.
    /// </summary>
    /// <exception cref="OverflowException">Результат не помещается в <see cref="int"/>.</exception>
    public static int MulDivToInt(int value, int multiplier, int divisor)
    {
        long result = MulDiv(value, multiplier, divisor);
        return checked((int)result);
    }

    /// <summary>
    /// Делит неотрицательное значение на положительный делитель с округлением вверх.
    /// Используется для капов экономики, например <c>ceil(growth × 14)</c>.
    /// </summary>
    /// <param name="value">Неотрицательное значение.</param>
    /// <param name="divisor">Положительный делитель.</param>
    /// <exception cref="ArgumentOutOfRangeException">Значение или делитель вне допустимого диапазона.</exception>
    public static long CeilDiv(long value, long divisor)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Значение CeilDiv не может быть отрицательным.");
        }

        if (divisor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(divisor), divisor, "Делитель CeilDiv должен быть положительным.");
        }

        long quotient = value / divisor;
        return value % divisor == 0 ? quotient : quotient + 1;
    }
}
