namespace Game.Core.Domain;

/// <summary>
/// Правила бесплатного ветеранства 1–5 (ADR-014, GDD §8.1).
/// Ранг определяется числом побеждённых отрядов, а не уроном.
/// </summary>
public static class VeterancyRules
{
    /// <summary>Максимальный ранг ветеранства.</summary>
    public const int MaxRank = 5;

    /// <summary>Пороги побеждённых отрядов для рангов 1…5.</summary>
    private static readonly int[] Thresholds = { 0, 2, 5, 10, 18 };

    /// <summary>
    /// Возвращает ранг 1…5 по числу побеждённых отрядов.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Отрицательное число побеждённых отрядов.</exception>
    public static int RankForKilledStacks(int killedStacks)
    {
        if (killedStacks < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(killedStacks),
                killedStacks,
                "Число побеждённых отрядов не может быть отрицательным.");
        }

        int rank = 1;
        for (int i = 1; i < Thresholds.Length; i++)
        {
            if (killedStacks >= Thresholds[i])
            {
                rank = i + 1;
            }
        }

        return rank;
    }

    /// <summary>Порог побеждённых отрядов для указанного ранга.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Ранг вне 1…5.</exception>
    public static int ThresholdForRank(int rank)
    {
        if (rank < 1 || rank > MaxRank)
        {
            throw new ArgumentOutOfRangeException(nameof(rank), rank, $"Ранг должен быть в диапазоне 1…{MaxRank}.");
        }

        return Thresholds[rank - 1];
    }

    /// <summary>Бонус атаки за ранг: +2 за каждый ранг выше первого.</summary>
    public static int AttackBonus(int rank)
    {
        return (ValidateRank(rank) - 1) * 2;
    }

    /// <summary>Бонус защиты за ранг: +2 за каждый ранг выше первого.</summary>
    public static int DefenseBonus(int rank)
    {
        return (ValidateRank(rank) - 1) * 2;
    }

    /// <summary>Бонус крит. шанса в п.п.: +1 за каждый ранг выше первого.</summary>
    public static int CritChanceBonus(int rank)
    {
        return ValidateRank(rank) - 1;
    }

    /// <summary>Бонус крит. урона в п.п.: +1 за каждый ранг выше первого.</summary>
    public static int CritDamageBonus(int rank)
    {
        return ValidateRank(rank) - 1;
    }

    /// <summary>
    /// Ключ локализации описательного названия ранга. Русские названия
    /// хранятся в JSON-локализации, а не в коде (инвариант 9).
    /// </summary>
    public static string NameKey(int rank)
    {
        return $"core.veterancy.rank.{ValidateRank(rank)}";
    }

    private static int ValidateRank(int rank)
    {
        if (rank < 1 || rank > MaxRank)
        {
            throw new ArgumentOutOfRangeException(nameof(rank), rank, $"Ранг должен быть в диапазоне 1…{MaxRank}.");
        }

        return rank;
    }
}
