using Game.Core.Domain;

namespace Game.Core.Map;

/// <summary>
/// Туман войны по фракциям (GDD §4). Каждый гекс хранит состояние
/// «скрытый / исследованный / видимый». Исследованные гексы остаются
/// исследованными навсегда; видимость пересчитывается при движении партии.
/// </summary>
public sealed class FogOfWar
{
    private readonly HexGrid _grid;
    private readonly Dictionary<string, FogState[]> _byFaction = new(StringComparer.Ordinal);

    /// <summary>
    /// Создаёт туман для сетки.
    /// </summary>
    /// <exception cref="ArgumentNullException">Сетка не задана.</exception>
    public FogOfWar(HexGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        _grid = grid;
    }

    /// <summary>
    /// Возвращает состояние гекса для фракции. Для неизвестной фракции
    /// все гексы скрыты.
    /// </summary>
    public FogState GetState(string factionId, Hex hex)
    {
        if (factionId is null || !_byFaction.TryGetValue(factionId, out FogState[]? states))
        {
            return FogState.Hidden;
        }

        int index = _grid.IndexOf(hex);
        return index < 0 ? FogState.Hidden : states[index];
    }

    /// <summary>Виден ли гекс фракции сейчас.</summary>
    public bool IsVisible(string factionId, Hex hex)
    {
        return GetState(factionId, hex) == FogState.Visible;
    }

    /// <summary>Исследован ли гекс фракцией (видимый тоже исследован).</summary>
    public bool IsExplored(string factionId, Hex hex)
    {
        return GetState(factionId, hex) != FogState.Hidden;
    }

    /// <summary>
    /// Пересчитывает видимость вокруг одного центра: прежние видимые гексы фракции
    /// становятся исследованными, затем все гексы в радиусе становятся видимыми.
    /// Подходит для фракции с единственным источником обзора; для нескольких
    /// партий одной фракции используйте <see cref="ClearVisibility"/> и
    /// <see cref="AddVision"/>, иначе обзор прежних источников будет сброшен.
    /// </summary>
    /// <param name="factionId">ID фракции.</param>
    /// <param name="center">Центр обзора (позиция партии).</param>
    /// <param name="radius">Радиус обзора; отрицательный трактуется как 0.</param>
    /// <exception cref="ArgumentException">ID фракции пуст.</exception>
    public void Reveal(string factionId, Hex center, int radius)
    {
        ClearVisibility(factionId);
        AddVision(factionId, center, radius);
    }

    /// <summary>
    /// Переводит все видимые гексы фракции в «исследованные». Вызывается один раз
    /// перед накоплением обзора от всех источников фракции через <see cref="AddVision"/>.
    /// </summary>
    /// <param name="factionId">ID фракции.</param>
    /// <exception cref="ArgumentException">ID фракции пуст.</exception>
    public void ClearVisibility(string factionId)
    {
        if (string.IsNullOrWhiteSpace(factionId))
        {
            throw new ArgumentException("ID фракции тумана не может быть пустым.", nameof(factionId));
        }

        FogState[] states = GetOrCreate(factionId);

        for (int i = 0; i < states.Length; i++)
        {
            if (states[i] == FogState.Visible)
            {
                states[i] = FogState.Explored;
            }
        }
    }

    /// <summary>
    /// Делает видимыми все гексы в радиусе, не трогая видимость от других источников.
    /// </summary>
    /// <param name="factionId">ID фракции.</param>
    /// <param name="center">Центр обзора.</param>
    /// <param name="radius">Радиус обзора; отрицательный трактуется как 0.</param>
    /// <exception cref="ArgumentException">ID фракции пуст.</exception>
    public void AddVision(string factionId, Hex center, int radius)
    {
        if (string.IsNullOrWhiteSpace(factionId))
        {
            throw new ArgumentException("ID фракции тумана не может быть пустым.", nameof(factionId));
        }

        FogState[] states = GetOrCreate(factionId);

        foreach (Hex hex in _grid.WithinRange(center, Math.Max(0, radius)))
        {
            int index = _grid.IndexOf(hex);

            if (index >= 0)
            {
                states[index] = FogState.Visible;
            }
        }
    }

    /// <summary>
    /// Помечает гексы в радиусе как исследованные, не делая их видимыми.
    /// Ранее видимые гексы сохраняют состояние «видимый».
    /// </summary>
    /// <param name="factionId">ID фракции.</param>
    /// <param name="center">Центр области.</param>
    /// <param name="radius">Радиус в шагах; отрицательный трактуется как 0.</param>
    /// <exception cref="ArgumentException">ID фракции пуст.</exception>
    public void MarkExplored(string factionId, Hex center, int radius)
    {
        if (string.IsNullOrWhiteSpace(factionId))
        {
            throw new ArgumentException("ID фракции тумана не может быть пустым.", nameof(factionId));
        }

        FogState[] states = GetOrCreate(factionId);

        foreach (Hex hex in _grid.WithinRange(center, radius))
        {
            int index = _grid.IndexOf(hex);

            if (index >= 0 && states[index] == FogState.Hidden)
            {
                states[index] = FogState.Explored;
            }
        }
    }

    /// <summary>Количество видимых сейчас гексов фракции.</summary>
    public int VisibleCount(string factionId)
    {
        return CountState(factionId, FogState.Visible);
    }

    /// <summary>Количество исследованных (включая видимые) гексов фракции.</summary>
    public int ExploredCount(string factionId)
    {
        if (factionId is null || !_byFaction.TryGetValue(factionId, out FogState[]? states))
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < states.Length; i++)
        {
            if (states[i] != FogState.Hidden)
            {
                count++;
            }
        }

        return count;
    }

    private int CountState(string factionId, FogState state)
    {
        if (factionId is null || !_byFaction.TryGetValue(factionId, out FogState[]? states))
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < states.Length; i++)
        {
            if (states[i] == state)
            {
                count++;
            }
        }

        return count;
    }

    private FogState[] GetOrCreate(string factionId)
    {
        if (_byFaction.TryGetValue(factionId, out FogState[]? states))
        {
            return states;
        }

        FogState[] created = new FogState[_grid.HexCount];
        _byFaction.Add(factionId, created);
        return created;
    }
}
