using System.Collections.ObjectModel;

namespace Game.Core.Map;

/// <summary>
/// Иммутабельный справочник биомов по стабильному строковому ID.
/// </summary>
public sealed class BiomeCatalog
{
    private readonly ReadOnlyDictionary<string, BiomeDefinition> _byId;

    /// <summary>
    /// Создаёт справочник. Повторяющийся ID — ошибка контента и здесь запрещён.
    /// </summary>
    /// <exception cref="ArgumentNullException">Список не задан.</exception>
    /// <exception cref="ArgumentException">Пустой или повторяющийся ID.</exception>
    public BiomeCatalog(IEnumerable<BiomeDefinition> biomes)
    {
        ArgumentNullException.ThrowIfNull(biomes);

        Dictionary<string, BiomeDefinition> byId = new(StringComparer.Ordinal);

        foreach (BiomeDefinition biome in biomes)
        {
            if (!byId.TryAdd(biome.Id, biome))
            {
                throw new ArgumentException($"Повторяющийся ID биома «{biome.Id}».", nameof(biomes));
            }
        }

        _byId = new ReadOnlyDictionary<string, BiomeDefinition>(byId);
    }

    /// <summary>Все биомы по ID.</summary>
    public IReadOnlyDictionary<string, BiomeDefinition> All => _byId;

    /// <summary>Количество биомов.</summary>
    public int Count => _byId.Count;

    /// <summary>
    /// Возвращает биом по ID.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Биом не найден.</exception>
    public BiomeDefinition Get(string id)
    {
        if (id is null || !_byId.TryGetValue(id, out BiomeDefinition? biome))
        {
            throw new KeyNotFoundException($"Биом «{id}» отсутствует в справочнике.");
        }

        return biome;
    }

    /// <summary>
    /// Пытается получить биом по ID.
    /// </summary>
    public bool TryGet(string id, out BiomeDefinition biome)
    {
        if (id is not null && _byId.TryGetValue(id, out BiomeDefinition? found))
        {
            biome = found;
            return true;
        }

        biome = null!;
        return false;
    }
}
