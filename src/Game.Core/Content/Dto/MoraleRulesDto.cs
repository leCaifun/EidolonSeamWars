namespace Game.Core.Content.Dto;

/// <summary>Правила морали из <c>rules/morale.json</c>.</summary>
public sealed class MoraleRulesDto
{
    /// <summary>Версия схемы.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>ID набора правил.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Минимальный уровень морали.</summary>
    public int MinLevel { get; init; }

    /// <summary>Максимальный уровень морали.</summary>
    public int MaxLevel { get; init; }

    /// <summary>Кап шанса дополнительного действия, %.</summary>
    public int ExtraActionChanceCapPercent { get; init; }

    /// <summary>Кап модификатора характеристик, %.</summary>
    public int StatModifierCapPercent { get; init; }

    /// <summary>Максимум дополнительных действий на юнита за раунд.</summary>
    public int MaxExtraActionsPerUnitPerRound { get; init; }

    /// <summary>Теги, иммунные к морали.</summary>
    public IReadOnlyList<string> ImmuneTags { get; init; } = Array.Empty<string>();

    /// <summary>Уровни морали.</summary>
    public IReadOnlyList<MoraleLevelDto> Levels { get; init; } = Array.Empty<MoraleLevelDto>();

    /// <summary>Источники изменения морали.</summary>
    public MoraleSourcesDto Sources { get; init; } = new();
}
