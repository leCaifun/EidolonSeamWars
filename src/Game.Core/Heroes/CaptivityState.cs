namespace Game.Core.Heroes;

/// <summary>
/// Состояние плена героя (GDD §7.7, ADR-012). Плен длится 5 дней; каждый
/// дневной тик уменьшает счётчик. По истечении герой освобождается.
/// </summary>
public sealed class CaptivityState
{
    /// <summary>
    /// Создаёт состояние плена.
    /// </summary>
    /// <param name="heroId">ID пленённого героя.</param>
    /// <param name="daysRemaining">Остаток дней; по умолчанию полный срок ADR-012.</param>
    /// <exception cref="ArgumentException">ID героя пуст.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Остаток дней отрицателен.</exception>
    public CaptivityState(string heroId, int daysRemaining = EscapeRules.CaptivityDays)
    {
        if (string.IsNullOrWhiteSpace(heroId))
        {
            throw new ArgumentException("ID пленённого героя не может быть пустым.", nameof(heroId));
        }

        if (daysRemaining < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(daysRemaining), daysRemaining, "Остаток дней плена не может быть отрицательным.");
        }

        HeroId = heroId;
        DaysRemaining = daysRemaining;
    }

    /// <summary>ID пленённого героя.</summary>
    public string HeroId { get; }

    /// <summary>Остаток дней плена.</summary>
    public int DaysRemaining { get; private set; }

    /// <summary>Освобождён ли герой.</summary>
    public bool IsFreed => DaysRemaining == 0;

    /// <summary>
    /// Уменьшает срок плена на один день.
    /// </summary>
    /// <returns><c>true</c>, если герой освобождён этим вызовом.</returns>
    public bool AdvanceDay()
    {
        if (DaysRemaining == 0)
        {
            return false;
        }

        DaysRemaining--;
        return DaysRemaining == 0;
    }
}
