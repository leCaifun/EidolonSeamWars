using System.Collections.ObjectModel;

namespace Game.Core.Domain;

/// <summary>
/// Контентное описание юнита (GDD §8, CONTENT_SCHEMA §6.2).
/// Конкретные юниты в коде не создаются: экземпляры приходят из JSON-контента
/// на этапе 03. Класс валидирует все поля на границе домена.
/// </summary>
public sealed class UnitDef
{
    private readonly ReadOnlyCollection<string> _abilityIds;
    private readonly ReadOnlyCollection<string> _promotionIds;

    /// <summary>
    /// Создаёт описание юнита и проверяет инварианты GDD.
    /// </summary>
    /// <exception cref="ArgumentException">Пустой ID, ключ локализации или ID фракции.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Числовое поле вне допустимого диапазона.</exception>
    public UnitDef(
        string id,
        int schemaVersion,
        string nameKey,
        string descriptionKey,
        string factionId,
        int tier,
        int leadershipCost,
        int attack,
        int defense,
        int initiative,
        int speed,
        int damageMin,
        int damageMax,
        int critChancePercent,
        int critDamagePercent,
        int health,
        MovementType movementType,
        Resistances resistances,
        IEnumerable<string>? abilityIds = null,
        IEnumerable<string>? promotionIds = null,
        int ammo = 0,
        string unitClass = "",
        DamageType damageType = DamageType.Physical)
    {
        Id = RequireText(id, nameof(id));
        SchemaVersion = RequirePositive(schemaVersion, nameof(schemaVersion));
        NameKey = RequireText(nameKey, nameof(nameKey));
        DescriptionKey = RequireText(descriptionKey, nameof(descriptionKey));
        FactionId = RequireText(factionId, nameof(factionId));
        Tier = RequireRange(tier, 1, 5, nameof(tier));
        LeadershipCost = RequirePositive(leadershipCost, nameof(leadershipCost));
        Attack = RequireNonNegative(attack, nameof(attack));
        Defense = RequireNonNegative(defense, nameof(defense));
        Initiative = RequireNonNegative(initiative, nameof(initiative));
        Speed = RequireNonNegative(speed, nameof(speed));
        DamageMin = RequireNonNegative(damageMin, nameof(damageMin));
        DamageMax = RequireNonNegative(damageMax, nameof(damageMax));

        if (DamageMax < DamageMin)
        {
            throw new ArgumentOutOfRangeException(
                nameof(damageMax),
                damageMax,
                "Максимальный урон не может быть меньше минимального.");
        }

        CritChancePercent = RequireRange(critChancePercent, 0, 75, nameof(critChancePercent));
        CritDamagePercent = RequireRange(critDamagePercent, 100, 300, nameof(critDamagePercent));
        Health = RequirePositive(health, nameof(health));
        MovementType = movementType;
        Resistances = resistances;
        _abilityIds = (ReadOnlyCollection<string>)CopyIds(abilityIds, nameof(abilityIds));
        _promotionIds = (ReadOnlyCollection<string>)CopyIds(promotionIds, nameof(promotionIds));
        Ammo = RequireNonNegative(ammo, nameof(ammo));
        UnitClass = unitClass ?? string.Empty;
        DamageType = damageType;
    }

    /// <summary>Стабильный строковый ID записи контента.</summary>
    public string Id { get; }

    /// <summary>Версия схемы записи.</summary>
    public int SchemaVersion { get; }

    /// <summary>Ключ локализации имени.</summary>
    public string NameKey { get; }

    /// <summary>Ключ локализации описания.</summary>
    public string DescriptionKey { get; }

    /// <summary>ID фракции-владельца.</summary>
    public string FactionId { get; }

    /// <summary>Тир юнита, 1…5.</summary>
    public int Tier { get; }

    /// <summary>Стоимость лидерства за одного юнита.</summary>
    public int LeadershipCost { get; }

    /// <summary>Базовая атака.</summary>
    public int Attack { get; }

    /// <summary>Базовая защита.</summary>
    public int Defense { get; }

    /// <summary>Инициатива.</summary>
    public int Initiative { get; }

    /// <summary>Скорость передвижения.</summary>
    public int Speed { get; }

    /// <summary>Минимальный урон.</summary>
    public int DamageMin { get; }

    /// <summary>Максимальный урон.</summary>
    public int DamageMax { get; }

    /// <summary>Шанс крита, %, кап 75 (GDD §7.4).</summary>
    public int CritChancePercent { get; }

    /// <summary>Множитель критического урона, %, 100…300.</summary>
    public int CritDamagePercent { get; }

    /// <summary>Здоровье одного юнита.</summary>
    public int Health { get; }

    /// <summary>Тип передвижения.</summary>
    public MovementType MovementType { get; }

    /// <summary>Пять резистов.</summary>
    public Resistances Resistances { get; }

    /// <summary>ID пассивных и активных способностей.</summary>
    public IReadOnlyList<string> AbilityIds => _abilityIds;

    /// <summary>ID промоушенов (M2); в MVP не влияют на бой.</summary>
    public IReadOnlyList<string> PromotionIds => _promotionIds;

    /// <summary>Зарезервированный боезапас; в MVP не влияет на бой (ADR-015).</summary>
    public int Ammo { get; }

    /// <summary>
    /// Класс юнита из контента (например, <c>undead</c>, <c>construct</c>, <c>caster</c>);
    /// пустая строка, если класс не задан. Используется правилами морали (иммунитет по тегам).
    /// </summary>
    public string UnitClass { get; }

    /// <summary>Тип урона, который наносит атака юнита (GDD §7.4).</summary>
    public DamageType DamageType { get; }

    /// <summary>
    /// Создаёт стак этого юнита с указанным количеством.
    /// </summary>
    public Stack CreateStack(int count, int killedStacks = 0)
    {
        return new Stack(this, count, killedStacks);
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Строковое поле юнита не может быть пустым.", parameterName);
        }

        return value;
    }

    private static int RequirePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Значение должно быть положительным.");
        }

        return value;
    }

    private static int RequireNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Значение не может быть отрицательным.");
        }

        return value;
    }

    private static int RequireRange(int value, int min, int max, string parameterName)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Значение должно быть в диапазоне {min}…{max}.");
        }

        return value;
    }

    private static IReadOnlyList<string> CopyIds(IEnumerable<string>? ids, string parameterName)
    {
        if (ids is null)
        {
            return Array.AsReadOnly(Array.Empty<string>());
        }

        List<string> copy = new();
        foreach (string id in ids)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("ID способности или промоушена не может быть пустым.", parameterName);
            }

            copy.Add(id);
        }

        return new ReadOnlyCollection<string>(copy);
    }
}
