namespace Game.Core.Content.Dto;

/// <summary>Правила размещения стартовых позиций игроков.</summary>
public sealed class PlayerStartRulesDto
{
    /// <summary>Минимальное гексагональное расстояние между стартами игроков.</summary>
    public int MinDistanceBetweenPlayers { get; init; }

    /// <summary>Минимальное расстояние от старта до края карты.</summary>
    public int DistanceFromBorder { get; init; }
}
