using Game.Core.Domain;

namespace Game.Core.Map;

/// <summary>
/// Описание стартовой позиции стороны на глобальной карте: роль, фракция,
/// герой и гекс. Используется и рукотворными картами, и генератором.
/// </summary>
public sealed class MapStartDefinition
{
    /// <summary>
    /// Создаёт стартовую позицию.
    /// </summary>
    /// <param name="role">Роль стороны.</param>
    /// <param name="factionId">ID фракции.</param>
    /// <param name="heroId">ID героя; null — отряд без героя.</param>
    /// <param name="position">Стартовый гекс.</param>
    /// <exception cref="ArgumentException">ID фракции пуст.</exception>
    public MapStartDefinition(
        PartyRole role,
        string factionId,
        string? heroId,
        Hex position)
    {
        if (string.IsNullOrWhiteSpace(factionId))
        {
            throw new ArgumentException("ID фракции старта не может быть пустым.", nameof(factionId));
        }

        Role = role;
        FactionId = factionId;
        HeroId = heroId;
        Position = position;
    }

    /// <summary>Роль стороны.</summary>
    public PartyRole Role { get; }

    /// <summary>ID фракции.</summary>
    public string FactionId { get; }

    /// <summary>ID героя; null — отряд без героя.</summary>
    public string? HeroId { get; }

    /// <summary>Стартовый гекс.</summary>
    public Hex Position { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"MapStart({Role}, {FactionId}, {Position})";
    }
}
