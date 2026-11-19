namespace Game.Core.Battle;

/// <summary>
/// Действующий на стак длительный эффект (бафф, дебафф, регенерация, укоренение).
/// </summary>
public sealed class ActiveEffect
{
    /// <summary>
    /// Создаёт действующий эффект.
    /// </summary>
    /// <param name="type">Тип эффекта (<see cref="BattleEffectTypes"/>).</param>
    /// <param name="value">Значение.</param>
    /// <param name="remainingRounds">Сколько раундов эффект ещё действует, не меньше 1.</param>
    /// <param name="sourceId">ID источника (стак, герой или способность).</param>
    /// <exception cref="ArgumentOutOfRangeException">Длительность меньше 1.</exception>
    public ActiveEffect(string type, int value, int remainingRounds, string sourceId)
    {
        if (remainingRounds < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(remainingRounds),
                remainingRounds,
                "Длительный эффект должен действовать хотя бы один раунд.");
        }

        Type = type;
        Value = value;
        RemainingRounds = remainingRounds;
        SourceId = sourceId;
    }

    /// <summary>Тип эффекта.</summary>
    public string Type { get; }

    /// <summary>Значение.</summary>
    public int Value { get; }

    /// <summary>Сколько раундов осталось.</summary>
    public int RemainingRounds { get; private set; }

    /// <summary>Источник эффекта.</summary>
    public string SourceId { get; }

    /// <summary>Уменьшает остаток на раунд.</summary>
    /// <returns><c>true</c>, если эффект закончился.</returns>
    public bool Tick()
    {
        RemainingRounds--;
        return RemainingRounds <= 0;
    }
}
