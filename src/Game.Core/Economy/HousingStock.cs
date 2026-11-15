namespace Game.Core.Economy;

/// <summary>
/// Внешнее жилище (GDD §5.5): охраняемый сток юнитов. После захвата флага
/// жилище даёт дневной прирост через <see cref="GrowthAccumulator"/>.
/// Жилище без флага захвата — конечный сток с медленной регенерацией.
/// </summary>
public sealed class HousingStock
{
    private readonly GrowthAccumulator _growth;

    /// <summary>
    /// Создаёт жилище.
    /// </summary>
    /// <param name="stock">Текущий сток юнитов.</param>
    /// <param name="cap">Максимальный сток юнитов.</param>
    /// <param name="growthPerDayPerMille">Дневной прирост после захвата, в промилле.</param>
    /// <param name="uncapturedRegenPerDay">Регенерация стока без флага захвата, юнитов в день.</param>
    /// <exception cref="ArgumentOutOfRangeException">Значения вне допустимого диапазона.</exception>
    public HousingStock(
        int stock,
        int cap,
        int growthPerDayPerMille,
        int uncapturedRegenPerDay)
    {
        if (cap < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cap), cap, "Максимальный сток не может быть отрицательным.");
        }

        if (stock < 0 || stock > cap)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stock),
                stock,
                $"Текущий сток должен быть в диапазоне 0…{cap}.");
        }

        if (uncapturedRegenPerDay < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(uncapturedRegenPerDay),
                uncapturedRegenPerDay,
                "Регенерация не может быть отрицательной.");
        }

        Stock = stock;
        Cap = cap;
        UncapturedRegenPerDay = uncapturedRegenPerDay;
        _growth = new GrowthAccumulator(growthPerDayPerMille);
    }

    /// <summary>Текущий сток юнитов.</summary>
    public int Stock { get; private set; }

    /// <summary>Максимальный сток.</summary>
    public int Cap { get; }

    /// <summary>Регенерация без флага захвата, юнитов в день.</summary>
    public int UncapturedRegenPerDay { get; }

    /// <summary>Установлен ли флаг захвата.</summary>
    public bool IsCaptured { get; private set; }

    /// <summary>Захватывает жилище; с этого дня работает дневной прирост.</summary>
    public void Capture()
    {
        IsCaptured = true;
    }

    /// <summary>
    /// Проводит день. Захваченное жилище производит прирост и пополняет сток
    /// до капа; незахваченное медленно регенерирует к капу.
    /// </summary>
    /// <returns>Произведённые целые юниты; для незахваченного жилища — 0.</returns>
    public int AdvanceDay()
    {
        if (!IsCaptured)
        {
            Stock = Math.Min(Cap, Stock + UncapturedRegenPerDay);
            return 0;
        }

        int produced = _growth.AdvanceDay();
        int added = Math.Min(produced, Cap - Stock);
        Stock += added;
        return added;
    }
}
