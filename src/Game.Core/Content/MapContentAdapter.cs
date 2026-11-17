using Game.Core.Content.Dto;
using Game.Core.Domain;
using Game.Core.Map;

namespace Game.Core.Content;

/// <summary>
/// Мост между контентным слоем и доменом карты. Преобразует DTO биомов,
/// объектов и рукотворных карт в иммутабельные доменные типы. Прямой файловый
/// ввод-вывод здесь отсутствует: данные уже загружены <see cref="ContentLoader"/>.
/// </summary>
public static class MapContentAdapter
{
    /// <summary>
    /// Создаёт справочник биомов из реестра контента.
    /// </summary>
    /// <exception cref="ArgumentNullException">Реестр не задан.</exception>
    /// <exception cref="InvalidOperationException">Правила биомов отсутствуют.</exception>
    public static BiomeCatalog CreateBiomes(ContentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        if (registry.BiomesRules is null)
        {
            throw new InvalidOperationException("В контенте отсутствуют правила биомов.");
        }

        List<BiomeDefinition> biomes = new();

        foreach (ContentEntry<BiomeDto> entry in registry.Biomes.Values.OrderBy(
            item => item.Value.Id,
            StringComparer.Ordinal))
        {
            BiomeDto dto = entry.Value;

            biomes.Add(new BiomeDefinition(
                dto.Id,
                dto.NameKey,
                dto.TileId,
                dto.Passable,
                dto.MovementCostPerHex,
                dto.VisibilityRange,
                dto.FlyerPassable,
                dto.FlyerMovementCost,
                dto.Road));
        }

        return new BiomeCatalog(biomes);
    }

    /// <summary>
    /// Создаёт справочник объектов карты из реестра контента.
    /// </summary>
    /// <exception cref="ArgumentNullException">Реестр не задан.</exception>
    public static MapObjectCatalog CreateMapObjects(ContentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        List<MapObjectDefinition> definitions = new();

        foreach (ContentEntry<MapObjectDto> entry in registry.MapObjects.Values.OrderBy(
            item => item.Value.Id,
            StringComparer.Ordinal))
        {
            MapObjectDto dto = entry.Value;

            definitions.Add(new MapObjectDefinition(
                dto.Id,
                dto.Type,
                dto.NameKey,
                dto.DescriptionKey,
                dto.Guarded,
                dto.FactionId,
                dto.TemplateId,
                dto.InitialLevel,
                dto.IncomePerDay));
        }

        return new MapObjectCatalog(definitions);
    }

    /// <summary>
    /// Создаёт параметры генерации из правил контента.
    /// </summary>
    /// <exception cref="ArgumentNullException">Реестр не задан.</exception>
    /// <exception cref="InvalidOperationException">Правила генерации отсутствуют.</exception>
    public static MapGenerationSettings CreateSettings(ContentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        if (registry.MapGenerationRules is null)
        {
            throw new InvalidOperationException("В контенте отсутствуют правила генерации карты.");
        }

        return MapGenerationSettings.FromDto(registry.MapGenerationRules.Value);
    }

    /// <summary>
    /// Преобразует рукотворную карту из контента в доменное представление.
    /// </summary>
    /// <param name="registry">Реестр контента.</param>
    /// <param name="dto">Рукотворная карта.</param>
    /// <exception cref="ArgumentNullException">Аргумент не задан.</exception>
    /// <exception cref="InvalidOperationException">Layout не соответствует размерам или ссылается на неизвестный контент.</exception>
    public static MapLayoutDefinition CreateLayout(ContentRegistry registry, TestMapDto dto)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(dto);

        HexOffsetLayout layoutType = MapGenerationSettings.ParseLayout(dto.OffsetType);

        if (dto.Layout.Count != dto.Height)
        {
            throw new InvalidOperationException(
                $"Карта «{dto.Id}»: строк layout {dto.Layout.Count}, ожидается {dto.Height}.");
        }

        List<MapTile> tiles = new(dto.Width * dto.Height);

        for (int row = 0; row < dto.Height; row++)
        {
            string line = dto.Layout[row];

            if (line.Length != dto.Width)
            {
                throw new InvalidOperationException(
                    $"Карта «{dto.Id}»: строка {row} длиной {line.Length}, ожидается {dto.Width}.");
            }

            for (int column = 0; column < dto.Width; column++)
            {
                string symbol = line[column].ToString();

                if (!dto.Legend.TryGetValue(symbol, out string? biomeId))
                {
                    throw new InvalidOperationException(
                        $"Карта «{dto.Id}»: символ «{symbol}» отсутствует в legend.");
                }

                if (!registry.Biomes.ContainsKey(biomeId))
                {
                    throw new InvalidOperationException(
                        $"Карта «{dto.Id}»: биом «{biomeId}» отсутствует в контенте.");
                }

                Hex hex = Hex.FromOffsetCoordinates(column, row, layoutType);
                tiles.Add(new MapTile(hex, biomeId));
            }
        }

        List<MapLayoutObject> objects = new(dto.Objects.Count);

        for (int i = 0; i < dto.Objects.Count; i++)
        {
            TestMapObjectDto item = dto.Objects[i];

            if (!registry.MapObjects.TryGetValue(item.ObjectId, out ContentEntry<MapObjectDto>? entry))
            {
                throw new InvalidOperationException(
                    $"Карта «{dto.Id}»: объект «{item.ObjectId}» отсутствует в контенте.");
            }

            Hex position = Hex.FromOffsetCoordinates(item.Col, item.Row, layoutType);
            objects.Add(new MapLayoutObject(
                $"{dto.Id}.obj.{i}",
                item.ObjectId,
                position,
                entry.Value.FactionId));
        }

        List<MapStartDefinition> starts = new();

        if (dto.StartPositions?.Player is TestMapStartDto player)
        {
            starts.Add(ToStart(PartyRole.Player, player, layoutType, dto.Id));
        }

        if (dto.StartPositions?.Enemy is TestMapStartDto enemy)
        {
            starts.Add(ToStart(PartyRole.Enemy, enemy, layoutType, dto.Id));
        }

        if (dto.StartPositions?.Neutral is TestMapStartDto neutral)
        {
            starts.Add(ToStart(PartyRole.Neutral, neutral, layoutType, dto.Id));
        }

        return new MapLayoutDefinition(
            dto.Id,
            dto.NameKey,
            dto.Width,
            dto.Height,
            layoutType,
            tiles,
            objects,
            starts);
    }

    /// <summary>
    /// Создаёт карту из представления и реестра контента.
    /// </summary>
    /// <param name="layout">Представление карты.</param>
    /// <param name="registry">Реестр контента.</param>
    /// <exception cref="ArgumentNullException">Аргумент не задан.</exception>
    public static GameMap CreateMap(MapLayoutDefinition layout, ContentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        return CreateMap(layout, CreateBiomes(registry), CreateMapObjects(registry));
    }

    /// <summary>
    /// Создаёт карту из представления и явных справочников.
    /// </summary>
    /// <param name="layout">Представление карты.</param>
    /// <param name="biomes">Справочник биомов.</param>
    /// <param name="objects">Справочник объектов.</param>
    /// <exception cref="ArgumentNullException">Аргумент не задан.</exception>
    /// <exception cref="InvalidOperationException">Объект ссылается на неизвестное определение.</exception>
    public static GameMap CreateMap(
        MapLayoutDefinition layout,
        BiomeCatalog biomes,
        MapObjectCatalog objects)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(biomes);
        ArgumentNullException.ThrowIfNull(objects);

        List<MapObjectInstance> instances = new(layout.Objects.Count);

        foreach (MapLayoutObject item in layout.Objects)
        {
            if (!objects.TryGet(item.DefinitionId, out MapObjectDefinition definition))
            {
                throw new InvalidOperationException(
                    $"Объект «{item.InstanceId}» ссылается на неизвестное определение «{item.DefinitionId}».");
            }

            instances.Add(new MapObjectInstance(
                item.InstanceId,
                definition,
                item.Position,
                item.OwnerFactionId));
        }

        return new GameMap(
            layout.CreateGrid(),
            layout.Tiles,
            instances,
            biomes);
    }

    /// <summary>
    /// Создаёт партии по стартовым позициям карты. Ёмкость героя берётся из
    /// контента, отряд без героя получает ёмкость 50 (ADR-006). Очки движения героя
    /// считаются по формуле GDD §9.2 (ADR-034), отряда без героя — из <paramref name="settings"/>.
    /// </summary>
    /// <param name="layout">Представление карты.</param>
    /// <param name="settings">Параметры движения.</param>
    /// <param name="registry">Реестр контента.</param>
    /// <exception cref="ArgumentNullException">Аргумент не задан.</exception>
    /// <exception cref="InvalidOperationException">Герой отсутствует в контенте.</exception>
    public static IReadOnlyList<Party> CreateParties(
        MapLayoutDefinition layout,
        MapGenerationSettings settings,
        ContentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(registry);

        List<Party> parties = new(layout.Starts.Count);

        foreach (MapStartDefinition start in layout.Starts)
        {
            int capacity;
            int movement;

            if (start.HeroId is null)
            {
                capacity = Army.HeroLessCapacity;
                movement = settings.ArmyMovementPointsPerDay;
            }
            else
            {
                if (!registry.Heroes.TryGetValue(start.HeroId, out ContentEntry<HeroDto>? hero))
                {
                    throw new InvalidOperationException(
                        $"Старт «{start.FactionId}» ссылается на неизвестного героя «{start.HeroId}».");
                }

                capacity = hero.Value.Stats.Leadership;
                // GDD §9.2 / ADR-034: очки движения героя = 12 + Логистика.
                movement = Hero.BaseMovementPoints + hero.Value.Stats.Logistics;
            }

            if (capacity <= 0)
            {
                throw new InvalidOperationException(
                    $"Ёмкость лидерства старта «{start.FactionId}» должна быть положительной.");
            }

            parties.Add(new Party(
                $"party.{start.Role.ToString().ToLowerInvariant()}",
                start.Role,
                start.FactionId,
                start.HeroId,
                capacity,
                start.Position,
                movement));
        }

        return parties;
    }

    private static MapStartDefinition ToStart(
        PartyRole role,
        TestMapStartDto dto,
        HexOffsetLayout layout,
        string mapId)
    {
        if (string.IsNullOrWhiteSpace(dto.FactionId))
        {
            throw new InvalidOperationException($"Карта «{mapId}»: у старта {role} нет factionId.");
        }

        Hex position = Hex.FromOffsetCoordinates(dto.Col, dto.Row, layout);
        return new MapStartDefinition(role, dto.FactionId, dto.HeroId, position);
    }
}
