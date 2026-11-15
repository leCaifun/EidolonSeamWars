namespace Game.Core.Content.Dto;

/// <summary>Шаг улучшения поселения.</summary>
public sealed class SettlementUpgradeDto
{
    /// <summary>Исходный уровень.</summary>
    public int FromLevel { get; init; }

    /// <summary>Целевой уровень.</summary>
    public int ToLevel { get; init; }

    /// <summary>Стоимость улучшения в ресурсах.</summary>
    public IReadOnlyDictionary<string, int> Cost { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Длительность улучшения в днях.</summary>
    public int Days { get; init; }
}
