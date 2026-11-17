using Game.Core.Domain;

namespace Game.Core.Map;

/// <summary>
/// Партия на глобальной карте: герой и его армия либо отряд без героя.
/// Хранит позицию, запас очков движения и ёмкость лидерства. Мутируется
/// только командами карты (ADR-018): движение и сброс очков на дневном тике.
/// </summary>
public sealed class Party
{
    /// <summary>
    /// Создаёт партию.
    /// </summary>
    /// <param name="id">Уникальный ID партии.</param>
    /// <param name="role">Роль партии.</param>
    /// <param name="factionId">ID фракции.</param>
    /// <param name="heroId">ID героя; null — отряд без героя.</param>
    /// <param name="capacity">Ёмкость лидерства: герой или 50 без героя (ADR-006).</param>
    /// <param name="position">Стартовый гекс.</param>
    /// <param name="movementPointsPerDay">Очки движения за день.</param>
    /// <exception cref="ArgumentException">Пустой ID или ID фракции.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Ёмкость или очки движения не положительны.</exception>
    public Party(
        string id,
        PartyRole role,
        string factionId,
        string? heroId,
        int capacity,
        Hex position,
        int movementPointsPerDay)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("ID партии не может быть пустым.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(factionId))
        {
            throw new ArgumentException("ID фракции партии не может быть пустым.", nameof(factionId));
        }

        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Ёмкость лидерства должна быть положительной.");
        }

        if (movementPointsPerDay <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(movementPointsPerDay),
                movementPointsPerDay,
                "Очки движения за день должны быть положительными.");
        }

        Id = id;
        Role = role;
        FactionId = factionId;
        HeroId = heroId;
        Capacity = capacity;
        Position = position;
        MovementPointsPerDay = movementPointsPerDay;
        MovementPoints = movementPointsPerDay;
    }

    /// <summary>Уникальный ID партии.</summary>
    public string Id { get; }

    /// <summary>Роль партии.</summary>
    public PartyRole Role { get; }

    /// <summary>ID фракции.</summary>
    public string FactionId { get; }

    /// <summary>ID героя; null — отряд без героя.</summary>
    public string? HeroId { get; }

    /// <summary>Есть ли у партии герой.</summary>
    public bool HasHero => !string.IsNullOrEmpty(HeroId);

    /// <summary>Ёмкость лидерства партии.</summary>
    public int Capacity { get; }

    /// <summary>Текущий гекс партии.</summary>
    public Hex Position { get; private set; }

    /// <summary>Очки движения за день.</summary>
    public int MovementPointsPerDay { get; }

    /// <summary>Оставшиеся очки движения.</summary>
    public int MovementPoints { get; private set; }

    /// <summary>
    /// Проверяет, хватает ли очков движения на стоимость.
    /// </summary>
    public bool CanSpend(int cost)
    {
        return cost >= 0 && MovementPoints >= cost;
    }

    /// <summary>
    /// Списывает очки движения.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Стоимость отрицательна или превышает остаток.</exception>
    public void Spend(int cost)
    {
        if (cost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost), cost, "Стоимость движения не может быть отрицательной.");
        }

        if (MovementPoints < cost)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cost),
                cost,
                $"Недостаточно очков движения: остаток {MovementPoints}.");
        }

        MovementPoints -= cost;
    }

    /// <summary>
    /// Перемещает партию на указанный гекс. Стоимость списывается отдельно.
    /// </summary>
    public void MoveTo(Hex position)
    {
        Position = position;
    }

    /// <summary>
    /// Сбрасывает очки движения на дневном тике.
    /// </summary>
    public void ResetMovement()
    {
        MovementPoints = MovementPointsPerDay;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Party({Id}, {FactionId}, {Position}, MP {MovementPoints}/{MovementPointsPerDay})";
    }
}
