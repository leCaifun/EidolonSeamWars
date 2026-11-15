namespace Game.Core.Content.Dto;

/// <summary>Правила побега из <c>rules/escape.json</c> (ADR-012).</summary>
public sealed class EscapeRulesDto
{
    /// <summary>Версия схемы.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>ID набора правил.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Базовый шанс побега, %.</summary>
    public int BaseChancePercent { get; init; }

    /// <summary>Множитель логистики героя.</summary>
    public int HeroLogisticsMultiplier { get; init; }

    /// <summary>Множитель логистики противника.</summary>
    public int EnemyLogisticsMultiplier { get; init; }

    /// <summary>Минимальный шанс побега, %.</summary>
    public int MinChancePercent { get; init; }

    /// <summary>Максимальный шанс побега, %.</summary>
    public int MaxChancePercent { get; init; }

    /// <summary>Минимум теряемых артефактов.</summary>
    public int ArtifactsLostMin { get; init; }

    /// <summary>Максимум теряемых артефактов.</summary>
    public int ArtifactsLostMax { get; init; }

    /// <summary>Длительность плена, дни.</summary>
    public int CaptivityDays { get; init; }

    /// <summary>Теряется ли армия при побеге.</summary>
    public bool ArmyLostOnEscape { get; init; }

    /// <summary>Может ли защитник осады бежать.</summary>
    public bool DefenderCanEscape { get; init; }

    /// <summary>Доля потерь отряда без героя, %.</summary>
    public int HerolessArmyLossPercent { get; init; }
}
