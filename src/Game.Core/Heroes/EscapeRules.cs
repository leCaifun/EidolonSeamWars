using Game.Core.Domain;

namespace Game.Core.Heroes;

/// <summary>
/// Чистые формулы побега, плена и выкупа (GDD §7.7, ADR-012).
/// Все коэффициенты — именованные константы; магических чисел нет.
/// </summary>
public static class EscapeRules
{
    /// <summary>Базовый шанс побега, %.</summary>
    public const int BaseEscapeChancePercent = 65;

    /// <summary>Бонус к шансу побега за каждую единицу логистики героя, п.п.</summary>
    public const int HeroLogisticsBonusPerPoint = 3;

    /// <summary>Штраф к шансу побега за каждую единицу логистики противника, п.п.</summary>
    public const int EnemyLogisticsPenaltyPerPoint = 2;

    /// <summary>Минимальный шанс побега, %.</summary>
    public const int MinEscapeChancePercent = 20;

    /// <summary>Максимальный шанс побега, %.</summary>
    public const int MaxEscapeChancePercent = 90;

    /// <summary>Срок плена при провале побега, дней.</summary>
    public const int CaptivityDays = 5;

    /// <summary>Надбавка к выкупу за уровень героя, золото.</summary>
    public const int HeroLevelRansomGold = 1000;

    /// <summary>Надбавка к выкупу за артефакт, золото.</summary>
    public const int ArtifactRansomGold = 100;

    /// <summary>
    /// Шанс побега: <c>clamp(65 + логистика героя × 3 − логистика врага × 2, 20, 90)%</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Отрицательная логистика.</exception>
    public static int EscapeChancePercent(int heroLogistics, int enemyLogistics)
    {
        if (heroLogistics < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(heroLogistics), heroLogistics, "Логистика героя не может быть отрицательной.");
        }

        if (enemyLogistics < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(enemyLogistics), enemyLogistics, "Логистика противника не может быть отрицательной.");
        }

        int chance = BaseEscapeChancePercent
            + (heroLogistics * HeroLogisticsBonusPerPoint)
            - (enemyLogistics * EnemyLogisticsPenaltyPerPoint);

        return Math.Clamp(chance, MinEscapeChancePercent, MaxEscapeChancePercent);
    }

    /// <summary>
    /// Разрешает или запрещает побег. Защитник осады бежать не может (ADR-012).
    /// </summary>
    public static EscapeResult TryEscape(
        int heroLogistics,
        int enemyLogistics,
        bool isSiegeDefender,
        int rollPerMille)
    {
        if (rollPerMille < 0 || rollPerMille > 999)
        {
            throw new ArgumentOutOfRangeException(nameof(rollPerMille), rollPerMille, "Бросок должен быть в диапазоне 0…999 промилле.");
        }

        if (isSiegeDefender)
        {
            return EscapeResult.Forbidden;
        }

        int chancePercent = EscapeChancePercent(heroLogistics, enemyLogistics);
        return rollPerMille < chancePercent * 10
            ? EscapeResult.Escaped
            : EscapeResult.Captured;
    }

    /// <summary>
    /// Строит счёт на выкуп героя.
    /// </summary>
    /// <param name="heroLevel">Уровень героя.</param>
    /// <param name="artifactCount">Число артефактов.</param>
    /// <param name="isSiegeDefender">Является ли герой защитником осады.</param>
    /// <param name="stacks">Выжившая армия.</param>
    /// <param name="unitPrice">Функция цены одного юнита по его ID.</param>
    /// <exception cref="ArgumentNullException">Армия или функция цены не заданы.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Уровень, артефакты или цена отрицательны.</exception>
    /// <exception cref="OverflowException">Стоимость не помещается в int.</exception>
    public static RansomQuote CreateRansomQuote(
        int heroLevel,
        int artifactCount,
        bool isSiegeDefender,
        IEnumerable<Stack> stacks,
        Func<string, int> unitPrice)
    {
        ArgumentNullException.ThrowIfNull(stacks);
        ArgumentNullException.ThrowIfNull(unitPrice);

        if (heroLevel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(heroLevel), heroLevel, "Уровень героя не может быть меньше 1.");
        }

        if (artifactCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(artifactCount), artifactCount, "Число артефактов не может быть отрицательным.");
        }

        if (isSiegeDefender)
        {
            return RansomQuote.Forbidden;
        }

        int unitsCost = 0;
        foreach (Stack stack in stacks)
        {
            ArgumentNullException.ThrowIfNull(stack, nameof(stacks));

            int price = unitPrice(stack.UnitDefId);
            if (price < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unitPrice), price, "Цена юнита не может быть отрицательной.");
            }

            unitsCost = checked(unitsCost + (price * stack.Count));
        }

        int heroLevelCost = checked(heroLevel * HeroLevelRansomGold);
        int artifactsCost = checked(artifactCount * ArtifactRansomGold);
        int total = checked(unitsCost + heroLevelCost + artifactsCost);

        return new RansomQuote(total, unitsCost, heroLevelCost, artifactsCost, isAllowed: true);
    }
}
