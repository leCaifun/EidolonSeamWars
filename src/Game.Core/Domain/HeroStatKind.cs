namespace Game.Core.Domain;

/// <summary>
/// Шесть характеристик героя (GDD §9.2). Числовые значения стабильны
/// и используются в сохранениях и весах левелапа.
/// </summary>
public enum HeroStatKind
{
    /// <summary>Атака — бонус к атаке армии.</summary>
    Attack = 0,

    /// <summary>Защита — бонус к защите армии.</summary>
    Defense = 1,

    /// <summary>Чародейство — сила заклинаний.</summary>
    Spellpower = 2,

    /// <summary>Знание — мана героя.</summary>
    Knowledge = 3,

    /// <summary>Лидерство — ёмкость армии.</summary>
    Leadership = 4,

    /// <summary>Логистика — очки движения и шанс побега.</summary>
    Logistics = 5
}
