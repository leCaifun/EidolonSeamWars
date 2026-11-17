namespace Game.Core.Battle;

/// <summary>
/// Подробный результат расчёта урона: все промежуточные шаги формулы сохранены
/// для журнала боя, тестов и реплея.
/// </summary>
/// <param name="UnitRoll">Бросок урона одного юнита (0, если удара не было).</param>
/// <param name="CritRoll">Бросок крита в промилле 0…999 (−1, если удара не было).</param>
/// <param name="BaseDamage"><c>count × UnitRoll</c>.</param>
/// <param name="AttackDefenseModifierPerMille">Модификатор «атака − защита» после клампа, промилле.</param>
/// <param name="RawDamage">Урон после модификатора «атака − защита».</param>
/// <param name="ResistPercent">Применённый резист цели по типу урона, %.</param>
/// <param name="ResistedDamage">Урон после резиста.</param>
/// <param name="ModifiedDamage">Урон после совокупного множителя дистанции/укрытия/баффов.</param>
/// <param name="IsCritical">Сработал ли крит.</param>
/// <param name="FinalDamage">Итоговый урон по цели.</param>
public readonly record struct DamageResult(
    int UnitRoll,
    int CritRoll,
    long BaseDamage,
    int AttackDefenseModifierPerMille,
    long RawDamage,
    int ResistPercent,
    long ResistedDamage,
    long ModifiedDamage,
    bool IsCritical,
    int FinalDamage);
