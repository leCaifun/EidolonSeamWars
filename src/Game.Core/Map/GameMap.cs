using System.Collections.ObjectModel;
using Game.Core.Domain;
using Game.Core.Numerics;

namespace Game.Core.Map;

/// <summary>
/// Глобальная карта: сетка, биомы тайлов, объекты и вычисленные проходимость
/// и стоимость входа. Иммутабелен по структуре; изменяемое состояние объектов
/// хранится в <see cref="MapObjectInstance"/> и меняется командами карты.
/// </summary>
public sealed class GameMap
{
    private readonly Dictionary<Hex, MapTile> _tiles;
    private readonly Dictionary<Hex, int> _movementCosts;
    private readonly Dictionary<Hex, List<MapObjectInstance>> _objectsByHex;
    private readonly Dictionary<string, MapObjectInstance> _objectsById;
    private readonly ReadOnlyCollection<MapObjectInstance> _objects;

    /// <summary>
    /// Создаёт карту и вычисляет проходимость и стоимость входа.
    /// </summary>
    /// <param name="grid">Сетка карты.</param>
    /// <param name="tiles">Тайлы карты; по одному на каждый гекс сетки.</param>
    /// <param name="objects">Экземпляры объектов карты.</param>
    /// <param name="biomes">Справочник биомов.</param>
    /// <exception cref="ArgumentNullException">Обязательный аргумент не задан.</exception>
    /// <exception cref="ArgumentException">Неполная сетка или объект вне сетки.</exception>
    public GameMap(
        HexGrid grid,
        IEnumerable<MapTile> tiles,
        IEnumerable<MapObjectInstance> objects,
        BiomeCatalog biomes)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(biomes);

        Grid = grid;
        Biomes = biomes;
        _tiles = new Dictionary<Hex, MapTile>();
        _movementCosts = new Dictionary<Hex, int>();
        _objectsByHex = new Dictionary<Hex, List<MapObjectInstance>>();
        _objectsById = new Dictionary<string, MapObjectInstance>(StringComparer.Ordinal);

        foreach (MapTile tile in tiles)
        {
            if (!grid.Contains(tile.Position))
            {
                throw new ArgumentException($"Тайл вне сетки: {tile.Position}.", nameof(tiles));
            }

            if (!_tiles.TryAdd(tile.Position, tile))
            {
                throw new ArgumentException($"Повторный тайл: {tile.Position}.", nameof(tiles));
            }

            BiomeDefinition biome = biomes.Get(tile.BiomeId);
            _movementCosts[tile.Position] = ComputeCost(biome);
        }

        if (_tiles.Count != grid.HexCount)
        {
            throw new ArgumentException(
                $"Сетка неполна: тайлов {_tiles.Count}, ожидается {grid.HexCount}.",
                nameof(tiles));
        }

        List<MapObjectInstance> objectList = new();

        foreach (MapObjectInstance instance in objects)
        {
            if (!grid.Contains(instance.Position))
            {
                throw new ArgumentException(
                    $"Объект «{instance.InstanceId}» вне сетки: {instance.Position}.",
                    nameof(objects));
            }

            if (!_objectsById.TryAdd(instance.InstanceId, instance))
            {
                throw new ArgumentException(
                    $"Повторный ID экземпляра объекта «{instance.InstanceId}».",
                    nameof(objects));
            }

            if (!_objectsByHex.TryGetValue(instance.Position, out List<MapObjectInstance>? list))
            {
                list = new List<MapObjectInstance>();
                _objectsByHex.Add(instance.Position, list);
            }

            list.Add(instance);
            objectList.Add(instance);
        }

        _objects = new ReadOnlyCollection<MapObjectInstance>(objectList);
    }

    /// <summary>Сетка карты.</summary>
    public HexGrid Grid { get; }

    /// <summary>Справочник биомов, использованный при создании карты.</summary>
    public BiomeCatalog Biomes { get; }

    /// <summary>Все тайлы карты.</summary>
    public IEnumerable<MapTile> Tiles => _tiles.Values;

    /// <summary>Все экземпляры объектов карты в порядке добавления.</summary>
    public IReadOnlyList<MapObjectInstance> Objects => _objects;

    /// <summary>
    /// Возвращает тайл по гексу.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Гекс вне карты.</exception>
    public MapTile GetTile(Hex hex)
    {
        if (!_tiles.TryGetValue(hex, out MapTile tile))
        {
            throw new KeyNotFoundException($"Гекс {hex} отсутствует на карте.");
        }

        return tile;
    }

    /// <summary>
    /// Возвращает описание биома гекса.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Гекс вне карты.</exception>
    public BiomeDefinition GetBiome(Hex hex)
    {
        return Biomes.Get(GetTile(hex).BiomeId);
    }

    /// <summary>Проходим ли гекс для наземных отрядов.</summary>
    public bool IsPassable(Hex hex)
    {
        return _movementCosts.TryGetValue(hex, out int cost) && cost != int.MaxValue;
    }

    /// <summary>
    /// Стоимость входа на гекс. Для непроходимого гекса возвращает
    /// <see cref="int.MaxValue"/>.
    /// </summary>
    public int GetMovementCost(Hex hex)
    {
        return _movementCosts.TryGetValue(hex, out int cost) ? cost : int.MaxValue;
    }

    /// <summary>Объекты, расположенные на гексе.</summary>
    public IReadOnlyList<MapObjectInstance> GetObjectsAt(Hex hex)
    {
        return _objectsByHex.TryGetValue(hex, out List<MapObjectInstance>? list)
            ? list
            : Array.Empty<MapObjectInstance>();
    }

    /// <summary>Находит объект по ID экземпляра или null.</summary>
    public MapObjectInstance? FindObject(string instanceId)
    {
        return instanceId is not null && _objectsById.TryGetValue(instanceId, out MapObjectInstance? instance)
            ? instance
            : null;
    }

    /// <summary>
    /// Возвращает первый блокирующий объект на гексе (патруль или охраняемый
    /// объект) либо null.
    /// </summary>
    public MapObjectInstance? GetBlockingObjectAt(Hex hex)
    {
        if (!_objectsByHex.TryGetValue(hex, out List<MapObjectInstance>? list))
        {
            return null;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Definition.IsBlocking)
            {
                return list[i];
            }
        }

        return null;
    }

    /// <summary>Есть ли на гексе блокирующий объект.</summary>
    public bool HasBlockingObject(Hex hex)
    {
        return GetBlockingObjectAt(hex) is not null;
    }

    private static int ComputeCost(BiomeDefinition biome)
    {
        return biome.Passable ? biome.MovementCostPerHex : int.MaxValue;
    }
}
