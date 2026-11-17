namespace Game.Core.Content.Dto;

/// <summary>
/// Описание биома из <c>rules/biomes.json</c>. Биом задаёт проходимость,
/// стоимость входа на гекс и радиус обзора, но не содержит игровой логики.
/// </summary>
public sealed class BiomeDto
{
    /// <summary>Стабильный ID биома.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Ключ локализации названия.</summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>Имя тайла для рендера, например <c>hex_grass</c>.</summary>
    public string TileId { get; init; } = string.Empty;

    /// <summary>Проходим ли биом для наземных отрядов.</summary>
    public bool Passable { get; init; }

    /// <summary>Стоимость входа на гекс в очках движения.</summary>
    public int MovementCostPerHex { get; init; }

    /// <summary>Радиус обзора с гекса этого биома.</summary>
    public int VisibilityRange { get; init; }

    /// <summary>Проходим ли биом для летающих отрядов.</summary>
    public bool FlyerPassable { get; init; }

    /// <summary>Стоимость входа для летающих отрядов.</summary>
    public int FlyerMovementCost { get; init; }

    /// <summary>
    /// Является ли биом дорогой. Стоимость дороги — 1 очко движения (ADR-034).
    /// </summary>
    public bool Road { get; init; }
}
