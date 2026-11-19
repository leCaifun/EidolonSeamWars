namespace Game.Core.Battle;

/// <summary>
/// Сторона боя. Порядок значений используется тай-брейком очереди хода: при равной
/// инициативе первым ходит атакующий, затем стороны чередуются (ADR-038).
/// </summary>
public enum BattleSide
{
    /// <summary>Атакующий.</summary>
    Attacker = 0,

    /// <summary>Защитник.</summary>
    Defender = 1
}
