namespace Game.Core.Battle;

/// <summary>
/// Запись очереди хода: стак и признак дополнительного действия морали.
/// </summary>
/// <param name="Unit">Боевой стак.</param>
/// <param name="IsExtraAction">Ход является дополнительным действием (цепочки запрещены).</param>
public readonly record struct TurnEntry(BattleUnit Unit, bool IsExtraAction);
