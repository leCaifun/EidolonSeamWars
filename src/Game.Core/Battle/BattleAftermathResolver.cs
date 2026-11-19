using Game.Core.Heroes;
using Game.Core.Rng;

namespace Game.Core.Battle;

/// <summary>
/// Определяет последствия боя для стороны по правилам побега этапа 02 (<see cref="EscapeRules"/>):
/// победа, выкуп, побег или плен героя побеждённой стороны, уничтожение отряда без героя.
/// Побеждённая армия без выкупа перестаёт существовать (решение заказчика, ADR-042).
/// </summary>
public static class BattleAftermathResolver
{
    private const int NoRoll = -1;

    /// <summary>
    /// Считает последствия для стороны. Бросок побега берётся из стрима <c>battle</c> после боя
    /// и только если у побеждённой стороны есть герой, он не откупился и побег разрешён.
    /// </summary>
    /// <param name="result">Итог завершённого боя.</param>
    /// <param name="side">Сторона.</param>
    /// <param name="hasHero">Есть ли у стороны герой-главнокомандующий.</param>
    /// <param name="heroLogistics">Логистика героя стороны.</param>
    /// <param name="enemyLogistics">Логистика героя противника (0, если героя нет).</param>
    /// <param name="isSiegeDefender">Сторона — защитник осады (побег запрещён).</param>
    /// <param name="ransomPaid">Побеждённый герой откупился (армия сохраняется, бросок побега не делается).</param>
    /// <param name="battleRandom">Стрим <c>battle</c>.</param>
    /// <exception cref="ArgumentNullException">Итог или стрим не заданы.</exception>
    public static BattleAftermath Resolve(
        BattleResult result,
        BattleSide side,
        bool hasHero,
        int heroLogistics,
        int enemyLogistics,
        bool isSiegeDefender,
        bool ransomPaid,
        SeededRandom battleRandom)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(battleRandom);

        if (result.IsVictory(side))
        {
            return new BattleAftermath(BattleAftermathKind.Victory, 0, NoRoll, false);
        }

        if (!hasHero)
        {
            return new BattleAftermath(BattleAftermathKind.ArmyDestroyed, 0, NoRoll, true);
        }

        if (ransomPaid)
        {
            return new BattleAftermath(BattleAftermathKind.HeroRansomed, 0, NoRoll, false);
        }

        int chance = EscapeRules.EscapeChancePercent(heroLogistics, enemyLogistics);

        if (isSiegeDefender)
        {
            return new BattleAftermath(BattleAftermathKind.EscapeForbidden, chance, NoRoll, true);
        }

        int roll = battleRandom.RollPerMille();
        EscapeResult escape = EscapeRules.TryEscape(heroLogistics, enemyLogistics, isSiegeDefender, roll);

        BattleAftermathKind kind = escape == EscapeResult.Escaped
            ? BattleAftermathKind.HeroEscaped
            : BattleAftermathKind.HeroCaptured;

        return new BattleAftermath(kind, chance, roll, true);
    }
}
