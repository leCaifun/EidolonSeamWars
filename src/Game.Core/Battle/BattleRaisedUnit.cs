namespace Game.Core.Battle;

/// <summary>
/// Юниты, поднятые после боя классовой способностью (например, «Учёт потерь»). Добавление в армию на карте
/// выполняет слой кампании.
/// </summary>
/// <param name="Side">Сторона, чей герой поднимает юнитов.</param>
/// <param name="UnitDefId">ID поднятого юнита.</param>
/// <param name="Count">Сколько юнитов поднято.</param>
public readonly record struct BattleRaisedUnit(BattleSide Side, string UnitDefId, int Count);
