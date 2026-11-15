namespace Game.Core.Economy;

/// <summary>
/// Эффект с длительностью в целых днях (GDD §5.2, ADR-004). Дневной тик
/// уменьшает срок и удаляет истёкшие эффекты.
/// </summary>
public sealed class TimedEffect
{
    /// <summary>
    /// Создаёт эффект.
    /// </summary>
    /// <param name="id">Стабильный ID эффекта.</param>
    /// <param name="targetId">ID цели (герой, отряд, поселение).</param>
    /// <param name="daysRemaining">Срок в днях, начиная с 1.</param>
    /// <exception cref="ArgumentException">Пустой ID эффекта или цели.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Срок не положителен.</exception>
    public TimedEffect(string id, string targetId, int daysRemaining)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("ID эффекта не может быть пустым.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("ID цели эффекта не может быть пустым.", nameof(targetId));
        }

        if (daysRemaining <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(daysRemaining), daysRemaining, "Срок эффекта должен быть положительным.");
        }

        Id = id;
        TargetId = targetId;
        DaysRemaining = daysRemaining;
    }

    /// <summary>Стабильный ID эффекта.</summary>
    public string Id { get; }

    /// <summary>ID цели эффекта.</summary>
    public string TargetId { get; }

    /// <summary>Остаток срока в днях.</summary>
    public int DaysRemaining { get; private set; }

    /// <summary>Истёк ли эффект.</summary>
    public bool IsExpired => DaysRemaining == 0;

    /// <summary>
    /// Уменьшает срок на один день.
    /// </summary>
    /// <returns><c>true</c>, если эффект истёк этим вызовом.</returns>
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
