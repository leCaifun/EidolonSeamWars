namespace Game.Core.Battle;

/// <summary>
/// Итог одного стака после боя: сколько юнитов было и осталось и сколько стаков победил.
/// Ранения верхнего юнита между боями не сохраняются — стак возвращается на карту только с количеством.
/// </summary>
/// <param name="UnitId">ID боевого стака.</param>
/// <param name="Side">Сторона.</param>
/// <param name="SlotIndex">Индекс слота в составе стороны.</param>
/// <param name="UnitDefId">ID определения юнита.</param>
/// <param name="StartCount">Количество юнитов до боя.</param>
/// <param name="EndCount">Количество юнитов после боя.</param>
/// <param name="KilledStacksGained">Побеждённые стаки за бой (ветеранство).</param>
public readonly record struct BattleStackResult(
    string UnitId,
    BattleSide Side,
    int SlotIndex,
    string UnitDefId,
    int StartCount,
    int EndCount,
    int KilledStacksGained)
{
    /// <summary>Потеряно юнитов.</summary>
    public int Lost => StartCount - EndCount;

    /// <summary>Выжил ли стак.</summary>
    public bool Survived => EndCount > 0;
}
