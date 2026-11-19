namespace Game.Core.Battle;

/// <summary>
/// Последствия боя для одной стороны. Применение к карте (плен, перемещение героя, артефакты)
/// выполняет слой кампании; формулы берутся из этапа 02 без дублирования.
/// </summary>
/// <param name="Kind">Вид последствий.</param>
/// <param name="EscapeChancePercent">Шанс побега, % (0, если побег не пробовали).</param>
/// <param name="RollPerMille">Бросок побега 0…999 (−1, если бросок не делался).</param>
/// <param name="ArmyDestroyed">Армия стороны перестаёт существовать (побеждённый без выкупа).</param>
public readonly record struct BattleAftermath(
    BattleAftermathKind Kind,
    int EscapeChancePercent,
    int RollPerMille,
    bool ArmyDestroyed);
