using Game.Core.Domain;

namespace Game.Core.Battle;

/// <summary>
/// Входные данные расчёта урона одного удара стака (GDD §7.4). Все значения —
/// итоговые целые после ветеранства и бонусов героя; процентные поправки
/// морали и защиты передаются отдельно и применяются без промежуточного
/// округления статов (ADR-022, ADR-035).
/// </summary>
/// <param name="UnitCount">Количество юнитов в атакующем стаке; 0 — удара нет.</param>
/// <param name="DamageMin">Минимальный урон одного юнита.</param>
/// <param name="DamageMax">Максимальный урон одного юнита.</param>
/// <param name="Attack">Атака стака (юнит + ветеранство + герой).</param>
/// <param name="Defense">Защита цели (юнит + ветеранство + герой).</param>
/// <param name="DamageType">Тип наносимого урона.</param>
/// <param name="TargetResistances">Резисты цели; применяется резист по типу урона.</param>
/// <param name="CritChancePercent">Шанс крита стака, %.</param>
/// <param name="CritDamagePercent">Модификатор критического урона стака, %.</param>
/// <param name="AttackPercent">Сумма процентных поправок к атаке (мораль и т. п.), %; не меньше −100.</param>
/// <param name="DefensePercent">Сумма процентных поправок к защите (защита, мораль), %; не меньше −100.</param>
/// <param name="ModifierPerMille">
/// Совокупный множитель дистанции, укрытия и баффов/дебаффов после резистов, промилле;
/// 1000 — без изменений.
/// </param>
public readonly record struct DamageRequest(
    int UnitCount,
    int DamageMin,
    int DamageMax,
    int Attack,
    int Defense,
    DamageType DamageType,
    Resistances TargetResistances,
    int CritChancePercent,
    int CritDamagePercent,
    int AttackPercent = 0,
    int DefensePercent = 0,
    int ModifierPerMille = 1000);
