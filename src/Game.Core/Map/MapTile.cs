using Game.Core.Domain;

namespace Game.Core.Map;

/// <summary>
/// Гекс карты: аксиальная позиция и ID биома. Стоимость входа и проходимость
/// вычисляются по справочнику биомов и хранятся в <see cref="GameMap"/>.
/// </summary>
public readonly struct MapTile : IEquatable<MapTile>
{
    /// <summary>
    /// Создаёт тайл.
    /// </summary>
    /// <exception cref="ArgumentException">ID биома пуст.</exception>
    public MapTile(Hex position, string biomeId)
    {
        if (string.IsNullOrWhiteSpace(biomeId))
        {
            throw new ArgumentException("ID биома тайла не может быть пустым.", nameof(biomeId));
        }

        Position = position;
        BiomeId = biomeId;
    }

    /// <summary>Аксиальная позиция гекса.</summary>
    public Hex Position { get; }

    /// <summary>ID биома.</summary>
    public string BiomeId { get; }

    /// <inheritdoc />
    public bool Equals(MapTile other)
    {
        return Position == other.Position
            && string.Equals(BiomeId, other.BiomeId, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is MapTile other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Position, BiomeId);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"MapTile({Position}, {BiomeId})";
    }
}
