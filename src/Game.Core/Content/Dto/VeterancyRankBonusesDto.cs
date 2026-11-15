namespace Game.Core.Content.Dto;

/// <summary>Бонусы за ранг ветеранства выше первого (ADR-014).</summary>
public sealed class VeterancyRankBonusesDto
{
    /// <summary>Бонус атаки.</summary>
    public int Attack { get; init; }

    /// <summary>Бонус защиты.</summary>
    public int Defense { get; init; }

    /// <summary>Бонус крит. шанса, п. п.</summary>
    public int CritChancePercent { get; init; }

    /// <summary>Бонус крит. урона, п. п.</summary>
    public int CritDamagePercent { get; init; }
}
