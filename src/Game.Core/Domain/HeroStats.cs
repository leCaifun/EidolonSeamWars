namespace Game.Core.Domain;

/// <summary>
/// Иммутабельный набор характеристик героя (GDD §9.2). Все значения целые
/// и неотрицательные; лидерство обязано быть положительным, иначе армия
/// героя не может существовать.
/// </summary>
public readonly struct HeroStats : IEquatable<HeroStats>
{
    /// <summary>
    /// Создаёт набор характеристик.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Отрицательная характеристика или нулевое лидерство.</exception>
    public HeroStats(
        int attack,
        int defense,
        int spellpower,
        int knowledge,
        int leadership,
        int logistics)
    {
        Attack = RequireNonNegative(attack, nameof(attack));
        Defense = RequireNonNegative(defense, nameof(defense));
        Spellpower = RequireNonNegative(spellpower, nameof(spellpower));
        Knowledge = RequireNonNegative(knowledge, nameof(knowledge));
        Leadership = RequirePositive(leadership, nameof(leadership));
        Logistics = RequireNonNegative(logistics, nameof(logistics));
    }

    /// <summary>Атака.</summary>
    public int Attack { get; }

    /// <summary>Защита.</summary>
    public int Defense { get; }

    /// <summary>Чародейство.</summary>
    public int Spellpower { get; }

    /// <summary>Знание.</summary>
    public int Knowledge { get; }

    /// <summary>Лидерство.</summary>
    public int Leadership { get; }

    /// <summary>Логистика.</summary>
    public int Logistics { get; }

    /// <summary>
    /// Возвращает значение указанной характеристики.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Неизвестная характеристика.</exception>
    public int Get(HeroStatKind kind)
    {
        return kind switch
        {
            HeroStatKind.Attack => Attack,
            HeroStatKind.Defense => Defense,
            HeroStatKind.Spellpower => Spellpower,
            HeroStatKind.Knowledge => Knowledge,
            HeroStatKind.Leadership => Leadership,
            HeroStatKind.Logistics => Logistics,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Неизвестная характеристика героя.")
        };
    }

    /// <summary>
    /// Возвращает новый набор с изменённой на <paramref name="delta"/> характеристикой.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Неизвестная характеристика или результат ниже нуля.</exception>
    /// <exception cref="OverflowException">Результат не помещается в int.</exception>
    public HeroStats With(HeroStatKind kind, int delta)
    {
        int changed = checked(Get(kind) + delta);

        return kind switch
        {
            HeroStatKind.Attack => new HeroStats(changed, Defense, Spellpower, Knowledge, Leadership, Logistics),
            HeroStatKind.Defense => new HeroStats(Attack, changed, Spellpower, Knowledge, Leadership, Logistics),
            HeroStatKind.Spellpower => new HeroStats(Attack, Defense, changed, Knowledge, Leadership, Logistics),
            HeroStatKind.Knowledge => new HeroStats(Attack, Defense, Spellpower, changed, Leadership, Logistics),
            HeroStatKind.Leadership => new HeroStats(Attack, Defense, Spellpower, Knowledge, changed, Logistics),
            HeroStatKind.Logistics => new HeroStats(Attack, Defense, Spellpower, Knowledge, Leadership, changed),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Неизвестная характеристика героя.")
        };
    }

    /// <inheritdoc />
    public bool Equals(HeroStats other)
    {
        return Attack == other.Attack
            && Defense == other.Defense
            && Spellpower == other.Spellpower
            && Knowledge == other.Knowledge
            && Leadership == other.Leadership
            && Logistics == other.Logistics;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is HeroStats other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Attack, Defense, Spellpower, Knowledge, Leadership, Logistics);
    }

    /// <summary>Сравнивает два набора характеристик.</summary>
    public static bool operator ==(HeroStats left, HeroStats right)
    {
        return left.Equals(right);
    }

    /// <summary>Сравнивает два набора характеристик на неравенство.</summary>
    public static bool operator !=(HeroStats left, HeroStats right)
    {
        return !left.Equals(right);
    }

    private static int RequireNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Характеристика не может быть отрицательной.");
        }

        return value;
    }

    private static int RequirePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Лидерство должно быть положительным.");
        }

        return value;
    }
}
