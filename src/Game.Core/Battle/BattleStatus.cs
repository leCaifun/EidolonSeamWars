namespace Game.Core.Battle;

/// <summary>
/// Состояние боя в целом.
/// </summary>
public enum BattleStatus
{
    /// <summary>Бой идёт.</summary>
    Ongoing = 0,

    /// <summary>Победил атакующий.</summary>
    AttackerWon = 1,

    /// <summary>Победил защитник.</summary>
    DefenderWon = 2,

    /// <summary>Обе стороны уничтожены одновременно.</summary>
    Draw = 3
}
