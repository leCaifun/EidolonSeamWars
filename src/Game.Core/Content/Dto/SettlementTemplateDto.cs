namespace Game.Core.Content.Dto;

/// <summary>Шаблон поселения фракции.</summary>
public sealed class SettlementTemplateDto
{
    /// <summary>Версия схемы записи.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>Стабильный ID шаблона.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>ID фракции-владельца.</summary>
    public string FactionId { get; init; } = string.Empty;

    /// <summary>Ключ локализации названия.</summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>Ключ локализации описания.</summary>
    public string DescriptionKey { get; init; } = string.Empty;

    /// <summary>Максимальный уровень, 2…4.</summary>
    public int MaxLevel { get; init; }

    /// <summary>Уровни и слоты.</summary>
    public IReadOnlyList<SettlementLevelDto> Levels { get; init; } =
        Array.Empty<SettlementLevelDto>();

    /// <summary>ID зданий по умолчанию.</summary>
    public IReadOnlyList<string> DefaultBuildings { get; init; } = Array.Empty<string>();

    /// <summary>Шаги улучшения.</summary>
    public IReadOnlyList<SettlementUpgradeDto> Upgrades { get; init; } =
        Array.Empty<SettlementUpgradeDto>();
}
