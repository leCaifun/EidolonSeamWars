namespace Game.Core.Content.Dto;

/// <summary>Правила ветеранства из <c>rules/veterancy.json</c> (ADR-014).</summary>
public sealed class VeterancyRulesDto
{
    /// <summary>Версия схемы.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>ID набора правил.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Считаются ли побеждённые отряды.</summary>
    public bool CountsDefeatedStacks { get; init; }

    /// <summary>Пороги рангов 1…5.</summary>
    public IReadOnlyList<int> Thresholds { get; init; } = Array.Empty<int>();

    /// <summary>Ключи локализации названий рангов.</summary>
    public IReadOnlyList<string> RankNameKeys { get; init; } = Array.Empty<string>();

    /// <summary>Бонусы за ранг выше первого.</summary>
    public VeterancyRankBonusesDto PerRankAboveFirst { get; init; } = new();
}
