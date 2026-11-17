using System.Collections.ObjectModel;
using Game.Core.Content.Dto;
using Game.Core.Domain;

namespace Game.Core.Map;

/// <summary>
/// Иммутабельные параметры генерации глобальной карты, полученные из
/// <c>rules/map_generation.json</c>. Один мастер-сид вместе с этими параметрами
/// полностью определяет карту (ADR-027).
/// </summary>
public sealed class MapGenerationSettings
{
    private readonly ReadOnlyDictionary<string, int> _biomeDistribution;

    private MapGenerationSettings(
        int width,
        int height,
        HexOffsetLayout layout,
        string defaultBiomeId,
        string borderBiomeId,
        IReadOnlyDictionary<string, int> biomeDistribution,
        int minDistanceBetweenPlayers,
        int distanceFromBorder,
        int mineCount,
        int dwellingCount,
        int echoCacheCount,
        int patrolCount,
        int armyMovementPointsPerDay,
        int fogVisibilityRadius,
        int fogExploredRadius)
    {
        Width = width;
        Height = height;
        Layout = layout;
        DefaultBiomeId = defaultBiomeId;
        BorderBiomeId = borderBiomeId;
        _biomeDistribution = new ReadOnlyDictionary<string, int>(
            new Dictionary<string, int>(biomeDistribution, StringComparer.Ordinal));
        MinDistanceBetweenPlayers = minDistanceBetweenPlayers;
        DistanceFromBorder = distanceFromBorder;
        MineCount = mineCount;
        DwellingCount = dwellingCount;
        EchoCacheCount = echoCacheCount;
        PatrolCount = patrolCount;
        ArmyMovementPointsPerDay = armyMovementPointsPerDay;
        FogVisibilityRadius = fogVisibilityRadius;
        FogExploredRadius = fogExploredRadius;
    }

    /// <summary>Ширина сетки в гексах.</summary>
    public int Width { get; }

    /// <summary>Высота сетки в гексах.</summary>
    public int Height { get; }

    /// <summary>Схема offset-координат.</summary>
    public HexOffsetLayout Layout { get; }

    /// <summary>ID биома, которым заполняется карта по умолчанию.</summary>
    public string DefaultBiomeId { get; }

    /// <summary>ID биома граничного кольца («Шов»); пусто — карта без бордюра.</summary>
    public string BorderBiomeId { get; }

    /// <summary>Веса распределения биомов в процентах.</summary>
    public IReadOnlyDictionary<string, int> BiomeDistribution => _biomeDistribution;

    /// <summary>Минимальное расстояние между стартами игроков.</summary>
    public int MinDistanceBetweenPlayers { get; }

    /// <summary>Минимальное расстояние от старта до края карты.</summary>
    public int DistanceFromBorder { get; }

    /// <summary>Количество шахт при генерации.</summary>
    public int MineCount { get; }

    /// <summary>Количество жилищ при генерации.</summary>
    public int DwellingCount { get; }

    /// <summary>Количество эхо-схронов при генерации.</summary>
    public int EchoCacheCount { get; }

    /// <summary>Количество патрулей при генерации.</summary>
    public int PatrolCount { get; }

    /// <summary>Очки движения отряда без героя за день.</summary>
    public int ArmyMovementPointsPerDay { get; }

    /// <summary>Базовый радиус видимости партии.</summary>
    public int FogVisibilityRadius { get; }

    /// <summary>Радиус исследованной области вокруг партии.</summary>
    public int FogExploredRadius { get; }

    /// <summary>
    /// Создаёт параметры из DTO правил генерации.
    /// </summary>
    /// <param name="dto">Правила из контента.</param>
    /// <exception cref="ArgumentNullException">DTO не задан.</exception>
    /// <exception cref="ArgumentException">Параметры непригодны для генерации.</exception>
    public static MapGenerationSettings FromDto(MapGenerationRulesDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.GridWidth <= 0 || dto.GridHeight <= 0)
        {
            throw new ArgumentException("Размер сетки генерации должен быть положительным.", nameof(dto));
        }

        if (string.IsNullOrWhiteSpace(dto.DefaultBiome))
        {
            throw new ArgumentException("Биом по умолчанию не задан.", nameof(dto));
        }

        if (dto.BiomeDistribution.Count == 0)
        {
            throw new ArgumentException("Распределение биомов пусто.", nameof(dto));
        }

        foreach (KeyValuePair<string, int> pair in dto.BiomeDistribution)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new ArgumentException("ID биома в распределении пуст.", nameof(dto));
            }

            if (pair.Value <= 0)
            {
                throw new ArgumentException(
                    $"Вес биома «{pair.Key}» должен быть положительным.",
                    nameof(dto));
            }
        }

        if (!dto.BiomeDistribution.ContainsKey(dto.DefaultBiome))
        {
            throw new ArgumentException(
                $"Биом по умолчанию «{dto.DefaultBiome}» отсутствует в распределении.",
                nameof(dto));
        }

        if (dto.PlayerStartPositions.DistanceFromBorder < 0)
        {
            throw new ArgumentException("Расстояние от старта до края не может быть отрицательным.", nameof(dto));
        }

        if (dto.ArmyMovementPointsPerDay <= 0)
        {
            throw new ArgumentException("Очки движения за день должны быть положительными.", nameof(dto));
        }

        if (dto.FogVisibilityRadius < 0 || dto.FogExploredRadius < 0)
        {
            throw new ArgumentException("Радиусы тумана не могут быть отрицательными.", nameof(dto));
        }

        return new MapGenerationSettings(
            dto.GridWidth,
            dto.GridHeight,
            ParseLayout(dto.OffsetType),
            dto.DefaultBiome,
            dto.BorderBiome ?? string.Empty,
            dto.BiomeDistribution,
            dto.PlayerStartPositions.MinDistanceBetweenPlayers,
            dto.PlayerStartPositions.DistanceFromBorder,
            dto.ObjectCounts.Mines,
            dto.ObjectCounts.Dwellings,
            dto.ObjectCounts.EchoCaches,
            dto.ObjectCounts.Patrols,
            dto.ArmyMovementPointsPerDay,
            dto.FogVisibilityRadius,
            dto.FogExploredRadius);
    }

    /// <summary>
    /// Разбирает строковый токен схемы offset-координат.
    /// </summary>
    /// <param name="value">Значение <c>odd-r</c> или <c>even-r</c>.</param>
    /// <exception cref="ArgumentException">Неизвестная схема.</exception>
    public static HexOffsetLayout ParseLayout(string value)
    {
        return value switch
        {
            "odd-r" => HexOffsetLayout.OddR,
            "even-r" => HexOffsetLayout.EvenR,
            _ => throw new ArgumentException($"Неизвестная схема offset-координат «{value}».", nameof(value))
        };
    }
}
