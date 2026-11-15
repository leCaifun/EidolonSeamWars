namespace Game.Core.Domain;

/// <summary>
/// Позиция на гексагональной сетке в аксиальных координатах (q, r).
/// Третья кубическая координата вычисляется как s = −q − r и не хранится.
/// Тип иммутабелен и пригоден как ключ словаря.
/// </summary>
public readonly struct Hex : IEquatable<Hex>
{
    /// <summary>Количество направлений на гексагональной сетке.</summary>
    public const int DirectionCount = 6;

    /// <summary>Смещения соседей в порядке: восток, северо-восток, северо-запад, запад, юго-запад, юго-восток.</summary>
    private static readonly (int Q, int R)[] NeighborOffsets =
    {
        (1, 0),
        (1, -1),
        (0, -1),
        (-1, 0),
        (-1, 1),
        (0, 1)
    };

    /// <summary>Начало координат (0, 0).</summary>
    public static readonly Hex Zero = new(0, 0);

    /// <summary>
    /// Создаёт гекс с указанными аксиальными координатами.
    /// </summary>
    /// <param name="q">Координата q (столбец по оси восток-запад).</param>
    /// <param name="r">Координата r (ось северо-восток — юго-запад).</param>
    public Hex(int q, int r)
    {
        Q = q;
        R = r;
    }

    /// <summary>Аксиальная координата q.</summary>
    public int Q { get; }

    /// <summary>Аксиальная координата r.</summary>
    public int R { get; }

    /// <summary>Третья кубическая координата s = −q − r.</summary>
    public int S => -Q - R;

    /// <summary>
    /// Возвращает соседа в заданном направлении.
    /// </summary>
    /// <param name="direction">Индекс направления от 0 до 5 (см. <see cref="DirectionCount"/>).</param>
    /// <exception cref="ArgumentOutOfRangeException">Индекс направления вне диапазона 0…5.</exception>
    public Hex Neighbor(int direction)
    {
        if (direction < 0 || direction >= DirectionCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                $"Индекс направления должен быть в диапазоне 0…{DirectionCount - 1}.");
        }

        (int dq, int dr) = NeighborOffsets[direction];
        return new Hex(unchecked(Q + dq), unchecked(R + dr));
    }

    /// <summary>
    /// Возвращает всех шести соседей в стабильном порядке направлений.
    /// </summary>
    public Hex[] GetNeighbors()
    {
        Hex[] neighbors = new Hex[DirectionCount];
        for (int i = 0; i < DirectionCount; i++)
        {
            neighbors[i] = Neighbor(i);
        }

        return neighbors;
    }

    /// <summary>
    /// Вычисляет гексагональное расстояние до другого гекса (минимум шагов по соседям).
    /// </summary>
    /// <param name="other">Целевой гекс.</param>
    public int DistanceTo(Hex other)
    {
        return Distance(this, other);
    }

    /// <summary>
    /// Вычисляет гексагональное расстояние между двумя гексами.
    /// </summary>
    /// <param name="a">Первый гекс.</param>
    /// <param name="b">Второй гекс.</param>
    public static int Distance(Hex a, Hex b)
    {
        long dq = (long)b.Q - a.Q;
        long dr = (long)b.R - a.R;
        long ds = (long)b.S - a.S;
        long distance = (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(ds)) / 2;
        return checked((int)distance);
    }

    /// <summary>
    /// Преобразует аксиальные координаты в offset-координаты (столбец, строка).
    /// Схема нужна только для сериализации и рендера; истина — аксиальные координаты.
    /// </summary>
    /// <param name="layout">Схема offset-координат.</param>
    /// <exception cref="ArgumentOutOfRangeException">Неизвестная схема.</exception>
    public (int Column, int Row) ToOffsetCoordinates(HexOffsetLayout layout)
    {
        int row = R;
        int column = layout switch
        {
            HexOffsetLayout.OddR => Q + (R - (R & 1)) / 2,
            HexOffsetLayout.EvenR => Q + (R + (R & 1)) / 2,
            _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, "Неизвестная схема offset-координат.")
        };

        return (column, row);
    }

    /// <summary>
    /// Восстанавливает аксиальные координаты из offset-координат.
    /// </summary>
    /// <param name="column">Столбец offset-сетки.</param>
    /// <param name="row">Строка offset-сетки.</param>
    /// <param name="layout">Схема offset-координат.</param>
    /// <exception cref="ArgumentOutOfRangeException">Неизвестная схема.</exception>
    public static Hex FromOffsetCoordinates(int column, int row, HexOffsetLayout layout)
    {
        int q = layout switch
        {
            HexOffsetLayout.OddR => column - (row - (row & 1)) / 2,
            HexOffsetLayout.EvenR => column - (row + (row & 1)) / 2,
            _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, "Неизвестная схема offset-координат.")
        };

        return new Hex(q, row);
    }

    /// <summary>
    /// Складывает две позиции покоординатно.
    /// </summary>
    public static Hex operator +(Hex left, Hex right)
    {
        return new Hex(unchecked(left.Q + right.Q), unchecked(left.R + right.R));
    }

    /// <summary>
    /// Вычитает одну позицию из другой покоординатно.
    /// </summary>
    public static Hex operator -(Hex left, Hex right)
    {
        return new Hex(unchecked(left.Q - right.Q), unchecked(left.R - right.R));
    }

    /// <inheritdoc />
    public bool Equals(Hex other)
    {
        return Q == other.Q && R == other.R;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Hex other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Q, R);
    }

    /// <summary>Сравнивает две позиции на равенство.</summary>
    public static bool operator ==(Hex left, Hex right)
    {
        return left.Equals(right);
    }

    /// <summary>Сравнивает две позиции на неравенство.</summary>
    public static bool operator !=(Hex left, Hex right)
    {
        return !left.Equals(right);
    }

    /// <summary>Возвращает строковое представление вида Hex(q, r).</summary>
    public override string ToString()
    {
        return $"Hex({Q}, {R})";
    }
}
