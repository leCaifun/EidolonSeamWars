namespace Game.Core.Content.Dto;

/// <summary>Правила прогрессии героя из <c>rules/progression.json</c> (ADR-025).</summary>
public sealed class ProgressionRulesDto
{
    /// <summary>Версия схемы.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>ID набора правил.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Не ограничена ли формула уровнем.</summary>
    public bool UnlimitedLevel { get; init; }

    /// <summary>Формула опыта до следующего уровня.</summary>
    public ProgressionFormulaDto XpToNextFormula { get; init; } = new();

    /// <summary>Очков навыка за уровень.</summary>
    public int SkillPointsPerLevel { get; init; }

    /// <summary>Мягкий предел уровня сценария; 0 — без предела.</summary>
    public int ScenarioSoftCap { get; init; }
}
