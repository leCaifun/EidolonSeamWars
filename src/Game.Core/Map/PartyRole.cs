namespace Game.Core.Map;

/// <summary>
/// Роль партии на глобальной карте. Роль определяет сторону, за которую
/// действует партия, и используется туманом войны и ходом ИИ.
/// </summary>
public enum PartyRole
{
    /// <summary>Партия игрока.</summary>
    Player = 0,

    /// <summary>Партия противника.</summary>
    Enemy = 1,

    /// <summary>Нейтральная партия.</summary>
    Neutral = 2
}
