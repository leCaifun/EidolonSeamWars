using Game.Core.Diagnostics;
using Game.Core.Domain;
using Game.Core.Rng;

namespace Game.Core.Map;

/// <summary>
/// Детерминированный генератор глобальной карты (ADR-027). Использует только
/// стрим <see cref="RngStreamKind.Map"/>, фиксированный порядок гексов и
/// стабильную сортировку определений объектов, поэтому один мастер-сид всегда
/// даёт одну и ту же карту.
/// </summary>
public sealed class MapGenerator
{
    private const string SettlementType = "settlement";
    private const string MineType = "mine";
    private const string DwellingType = "dwelling";
    private const string EchoCacheType = "echoCache";
    private const string PatrolType = "patrol";

    private readonly BiomeCatalog _biomes;
    private readonly MapObjectCatalog _objects;
    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт генератор.
    /// </summary>
    /// <param name="biomes">Справочник биомов.</param>
    /// <param name="objects">Справочник объектов карты.</param>
    /// <param name="logger">Необязательный журнал доменных операций.</param>
    /// <exception cref="ArgumentNullException">Справочник не задан.</exception>
    public MapGenerator(BiomeCatalog biomes, MapObjectCatalog objects, IGameLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(biomes);
        ArgumentNullException.ThrowIfNull(objects);

        _biomes = biomes;
        _objects = objects;
        _logger = logger;
    }

    /// <summary>
    /// Генерирует карту по запросу.
    /// </summary>
    /// <param name="request">Мастер-сид, параметры и стартовые позиции.</param>
    /// <exception cref="ArgumentNullException">Запрос не задан.</exception>
    /// <exception cref="ArgumentException">Старт вне сетки или повторяется.</exception>
    /// <exception cref="InvalidOperationException">Контента недостаточно для размещения объектов.</exception>
    public MapLayoutDefinition Generate(MapStartRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        MapGenerationSettings settings = request.Settings;
        HexGrid grid = new(settings.Width, settings.Height, settings.Layout);
        SeededRandom rng = new RngStreams(request.MasterSeed).Get(RngStreamKind.Map);

        KeyValuePair<string, int>[] weighted = settings.BiomeDistribution
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToArray();
        int totalWeight = 0;

        foreach (KeyValuePair<string, int> pair in weighted)
        {
            totalWeight = checked(totalWeight + pair.Value);
        }

        Dictionary<Hex, string> biomeByHex = new();

        foreach (Hex hex in grid.AllHexes())
        {
            biomeByHex[hex] = PickBiome(rng, weighted, totalWeight);
        }

        HashSet<Hex> occupied = new();
        List<MapLayoutObject> objects = new();
        IReadOnlyList<MapObjectDefinition> settlements = _objects.ByType(SettlementType);

        foreach (MapStartDefinition start in request.Starts)
        {
            if (!grid.Contains(start.Position))
            {
                throw new ArgumentException(
                    $"Старт «{start.FactionId}» вне сетки: {start.Position}.",
                    nameof(request));
            }

            if (!occupied.Add(start.Position))
            {
                throw new ArgumentException(
                    $"Повторный стартовый гекс: {start.Position}.",
                    nameof(request));
            }

            biomeByHex[start.Position] = settings.DefaultBiomeId;

            MapObjectDefinition? settlement = settlements
                .FirstOrDefault(definition => string.Equals(
                    definition.FactionId,
                    start.FactionId,
                    StringComparison.Ordinal));

            if (settlement is null)
            {
                _logger?.Log(LogEvent.Create(
                    LogLevel.Warning,
                    "Map",
                    $"Для фракции «{start.FactionId}» нет объекта поселения; старт без поселения.",
                    fields: new Dictionary<string, string>
                    {
                        ["faction"] = start.FactionId
                    }));

                continue;
            }

            objects.Add(new MapLayoutObject(
                "gen.settlement." + IdSuffix(start.FactionId),
                settlement.Id,
                start.Position,
                start.FactionId));
        }

        List<Hex> candidates = new();

        foreach (Hex hex in grid.AllHexes())
        {
            if (!occupied.Contains(hex) && _biomes.Get(biomeByHex[hex]).Passable)
            {
                candidates.Add(hex);
            }
        }

        PlaceType(rng, MineType, "mine", settings.MineCount, candidates, objects);
        PlaceType(rng, DwellingType, "dwelling", settings.DwellingCount, candidates, objects);
        PlaceType(rng, EchoCacheType, "cache", settings.EchoCacheCount, candidates, objects);
        PlaceType(rng, PatrolType, "patrol", settings.PatrolCount, candidates, objects);

        List<MapTile> tiles = new(grid.HexCount);

        foreach (Hex hex in grid.AllHexes())
        {
            tiles.Add(new MapTile(hex, biomeByHex[hex]));
        }

        _logger?.Log(LogEvent.Create(
            LogLevel.Info,
            "Map",
            $"Карта сгенерирована: сид {request.MasterSeed}, гексов {tiles.Count}, объектов {objects.Count}.",
            fields: new Dictionary<string, string>
            {
                ["seed"] = request.MasterSeed.ToString(),
                ["tiles"] = tiles.Count.ToString(),
                ["objects"] = objects.Count.ToString()
            }));

        return new MapLayoutDefinition(
            $"generated.{request.MasterSeed:x16}",
            "map.generated.name",
            settings.Width,
            settings.Height,
            settings.Layout,
            tiles,
            objects,
            request.Starts);
    }

    /// <summary>
    /// Создаёт стартовые позиции по умолчанию: игрок в левом нижнем углу,
    /// противник в правом верхнем, нейтральная сторона в центре. Отступ от края
    /// берётся из правил генерации. Герои не назначаются.
    /// </summary>
    /// <param name="settings">Параметры генерации.</param>
    /// <param name="factionIds">ID фракций в порядке сторон; от одной до трёх.</param>
    /// <exception cref="ArgumentNullException">Параметры или список фракций не заданы.</exception>
    /// <exception cref="ArgumentException">Список фракций пуст или слишком длинный.</exception>
    /// <exception cref="InvalidOperationException">Сетка слишком мала для стартов.</exception>
    public static IReadOnlyList<MapStartDefinition> CreateDefaultStarts(
        MapGenerationSettings settings,
        IReadOnlyList<string> factionIds)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(factionIds);

        if (factionIds.Count == 0)
        {
            throw new ArgumentException("Нужен хотя бы один ID фракции.", nameof(factionIds));
        }

        if (factionIds.Count > 3)
        {
            throw new ArgumentException("Поддерживается не больше трёх сторон.", nameof(factionIds));
        }

        int margin = settings.DistanceFromBorder;
        int width = settings.Width;
        int height = settings.Height;

        if (margin * 2 >= width || margin * 2 >= height)
        {
            throw new InvalidOperationException(
                $"Сетка {width}×{height} слишком мала для отступа {margin}.");
        }

        HexGrid grid = new(width, height, settings.Layout);
        Hex[] anchors =
        {
            grid.HexAt(margin, height - 1 - margin),
            grid.HexAt(width - 1 - margin, margin),
            grid.HexAt(width / 2, height / 2)
        };

        for (int i = 0; i < anchors.Length; i++)
        {
            for (int j = 0; j < i; j++)
            {
                if (anchors[i] == anchors[j])
                {
                    throw new InvalidOperationException(
                        $"Стартовые гексы совпали: {anchors[i]}.");
                }
            }
        }

        List<MapStartDefinition> starts = new(factionIds.Count);

        for (int i = 0; i < factionIds.Count; i++)
        {
            PartyRole role = i switch
            {
                0 => PartyRole.Player,
                1 => PartyRole.Enemy,
                _ => PartyRole.Neutral
            };

            starts.Add(new MapStartDefinition(role, factionIds[i], null, anchors[i]));
        }

        return starts;
    }

    private void PlaceType(
        SeededRandom rng,
        string type,
        string idToken,
        int count,
        List<Hex> candidates,
        List<MapLayoutObject> objects)
    {
        if (count <= 0)
        {
            return;
        }

        IReadOnlyList<MapObjectDefinition> pool = _objects.ByType(type);

        if (pool.Count == 0)
        {
            throw new InvalidOperationException(
                $"Нет определений объектов типа «{type}», а требуется {count}.");
        }

        for (int i = 0; i < count; i++)
        {
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Недостаточно проходимых гексов для размещения объектов типа «{type}».");
            }

            int index = rng.Next(candidates.Count);
            Hex hex = candidates[index];
            candidates[index] = candidates[^1];
            candidates.RemoveAt(candidates.Count - 1);

            MapObjectDefinition definition = pool[i % pool.Count];
            objects.Add(new MapLayoutObject(
                $"gen.{idToken}.{i}",
                definition.Id,
                hex,
                definition.FactionId));
        }
    }

    private static string PickBiome(
        SeededRandom rng,
        IReadOnlyList<KeyValuePair<string, int>> weighted,
        int totalWeight)
    {
        int roll = rng.Next(totalWeight);
        int accumulated = 0;

        foreach (KeyValuePair<string, int> pair in weighted)
        {
            accumulated += pair.Value;

            if (roll < accumulated)
            {
                return pair.Key;
            }
        }

        return weighted[^1].Key;
    }

    private static string IdSuffix(string id)
    {
        int index = id.LastIndexOf('.');
        return index >= 0 && index + 1 < id.Length ? id[(index + 1)..] : id;
    }
}
