using Game.Core.Domain;

namespace Game.Core.Map;

/// <summary>
/// Прямоугольная гексагональная сетка заданного размера в offset-координатах.
/// Истина — аксиальные координаты <see cref="Hex"/>; offset-схема применяется
/// только на границе: при загрузке карты, сериализации и рендере (ADR-003).
/// Сетка карты и сетка боя используют один тип <see cref="Hex"/> и разные размеры.
/// </summary>
public sealed class HexGrid
{
    /// <summary>
    /// Создаёт сетку.
    /// </summary>
    /// <param name="width">Ширина в гексах; положительная.</param>
    /// <param name="height">Высота в гексах; положительная.</param>
    /// <param name="layout">Схема offset-координат; по умолчанию odd-r.</param>
    /// <exception cref="ArgumentOutOfRangeException">Размер не положителен.</exception>
    public HexGrid(int width, int height, HexOffsetLayout layout = HexOffsetLayout.OddR)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Ширина сетки должна быть положительной.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Высота сетки должна быть положительной.");
        }

        Width = width;
        Height = height;
        Layout = layout;
    }

    /// <summary>Ширина сетки в гексах.</summary>
    public int Width { get; }

    /// <summary>Высота сетки в гексах.</summary>
    public int Height { get; }

    /// <summary>Схема offset-координат, используемая на границе.</summary>
    public HexOffsetLayout Layout { get; }

    /// <summary>Общее число гексов сетки.</summary>
    public int HexCount => Width * Height;

    /// <summary>
    /// Проверяет, лежит ли гекс внутри сетки.
    /// </summary>
    public bool Contains(Hex hex)
    {
        return TryGetOffset(hex, out _, out _);
    }

    /// <summary>
    /// Преобразует аксиальные координаты в offset-координаты и проверяет границы.
    /// </summary>
    /// <returns><c>true</c>, если гекс внутри сетки.</returns>
    public bool TryGetOffset(Hex hex, out int column, out int row)
    {
        (column, row) = hex.ToOffsetCoordinates(Layout);
        return column >= 0 && column < Width && row >= 0 && row < Height;
    }

    /// <summary>
    /// Возвращает гекс по offset-координатам.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Координаты вне сетки.</exception>
    public Hex HexAt(int column, int row)
    {
        if (column < 0 || column >= Width)
        {
            throw new ArgumentOutOfRangeException(nameof(column), column, $"Столбец должен быть в диапазоне 0…{Width - 1}.");
        }

        if (row < 0 || row >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(row), row, $"Строка должна быть в диапазоне 0…{Height - 1}.");
        }

        return Hex.FromOffsetCoordinates(column, row, Layout);
    }

    /// <summary>
    /// Возвращает линейный индекс гекса в порядке «строка сверху вниз, столбец слева направо»
    /// или −1, если гекс вне сетки. Индекс стабилен и используется в сейвах и рендере.
    /// </summary>
    public int IndexOf(Hex hex)
    {
        return TryGetOffset(hex, out int column, out int row) ? (row * Width) + column : -1;
    }

    /// <summary>
    /// Перечисляет все гексы в стабильном порядке: строки сверху вниз, столбцы слева направо.
    /// </summary>
    public IEnumerable<Hex> AllHexes()
    {
        for (int row = 0; row < Height; row++)
        {
            for (int column = 0; column < Width; column++)
            {
                yield return HexAt(column, row);
            }
        }
    }

    /// <summary>
    /// Перечисляет соседей гекса, лежащих внутри сетки, в фиксированном порядке направлений.
    /// </summary>
    public IEnumerable<Hex> NeighborsInBounds(Hex hex)
    {
        Hex[] neighbors = hex.GetNeighbors();

        for (int i = 0; i < neighbors.Length; i++)
        {
            if (Contains(neighbors[i]))
            {
                yield return neighbors[i];
            }
        }
    }

    /// <summary>
    /// Перечисляет гексы в пределах гексагонального радиуса от центра, включая центр.
    /// Порядок детерминирован; гексы вне сетки пропускаются.
    /// </summary>
    /// <param name="center">Центр области.</param>
    /// <param name="radius">Радиус в шагах; отрицательный трактуется как 0.</param>
    public IEnumerable<Hex> WithinRange(Hex center, int radius)
    {
        if (radius < 0)
        {
            yield break;
        }

        for (int dq = -radius; dq <= radius; dq++)
        {
            int minDr = Math.Max(-radius, -dq - radius);
            int maxDr = Math.Min(radius, -dq + radius);

            for (int dr = minDr; dr <= maxDr; dr++)
            {
                Hex hex = new(unchecked(center.Q + dq), unchecked(center.R + dr));

                if (Contains(hex))
                {
                    yield return hex;
                }
            }
        }
    }
}
