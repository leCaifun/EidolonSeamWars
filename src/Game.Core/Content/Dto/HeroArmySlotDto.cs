namespace Game.Core.Content.Dto;

/// <summary>Слот стартовой армии героя.</summary>
public sealed class HeroArmySlotDto
{
    /// <summary>ID юнита.</summary>
    public string UnitId { get; init; } = string.Empty;

    /// <summary>Количество юнитов.</summary>
    public int Count { get; init; }
}
