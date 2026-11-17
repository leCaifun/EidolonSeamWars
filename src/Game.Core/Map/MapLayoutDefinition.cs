using System.Collections.ObjectModel;
using Game.Core.Domain;

namespace Game.Core.Map;

/// <summary>
/// Промежуточное представление карты до создания <see cref="GameMap"/>:
/// размеры, тайлы, размещения объектов и стартовые позиции. Не зависит от
/// контентного слоя и одинаково подходит генератору и загрузчику рукотворных карт.
/// </summary>
public sealed class MapLayoutDefinition
{
    private readonly ReadOnlyCollection<MapTile> _tiles;
    private readonly ReadOnlyCollection<MapLayoutObject> _objects;
    private readonly ReadOnlyCollection<MapStartDefinition> _starts;

    /// <summary>
    /// Создаёт представление карты.
    /// </summary>
    /// <param name="id">Стабильный ID карты.</param>
    /// <param name="nameKey">Ключ локализации названия.</param>
    /// <param name="width">Ширина в гексах.</param>
    /// <param name="height">Высота в гексах.</param>
    /// <param name="layout">Схема offset-координат.</param>
    /// <param name="tiles">Тайлы карты.</param>
    /// <param name="objects">Размещения объектов.</param>
    /// <param name="starts">Стартовые позиции.</param>
    /// <exception cref="ArgumentException">ID, ключ или размеры некорректны.</exception>
    /// <exception cref="ArgumentNullException">Обязательный список не задан.</exception>
    public MapLayoutDefinition(
        string id,
        string nameKey,
        int width,
        int height,
        HexOffsetLayout layout,
        IEnumerable<MapTile> tiles,
        IEnumerable<MapLayoutObject> objects,
        IEnumerable<MapStartDefinition> starts)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("ID карты не может быть пустым.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(nameKey))
        {
            throw new ArgumentException("Ключ названия карты не может быть пустым.", nameof(nameKey));
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException("Размеры карты должны быть положительными.", nameof(width));
        }

        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(starts);

        Id = id;
        NameKey = nameKey;
        Width = width;
        Height = height;
        Layout = layout;
        _tiles = new ReadOnlyCollection<MapTile>(tiles.ToArray());
        _objects = new ReadOnlyCollection<MapLayoutObject>(objects.ToArray());
        _starts = new ReadOnlyCollection<MapStartDefinition>(starts.ToArray());
    }

    /// <summary>Стабильный ID карты.</summary>
    public string Id { get; }

    /// <summary>Ключ локализации названия.</summary>
    public string NameKey { get; }

    /// <summary>Ширина в гексах.</summary>
    public int Width { get; }

    /// <summary>Высота в гексах.</summary>
    public int Height { get; }

    /// <summary>Схема offset-координат.</summary>
    public HexOffsetLayout Layout { get; }

    /// <summary>Тайлы карты.</summary>
    public IReadOnlyList<MapTile> Tiles => _tiles;

    /// <summary>Размещения объектов.</summary>
    public IReadOnlyList<MapLayoutObject> Objects => _objects;

    /// <summary>Стартовые позиции.</summary>
    public IReadOnlyList<MapStartDefinition> Starts => _starts;

    /// <summary>
    /// Создаёт пустую сетку с параметрами карты.
    /// </summary>
    public HexGrid CreateGrid()
    {
        return new HexGrid(Width, Height, Layout);
    }
}
