using Game.Core.Content.Dto;
using Game.Core.Map;

namespace Game.Core.Content;

/// <summary>
/// Загрузчик рукотворных тестовых карт из уже загруженного реестра контента.
/// Прямого файлового ввода-вывода нет: карты приходят из <see cref="ContentRegistry"/>.
/// Загрузчик используется тестами и инструментами проверки карт.
/// </summary>
public sealed class HandmadeMapLoader
{
    private readonly ContentRegistry _registry;
    private readonly BiomeCatalog _biomes;
    private readonly MapObjectCatalog _objects;
    private readonly MapGenerationSettings _settings;

    /// <summary>
    /// Создаёт загрузчик для реестра контента.
    /// </summary>
    /// <param name="registry">Реестр контента.</param>
    /// <exception cref="ArgumentNullException">Реестр не задан.</exception>
    public HandmadeMapLoader(ContentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        _registry = registry;
        _biomes = MapContentAdapter.CreateBiomes(registry);
        _objects = MapContentAdapter.CreateMapObjects(registry);
        _settings = MapContentAdapter.CreateSettings(registry);
    }

    /// <summary>
    /// Загружает одну рукотворную карту по ID.
    /// </summary>
    /// <param name="testMapId">Стабильный ID тестовой карты.</param>
    /// <exception cref="ArgumentException">ID пуст.</exception>
    /// <exception cref="KeyNotFoundException">Карта не найдена в контенте.</exception>
    public HandmadeMapResult Load(string testMapId)
    {
        if (string.IsNullOrWhiteSpace(testMapId))
        {
            throw new ArgumentException("ID тестовой карты не может быть пустым.", nameof(testMapId));
        }

        if (!_registry.TestMaps.TryGetValue(testMapId, out ContentEntry<TestMapDto>? entry))
        {
            throw new KeyNotFoundException($"Тестовая карта «{testMapId}» отсутствует в контенте.");
        }

        return Build(entry.Value);
    }

    /// <summary>
    /// Загружает все рукотворные карты в стабильном порядке ID.
    /// </summary>
    public IReadOnlyList<HandmadeMapResult> LoadAll()
    {
        return _registry.TestMaps.Values
            .OrderBy(item => item.Value.Id, StringComparer.Ordinal)
            .Select(item => Build(item.Value))
            .ToArray();
    }

    private HandmadeMapResult Build(TestMapDto dto)
    {
        MapLayoutDefinition layout = MapContentAdapter.CreateLayout(_registry, dto);
        GameMap map = MapContentAdapter.CreateMap(
            layout,
            _biomes,
            _objects);

        return new HandmadeMapResult(dto, layout, map);
    }
}
