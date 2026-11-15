namespace Game.Core.Economy;

/// <summary>
/// Контракт наёмников (GDD §5.5). Ротируется дневным тиком: срок уменьшается,
/// истёкшие контракты удаляются, источник может выдать новую ротацию.
/// </summary>
public sealed class RecruitmentContract
{
    /// <summary>
    /// Создаёт контракт.
    /// </summary>
    /// <param name="id">Стабильный ID контракта.</param>
    /// <param name="unitDefId">ID юнита из контента.</param>
    /// <param name="count">Доступное количество.</param>
    /// <param name="priceGold">Цена в золоте за весь лот.</param>
    /// <param name="daysRemaining">Срок действия в днях, начиная с 1.</param>
    /// <exception cref="ArgumentException">Пустой ID контракта или юнита.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Количество, цена или срок вне допустимого диапазона.</exception>
    public RecruitmentContract(string id, string unitDefId, int count, int priceGold, int daysRemaining)
    {
        Id = RequireText(id, nameof(id));
        UnitDefId = RequireText(unitDefId, nameof(unitDefId));

        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Количество в контракте должно быть положительным.");
        }

        if (priceGold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priceGold), priceGold, "Цена контракта не может быть отрицательной.");
        }

        if (daysRemaining <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(daysRemaining), daysRemaining, "Срок контракта должен быть положительным.");
        }

        Count = count;
        PriceGold = priceGold;
        DaysRemaining = daysRemaining;
    }

    /// <summary>Стабильный ID контракта.</summary>
    public string Id { get; }

    /// <summary>ID юнита из контента.</summary>
    public string UnitDefId { get; }

    /// <summary>Доступное количество юнитов.</summary>
    public int Count { get; }

    /// <summary>Цена лота в золоте.</summary>
    public int PriceGold { get; }

    /// <summary>Остаток срока в днях.</summary>
    public int DaysRemaining { get; private set; }

    /// <summary>Истёк ли контракт.</summary>
    public bool IsExpired => DaysRemaining == 0;

    /// <summary>
    /// Уменьшает срок на один день.
    /// </summary>
    /// <returns><c>true</c>, если контракт истёк этим вызовом.</returns>
    public bool AdvanceDay()
    {
        if (DaysRemaining == 0)
        {
            return false;
        }

        DaysRemaining--;
        return DaysRemaining == 0;
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Строковое поле контракта не может быть пустым.", parameterName);
        }

        return value;
    }
}
