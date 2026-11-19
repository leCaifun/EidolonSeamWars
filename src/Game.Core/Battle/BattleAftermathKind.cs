namespace Game.Core.Battle;

/// <summary>
/// Последствия боя для стороны (GDD §7.7, §9.5).
/// </summary>
public enum BattleAftermathKind
{
    /// <summary>Сторона победила.</summary>
    Victory = 0,

    /// <summary>Герой побеждённой стороны бежал; армия перестаёт существовать.</summary>
    HeroEscaped = 1,

    /// <summary>Побег провален: герой попадает в плен, армия перестаёт существовать.</summary>
    HeroCaptured = 2,

    /// <summary>Побег запрещён (защитник осады): армия перестаёт существовать, судьбу героя решает кампания.</summary>
    EscapeForbidden = 3,

    /// <summary>Побеждённый отряд без героя перестаёт существовать.</summary>
    ArmyDestroyed = 4,

    /// <summary>Герой откупился: армия сохранена.</summary>
    HeroRansomed = 5
}
