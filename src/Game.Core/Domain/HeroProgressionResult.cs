namespace Game.Core.Domain;

/// <summary>
/// Итог начисления опыта герою: сколько уровней получено и какие
/// характеристики выросли. Мягкий предел сценария останавливает рост,
/// но накопленный опыт сохраняется.
/// </summary>
public sealed class HeroProgressionResult
{
    /// <summary>
    /// Создаёт результат начисления опыта.
    /// </summary>
    /// <param name="gainedStats">Характеристики, выросшие за все новые уровни.</param>
    /// <param name="skillPointsGained">Сколько очков навыка начислено.</param>
    /// <param name="stoppedBySoftCap">Остановился ли рост на мягком пределе сценария.</param>
    /// <exception cref="ArgumentNullException">Список характеристик не задан.</exception>
    public HeroProgressionResult(
        IReadOnlyList<HeroStatKind> gainedStats,
        int skillPointsGained,
        bool stoppedBySoftCap)
    {
        ArgumentNullException.ThrowIfNull(gainedStats);
        GainedStats = gainedStats;
        SkillPointsGained = skillPointsGained;
        StoppedBySoftCap = stoppedBySoftCap;
    }

    /// <summary>Характеристики, выросшие за все новые уровни, в порядке повышений.</summary>
    public IReadOnlyList<HeroStatKind> GainedStats { get; }

    /// <summary>Количество новых уровней.</summary>
    public int LevelsGained => GainedStats.Count;

    /// <summary>Сколько очков навыка начислено.</summary>
    public int SkillPointsGained { get; }

    /// <summary>Остановился ли рост из-за мягкого предела сценария.</summary>
    public bool StoppedBySoftCap { get; }
}
