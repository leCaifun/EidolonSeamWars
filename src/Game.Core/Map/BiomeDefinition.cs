namespace Game.Core.Map;

/// <summary>
/// Иммутабельное описание биома: проходимость, стоимость входа, обзор и
/// признак дороги. Все значения приходят из <c>rules/biomes.json</c>;
/// код не содержит имён конкретных биомов (AGENTS.md, инвариант 3).
/// </summary>
public sealed class BiomeDefinition
{
    /// <summary>
    /// Создаёт описание биома.
    /// </summary>
    /// <exception cref="ArgumentException">Пустой ID, ключ или имя тайла.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Отрицательная стоимость или обзор.</exception>
    public BiomeDefinition(
        string id,
        string nameKey,
        string tileId,
        bool passable,
        int movementCostPerHex,
        int visibilityRange,
        bool flyerPassable = false,
        int flyerMovementCost = 0,
        bool road = false)
    {
        Id = RequireText(id, nameof(id));
        NameKey = RequireText(nameKey, nameof(nameKey));
        TileId = RequireText(tileId, nameof(tileId));

        if (movementCostPerHex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(movementCostPerHex),
                movementCostPerHex,
                "Стоимость входа не может быть отрицательной.");
        }

        if (visibilityRange < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(visibilityRange),
                visibilityRange,
                "Радиус обзора не может быть отрицательным.");
        }

        if (flyerMovementCost < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(flyerMovementCost),
                flyerMovementCost,
                "Стоимость входа летунов не может быть отрицательной.");
        }

        Passable = passable;
        MovementCostPerHex = movementCostPerHex;
        VisibilityRange = visibilityRange;
        FlyerPassable = flyerPassable;
        FlyerMovementCost = flyerMovementCost;
        Road = road;
    }

    /// <summary>Стабильный ID биома.</summary>
    public string Id { get; }

    /// <summary>Ключ локализации названия.</summary>
    public string NameKey { get; }

    /// <summary>Имя тайла для рендера, например <c>hex_grass</c>.</summary>
    public string TileId { get; }

    /// <summary>Проходим ли биом для наземных отрядов.</summary>
    public bool Passable { get; }

    /// <summary>Стоимость входа на гекс в очках движения.</summary>
    public int MovementCostPerHex { get; }

    /// <summary>Радиус обзора с гекса этого биома.</summary>
    public int VisibilityRange { get; }

    /// <summary>Проходим ли биом для летающих отрядов.</summary>
    public bool FlyerPassable { get; }

    /// <summary>Стоимость входа для летающих отрядов.</summary>
    public int FlyerMovementCost { get; }

    /// <summary>
    /// Является ли биом дорогой. Стоимость дороги — 1 очко движения (ADR-034).
    /// </summary>
    public bool Road { get; }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Строковое поле биома не может быть пустым.", parameterName);
        }

        return value;
    }
}
