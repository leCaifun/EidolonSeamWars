using System.Collections.ObjectModel;

namespace Game.Core.Map;

/// <summary>
/// Иммутабельный справочник объектов карты по стабильному строковому ID.
/// </summary>
public sealed class MapObjectCatalog
{
    private readonly ReadOnlyDictionary<string, MapObjectDefinition> _byId;

    /// <summary>
    /// Создаёт справочник.
    /// </summary>
    /// <exception cref="ArgumentNullException">Список не задан.</exception>
    /// <exception cref="ArgumentException">Пустой или повторяющийся ID.</exception>
    public MapObjectCatalog(IEnumerable<MapObjectDefinition> objects)
    {
        ArgumentNullException.ThrowIfNull(objects);

        Dictionary<string, MapObjectDefinition> byId = new(StringComparer.Ordinal);

        foreach (MapObjectDefinition definition in objects)
        {
            if (!byId.TryAdd(definition.Id, definition))
            {
                throw new ArgumentException($"Повторяющийся ID объекта карты «{definition.Id}».", nameof(objects));
            }
        }

        _byId = new ReadOnlyDictionary<string, MapObjectDefinition>(byId);
    }

    /// <summary>Все объекты по ID.</summary>
    public IReadOnlyDictionary<string, MapObjectDefinition> All => _byId;

    /// <summary>Количество объектов в справочнике.</summary>
    public int Count => _byId.Count;

    /// <summary>
    /// Возвращает описание объекта по ID.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Объект не найден.</exception>
    public MapObjectDefinition Get(string id)
    {
        if (id is null || !_byId.TryGetValue(id, out MapObjectDefinition? definition))
        {
            throw new KeyNotFoundException($"Объект карты «{id}» отсутствует в справочнике.");
        }

        return definition;
    }

    /// <summary>
    /// Пытается получить описание объекта по ID.
    /// </summary>
    public bool TryGet(string id, out MapObjectDefinition definition)
    {
        if (id is not null && _byId.TryGetValue(id, out MapObjectDefinition? found))
        {
            definition = found;
            return true;
        }

        definition = null!;
        return false;
    }

    /// <summary>
    /// Возвращает определения указанного типа, отсортированные по ID.
    /// Стабильный порядок важен для детерминированной генерации.
    /// </summary>
    public IReadOnlyList<MapObjectDefinition> ByType(string type)
    {
        return _byId.Values
            .Where(definition => string.Equals(definition.Type, type, StringComparison.Ordinal))
            .OrderBy(definition => definition.Id, StringComparer.Ordinal)
            .ToArray();
    }
}
