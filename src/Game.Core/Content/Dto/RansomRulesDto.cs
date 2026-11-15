namespace Game.Core.Content.Dto;

/// <summary>Правила выкупа из <c>rules/ransom.json</c> (ADR-012).</summary>
public sealed class RansomRulesDto
{
    /// <summary>Версия схемы.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>ID набора правил.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Доля цены юнитов, %.</summary>
    public int UnitPriceMultiplierPercent { get; init; }

    /// <summary>Стоимость за уровень героя.</summary>
    public int HeroLevelCost { get; init; }

    /// <summary>Стоимость за артефакт.</summary>
    public int ArtifactCost { get; init; }

    /// <summary>Сохраняется ли армия.</summary>
    public bool KeepsArmy { get; init; }

    /// <summary>Сохраняются ли артефакты.</summary>
    public bool KeepsArtifacts { get; init; }

    /// <summary>Очки движения после выкупа.</summary>
    public int MovePointsAfter { get; init; }

    /// <summary>Может ли защитник осады выкупиться.</summary>
    public bool DefenderCanRansom { get; init; }
}
