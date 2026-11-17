namespace Game.Core.Battle;

/// <summary>
/// Исход броска морали в начале хода юнита.
/// </summary>
public enum MoraleRoll
{
    /// <summary>Ничего не произошло.</summary>
    None = 0,

    /// <summary>Юнит получает дополнительное действие в конце текущей очереди.</summary>
    ExtraAction = 1,

    /// <summary>Юнит пропускает ход из-за морали.</summary>
    Skip = 2
}
