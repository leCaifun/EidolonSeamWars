using Game.Core.Numerics;

namespace Game.Core.Economy;

/// <summary>
/// Аккумулятор прироста юнитов (GDD §5.2, ADR-004).
/// Дробный прирост хранится в промилле, а целые юниты накапливаются в
/// <see cref="AvailableUnits"/> до капа <c>ceil(growthPerDay × 14)</c>.
/// Формула: <c>stock += growthPerDay; available = floor(stock); stock -= available</c>.
/// </summary>
public sealed class GrowthAccumulator
{
    /// <summary>Масштаб промилле.</summary>
    public const int PerMille = 1000;

    /// <summary>Горизонт накопления в днях (GDD §5.2).</summary>
    public const int CapDays = 14;

    /// <summary>
    /// Создаёт аккумулятор с заданным дневным приростом в промилле.
    /// </summary>
    /// <param name="growthPerDayPerMille">Прирост за день в промилле (500 = 0,5 юнита).</param>
    /// <exception cref="ArgumentOutOfRangeException">Прирост отрицателен.</exception>
    public GrowthAccumulator(int growthPerDayPerMille)
    {
        if (growthPerDayPerMille < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(growthPerDayPerMille),
                growthPerDayPerMille,
                "Дневной прирост не может быть отрицательным.");
        }

        GrowthPerDayPerMille = growthPerDayPerMille;
        CapUnits = (int)IntegerMath.CeilDiv((long)growthPerDayPerMille * CapDays, PerMille);
    }

    /// <summary>Дневной прирост в промилле.</summary>
    public int GrowthPerDayPerMille { get; }

    /// <summary>Максимальный запас целых юнитов: <c>ceil(growthPerDay × 14)</c>.</summary>
    public int CapUnits { get; }

    /// <summary>Дробный остаток прироста в промилле; всегда меньше 1000.</summary>
    public int StockPerMille { get; private set; }

    /// <summary>Накопленные целые юниты, доступные к найму.</summary>
    public int AvailableUnits { get; private set; }

    /// <summary>
    /// Проводит день: добавляет прирост, переносит целые юниты в доступный запас
    /// с учётом капа.
    /// </summary>
    /// <returns>Сколько целых юнитов добавлено к доступному запасу.</returns>
    /// <exception cref="OverflowException">Запас не помещается в int.</exception>
    public int AdvanceDay()
    {
        int stock = checked(StockPerMille + GrowthPerDayPerMille);
        int wholeUnits = stock / PerMille;
        StockPerMille = stock % PerMille;

        int room = CapUnits - AvailableUnits;
        int added = Math.Min(wholeUnits, Math.Max(0, room));
        AvailableUnits += added;
        return added;
    }

    /// <summary>
    /// Снимает юнитов с доступного запаса, например при найме.
    /// </summary>
    /// <param name="count">Сколько юнитов требуется; неотрицательно.</param>
    /// <returns>Сколько юнитов фактически снято.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Количество отрицательно.</exception>
    public int Consume(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Количество не может быть отрицательным.");
        }

        int removed = Math.Min(count, AvailableUnits);
        AvailableUnits -= removed;
        return removed;
    }
}
