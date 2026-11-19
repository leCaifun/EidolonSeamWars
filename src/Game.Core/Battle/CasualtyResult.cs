namespace Game.Core.Battle;

/// <summary>
/// Итог нанесения урона стаку: сколько юнитов погибло и что осталось от пула здоровья.
/// </summary>
/// <param name="DamageDealt">Урон, реально снятый с пула здоровья (без перебора).</param>
/// <param name="Overkill">Урон сверх оставшегося здоровья стака.</param>
/// <param name="UnitsKilled">Сколько юнитов погибло.</param>
/// <param name="RemainingCount">Сколько юнитов осталось.</param>
/// <param name="RemainingTopHealth">Здоровье верхнего юнита после удара (0, если стак уничтожен).</param>
/// <param name="StackDestroyed">Стак уничтожен полностью.</param>
public readonly record struct CasualtyResult(
    int DamageDealt,
    int Overkill,
    int UnitsKilled,
    int RemainingCount,
    int RemainingTopHealth,
    bool StackDestroyed);
