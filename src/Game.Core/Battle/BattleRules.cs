namespace Game.Core.Battle;

/// <summary>
/// Боевые константы из <c>rules/battle.json</c> (GDD §7). Иммутабельный набор чисел:
/// размеры поля, защита, контратака, модификатор «атака − защита» и капы крита.
/// Все значения целые; доли — в процентах или промилле (ADR-017).
/// </summary>
public sealed class BattleRules
{
    /// <summary>
    /// Создаёт набор боевых правил и проверяет его целостность.
    /// </summary>
    /// <param name="fieldWidth">Ширина поля, гексов.</param>
    /// <param name="fieldHeight">Высота поля, гексов.</param>
    /// <param name="heroOnBattlefield">Выводится ли герой на поле (в MVP всегда <c>false</c>, ADR-007).</param>
    /// <param name="defendDefenseBonusPercent">Бонус защиты от действия «Защита», %.</param>
    /// <param name="defendResistBonusPercent">Бонус резистов от защиты, % (в MVP 0, ADR-008).</param>
    /// <param name="defendInitiativePenalty">Штраф инициативы за защиту (в MVP 0).</param>
    /// <param name="retaliationPerRound">Контратак за раунд (ADR-009).</param>
    /// <param name="retaliationIsAction">Считается ли контратака действием (в MVP <c>false</c>).</param>
    /// <param name="waitMovesToEndOfRound">Ожидание переносит ход в конец раунда.</param>
    /// <param name="waitAffectsNextRound">Ожидание влияет на группу в следующем раунде.</param>
    /// <param name="attackDefenseModifierPerPoint">Изменение модификатора за единицу «атака − защита», промилле.</param>
    /// <param name="attackDefenseModifierMin">Нижняя граница модификатора, промилле.</param>
    /// <param name="attackDefenseModifierMax">Верхняя граница модификатора, промилле.</param>
    /// <param name="critChanceCapPercent">Кап шанса крита, %.</param>
    /// <param name="critDamageCapPercent">Кап критического урона, %.</param>
    /// <param name="rangedLongDistanceFieldPercent">Доля длины поля, с которой начинается большая дистанция, %.</param>
    /// <param name="rangedLongDistancePenaltyPercent">Штраф урона на большой дистанции, %.</param>
    /// <param name="rangedObstaclePenaltyPercent">Штраф урона при препятствии на линии огня, %.</param>
    /// <param name="rangedMeleePenaltyPercent">Штраф урона стрелка в ближнем бою, %.</param>
    /// <param name="attackMovementCost">Очков движения, требуемых для любой атаки.</param>
    /// <exception cref="ArgumentOutOfRangeException">Значение вне допустимого диапазона.</exception>
    public BattleRules(
        int fieldWidth,
        int fieldHeight,
        bool heroOnBattlefield,
        int defendDefenseBonusPercent,
        int defendResistBonusPercent,
        int defendInitiativePenalty,
        int retaliationPerRound,
        bool retaliationIsAction,
        bool waitMovesToEndOfRound,
        bool waitAffectsNextRound,
        int attackDefenseModifierPerPoint,
        int attackDefenseModifierMin,
        int attackDefenseModifierMax,
        int critChanceCapPercent,
        int critDamageCapPercent,
        int rangedLongDistanceFieldPercent,
        int rangedLongDistancePenaltyPercent,
        int rangedObstaclePenaltyPercent,
        int rangedMeleePenaltyPercent,
        int attackMovementCost)
    {
        FieldWidth = RequireAtLeast(fieldWidth, 1, nameof(fieldWidth));
        FieldHeight = RequireAtLeast(fieldHeight, 1, nameof(fieldHeight));
        HeroOnBattlefield = heroOnBattlefield;
        DefendDefenseBonusPercent = RequireAtLeast(defendDefenseBonusPercent, 0, nameof(defendDefenseBonusPercent));
        DefendResistBonusPercent = RequireAtLeast(defendResistBonusPercent, 0, nameof(defendResistBonusPercent));
        DefendInitiativePenalty = RequireAtLeast(defendInitiativePenalty, 0, nameof(defendInitiativePenalty));
        RetaliationPerRound = RequireAtLeast(retaliationPerRound, 0, nameof(retaliationPerRound));
        RetaliationIsAction = retaliationIsAction;
        WaitMovesToEndOfRound = waitMovesToEndOfRound;
        WaitAffectsNextRound = waitAffectsNextRound;
        AttackDefenseModifierPerPoint = RequireAtLeast(
            attackDefenseModifierPerPoint,
            0,
            nameof(attackDefenseModifierPerPoint));
        AttackDefenseModifierMin = RequireAtLeast(attackDefenseModifierMin, 0, nameof(attackDefenseModifierMin));
        AttackDefenseModifierMax = RequireAtLeast(
            attackDefenseModifierMax,
            attackDefenseModifierMin,
            nameof(attackDefenseModifierMax));
        CritChanceCapPercent = RequireRange(critChanceCapPercent, 0, 100, nameof(critChanceCapPercent));
        CritDamageCapPercent = RequireAtLeast(critDamageCapPercent, 100, nameof(critDamageCapPercent));
        RangedLongDistanceFieldPercent = RequireRange(
            rangedLongDistanceFieldPercent,
            1,
            100,
            nameof(rangedLongDistanceFieldPercent));
        RangedLongDistancePenaltyPercent = RequireRange(
            rangedLongDistancePenaltyPercent,
            0,
            100,
            nameof(rangedLongDistancePenaltyPercent));
        RangedObstaclePenaltyPercent = RequireRange(
            rangedObstaclePenaltyPercent,
            0,
            100,
            nameof(rangedObstaclePenaltyPercent));
        RangedMeleePenaltyPercent = RequireRange(
            rangedMeleePenaltyPercent,
            0,
            100,
            nameof(rangedMeleePenaltyPercent));
        AttackMovementCost = RequireAtLeast(attackMovementCost, 0, nameof(attackMovementCost));
    }

    /// <summary>Ширина поля боя, гексов.</summary>
    public int FieldWidth { get; }

    /// <summary>Высота поля боя, гексов.</summary>
    public int FieldHeight { get; }

    /// <summary>Выводится ли герой на поле; в MVP <c>false</c>.</summary>
    public bool HeroOnBattlefield { get; }

    /// <summary>Бонус защиты от действия «Защита», %.</summary>
    public int DefendDefenseBonusPercent { get; }

    /// <summary>Бонус резистов от защиты, %; в MVP 0.</summary>
    public int DefendResistBonusPercent { get; }

    /// <summary>Штраф инициативы за защиту; в MVP 0.</summary>
    public int DefendInitiativePenalty { get; }

    /// <summary>Контратак за раунд.</summary>
    public int RetaliationPerRound { get; }

    /// <summary>Считается ли контратака действием; в MVP <c>false</c>.</summary>
    public bool RetaliationIsAction { get; }

    /// <summary>Ожидание переносит ход в конец текущего раунда.</summary>
    public bool WaitMovesToEndOfRound { get; }

    /// <summary>Ожидание влияет на группу хода в следующем раунде.</summary>
    public bool WaitAffectsNextRound { get; }

    /// <summary>Изменение модификатора за единицу «атака − защита», промилле.</summary>
    public int AttackDefenseModifierPerPoint { get; }

    /// <summary>Нижняя граница модификатора «атака − защита», промилле.</summary>
    public int AttackDefenseModifierMin { get; }

    /// <summary>Верхняя граница модификатора «атака − защита», промилле.</summary>
    public int AttackDefenseModifierMax { get; }

    /// <summary>Кап шанса крита, %.</summary>
    public int CritChanceCapPercent { get; }

    /// <summary>Кап критического урона, %.</summary>
    public int CritDamageCapPercent { get; }

    /// <summary>Доля длины поля, с которой начинается большая дистанция, %.</summary>
    public int RangedLongDistanceFieldPercent { get; }

    /// <summary>Штраф урона дальней атаки на большой дистанции, %.</summary>
    public int RangedLongDistancePenaltyPercent { get; }

    /// <summary>Штраф урона дальней атаки при препятствии на линии огня, %.</summary>
    public int RangedObstaclePenaltyPercent { get; }

    /// <summary>Штраф урона стрелка в ближнем бою (удар вплотную и контратака), %.</summary>
    public int RangedMeleePenaltyPercent { get; }

    /// <summary>Очков движения, требуемых для любой атаки (по умолчанию 1).</summary>
    public int AttackMovementCost { get; }

    /// <summary>
    /// Дистанция в гексах, начиная с которой дальняя атака получает штраф большой дистанции:
    /// <c>ceil(fieldWidth × процент / 100)</c> (10 гексов при 80 % → 8; 15 → 12; 13 → 11).
    /// </summary>
    public int LongDistanceThreshold => (FieldWidth * RangedLongDistanceFieldPercent + 99) / 100;

    private static int RequireAtLeast(int value, int minimum, string parameterName)
    {
        if (value < minimum)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Значение должно быть не меньше {minimum}.");
        }

        return value;
    }

    private static int RequireRange(int value, int minimum, int maximum, string parameterName)
    {
        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Значение должно быть в диапазоне {minimum}…{maximum}.");
        }

        return value;
    }
}
