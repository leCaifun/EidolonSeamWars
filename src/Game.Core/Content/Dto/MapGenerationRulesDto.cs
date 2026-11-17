namespace Game.Core.Content.Dto;

/// <summary>
/// Параметры процедурной генерации карты из <c>rules/map_generation.json</c>.
/// Один мастер-сид вместе с этими данными полностью определяет карту.
/// </summary>
public sealed class MapGenerationRulesDto
{
    /// <summary>Версия схемы.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>ID набора правил.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Ширина сетки в гексах.</summary>
    public int GridWidth { get; init; }

    /// <summary>Высота сетки в гексах.</summary>
    public int GridHeight { get; init; }

    /// <summary>Схема offset-координат: <c>odd-r</c> или <c>even-r</c>.</summary>
    public string OffsetType { get; init; } = string.Empty;

    /// <summary>ID биома, которым заполняется карта по умолчанию.</summary>
    public string DefaultBiome { get; init; } = string.Empty;

    /// <summary>Веса распределения биомов в процентах; сумма обязана быть 100.</summary>
    /// <summary>
    /// ID биома граничного кольца карты (бордюр «Шов»); пусто — без бордюра.
    /// Биом непроходим и не входит в <see cref="BiomeDistribution"/>.
    /// </summary>
    public string BorderBiome { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, int> BiomeDistribution { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Правила размещения стартовых позиций.</summary>
    public PlayerStartRulesDto PlayerStartPositions { get; init; } = new();

    /// <summary>Количество объектов каждого типа.</summary>
    public MapObjectCountsDto ObjectCounts { get; init; } = new();

    /// <summary>Очки движения отряда без героя за день.</summary>
    public int ArmyMovementPointsPerDay { get; init; }

    /// <summary>Базовый радиус видимости партии.</summary>
    public int FogVisibilityRadius { get; init; }

    /// <summary>Радиус исследованной области вокруг партии.</summary>
    public int FogExploredRadius { get; init; }
}
