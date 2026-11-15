namespace Game.Core.Domain;

/// <summary>
/// Пять резистов юнита в целых процентах (ADR-010). Значения клампятся
/// проверкой 0…100 на границе контента; нулевые резисты в UI не показываются.
/// </summary>
public readonly struct Resistances : IEquatable<Resistances>
{
    /// <summary>Нулевые резисты по всем пяти типам.</summary>
    public static readonly Resistances None = new(0, 0, 0, 0, 0);

    /// <summary>
    /// Создаёт набор резистов.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Хотя бы одно значение вне 0…100.</exception>
    public Resistances(
        int physicalPercent,
        int magicPercent,
        int poisonPercent,
        int firePercent,
        int gunpowderPercent)
    {
        PhysicalPercent = Validate(physicalPercent, nameof(physicalPercent));
        MagicPercent = Validate(magicPercent, nameof(magicPercent));
        PoisonPercent = Validate(poisonPercent, nameof(poisonPercent));
        FirePercent = Validate(firePercent, nameof(firePercent));
        GunpowderPercent = Validate(gunpowderPercent, nameof(gunpowderPercent));
    }

    /// <summary>Физический резист, %.</summary>
    public int PhysicalPercent { get; }

    /// <summary>Магический резист, %.</summary>
    public int MagicPercent { get; }

    /// <summary>Ядовитый резист, %.</summary>
    public int PoisonPercent { get; }

    /// <summary>Огненный резист, %.</summary>
    public int FirePercent { get; }

    /// <summary>Огнестрельный резист, %.</summary>
    public int GunpowderPercent { get; }

    /// <summary>
    /// Возвращает резист, соответствующий типу урона.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Неизвестный тип урона.</exception>
    public int Get(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Physical => PhysicalPercent,
            DamageType.Magic => MagicPercent,
            DamageType.Poison => PoisonPercent,
            DamageType.Fire => FirePercent,
            DamageType.Gunpowder => GunpowderPercent,
            _ => throw new ArgumentOutOfRangeException(nameof(damageType), damageType, "Неизвестный тип урона.")
        };
    }

    /// <inheritdoc />
    public bool Equals(Resistances other)
    {
        return PhysicalPercent == other.PhysicalPercent
            && MagicPercent == other.MagicPercent
            && PoisonPercent == other.PoisonPercent
            && FirePercent == other.FirePercent
            && GunpowderPercent == other.GunpowderPercent;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Resistances other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(
            PhysicalPercent,
            MagicPercent,
            PoisonPercent,
            FirePercent,
            GunpowderPercent);
    }

    /// <summary>Сравнивает два набора резистов.</summary>
    public static bool operator ==(Resistances left, Resistances right)
    {
        return left.Equals(right);
    }

    /// <summary>Сравнивает два набора резистов на неравенство.</summary>
    public static bool operator !=(Resistances left, Resistances right)
    {
        return !left.Equals(right);
    }

    private static int Validate(int value, string parameterName)
    {
        if (value < 0 || value > 100)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Резист должен быть в диапазоне 0…100.");
        }

        return value;
    }
}
