namespace Game.Core.Domain;

/// <summary>
/// Типы урона MVP (GDD §7.4). Каждый тип отображается на один из пяти резистов.
/// </summary>
public enum DamageType
{
    /// <summary>Ближний бой и физическое оружие.</summary>
    Physical = 0,

    /// <summary>Заклинания и природный урон.</summary>
    Magic = 1,

    /// <summary>Яд и истощение.</summary>
    Poison = 2,

    /// <summary>Огонь и огненные эффекты.</summary>
    Fire = 3,

    /// <summary>Аркебузы, порох и огнестрельное оружие.</summary>
    Gunpowder = 4
}
