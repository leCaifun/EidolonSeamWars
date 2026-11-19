using Game.Core.Diagnostics;
using Game.Core.Numerics;
using Game.Core.Rng;

namespace Game.Core.Battle;

/// <summary>
/// Целочисленный расчёт урона по GDD §7.4 в промилле (ADR-017, ADR-022, ADR-035).
/// Порядок шагов: <c>base = count × бросок</c>; модификатор «атака − защита» с клампом;
/// резист по типу урона; совокупный множитель дистанции/укрытия/баффов; крит; нижняя граница 1.
/// Каждый шаг округляется один раз, «половина от нуля»; плавающая точка не используется.
/// </summary>
/// <remarks>
/// Контракт реплея: расчёт использует стрим <c>battle</c> и берёт ровно два броска в фиксированном
/// порядке — сначала урон одного юнита, затем крит; крит-бросок расходуется всегда, даже при шансе 0.
/// Стак без юнитов не бросает кубики и наносит 0 урона.
/// </remarks>
public sealed class DamageCalculator
{
    /// <summary>Нижняя граница итогового урона при ненулевой базе и отсутствии полного иммунитета.</summary>
    public const int MinimumDamage = 1;

    private const int PerMille = 1000;
    private const int Percent = 100;
    private const int MinimumStatPercent = -100;
    private const int CritChanceToPerMille = PerMille / Percent;

    private readonly BattleRules _rules;
    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт калькулятор урона.
    /// </summary>
    /// <param name="rules">Боевые правила (кап крита, модификатор «атака − защита»).</param>
    /// <param name="logger">Необязательный журнал; категория <c>Battle</c>.</param>
    /// <exception cref="ArgumentNullException">Правила не заданы.</exception>
    public DamageCalculator(BattleRules rules, IGameLogger? logger = null)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _logger = logger;
    }

    /// <summary>
    /// Считает модификатор «атака − защита» в промилле:
    /// <c>clamp(1000 + perPoint × (A×(100+A%) − D×(100+D%)) / 100, min, max)</c>.
    /// Процентные поправки применяются к статам без промежуточного округления.
    /// </summary>
    /// <param name="request">Запрос на расчёт урона.</param>
    /// <exception cref="ArgumentOutOfRangeException">Поля запроса вне допустимых диапазонов.</exception>
    public int GetAttackDefenseModifierPerMille(DamageRequest request)
    {
        Validate(request);

        long attackScaled = (long)request.Attack * (Percent + request.AttackPercent);
        long defenseScaled = (long)request.Defense * (Percent + request.DefensePercent);
        long adjustment = IntegerMath.MulDiv(
            attackScaled - defenseScaled,
            _rules.AttackDefenseModifierPerPoint,
            Percent);

        long modifier = PerMille + adjustment;

        return (int)Math.Clamp(
            modifier,
            _rules.AttackDefenseModifierMin,
            _rules.AttackDefenseModifierMax);
    }

    /// <summary>
    /// Совокупный множитель дальней атаки в промилле (ADR-037): на большой дистанции (с
    /// <see cref="BattleRules.LongDistanceThreshold"/>) и при препятствии на линии огня действуют
    /// штрафы, которые суммируются. Результат передаётся в <see cref="DamageRequest.ModifierPerMille"/>.
    /// </summary>
    /// <param name="distance">Расстояние от стрелка до цели, гексов.</param>
    /// <param name="obstacleBetween">Есть ли препятствие или юнит на линии огня.</param>
    /// <exception cref="ArgumentOutOfRangeException">Расстояние отрицательно.</exception>
    public int GetRangedModifierPerMille(int distance, bool obstacleBetween)
    {
        RequireAtLeast(distance, 0, nameof(distance));

        int penaltyPercent = 0;

        if (distance >= _rules.LongDistanceThreshold)
        {
            penaltyPercent += _rules.RangedLongDistancePenaltyPercent;
        }

        if (obstacleBetween)
        {
            penaltyPercent += _rules.RangedObstaclePenaltyPercent;
        }

        return Math.Max(0, PerMille - penaltyPercent * CritChanceToPerMille);
    }

    /// <summary>
    /// Множитель урона стрелка в ближнем бою (удар по соседнему гексу и контратака) в промилле:
    /// при штрафе 75 % — 250.
    /// </summary>
    public int GetShooterMeleeModifierPerMille()
    {
        return Math.Max(0, PerMille - _rules.RangedMeleePenaltyPercent * CritChanceToPerMille);
    }

    /// <summary>
    /// Рассчитывает урон одного удара, расходуя броски стрима <c>battle</c>.
    /// </summary>
    /// <param name="request">Запрос на расчёт урона.</param>
    /// <param name="battleRandom">Стрим <c>battle</c>; порядок бросков — контракт реплея.</param>
    /// <returns>Итог с промежуточными шагами формулы.</returns>
    /// <exception cref="ArgumentNullException">Стрим не задан.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Поля запроса вне допустимых диапазонов.</exception>
    public DamageResult Calculate(DamageRequest request, SeededRandom battleRandom)
    {
        ArgumentNullException.ThrowIfNull(battleRandom);
        Validate(request);

        if (request.UnitCount == 0)
        {
            return new DamageResult(0, -1, 0, PerMille, 0, 0, 0, 0, false, 0);
        }

        int unitRoll = battleRandom.Next(request.DamageMin, checked(request.DamageMax + 1));
        int critRoll = battleRandom.RollPerMille();

        long baseDamage = checked((long)request.UnitCount * unitRoll);
        int modifier = GetAttackDefenseModifierPerMille(request);
        long rawDamage = IntegerMath.MulDiv(baseDamage, modifier, PerMille);

        int resistPercent = request.TargetResistances.Get(request.DamageType);
        long resistedDamage = IntegerMath.MulDiv(rawDamage, Percent - resistPercent, Percent);
        long modifiedDamage = IntegerMath.MulDiv(resistedDamage, request.ModifierPerMille, PerMille);

        int critChance = Math.Min(request.CritChancePercent, _rules.CritChanceCapPercent);
        bool isCritical = critRoll < critChance * CritChanceToPerMille;
        long finalDamage = modifiedDamage;

        if (isCritical)
        {
            int critDamage = Math.Min(request.CritDamagePercent, _rules.CritDamageCapPercent);
            finalDamage = IntegerMath.MulDiv(modifiedDamage, critDamage, Percent);
        }

        if (baseDamage > 0
            && resistPercent < Percent
            && request.ModifierPerMille > 0
            && finalDamage < MinimumDamage)
        {
            finalDamage = MinimumDamage;
        }

        DamageResult result = new(
            unitRoll,
            critRoll,
            baseDamage,
            modifier,
            rawDamage,
            resistPercent,
            resistedDamage,
            modifiedDamage,
            isCritical,
            checked((int)finalDamage));

        LogResult(request, result);

        return result;
    }

    private static void Validate(DamageRequest request)
    {
        RequireAtLeast(request.UnitCount, 0, nameof(request.UnitCount));
        RequireAtLeast(request.DamageMin, 0, nameof(request.DamageMin));
        RequireAtLeast(request.DamageMax, request.DamageMin, nameof(request.DamageMax));
        RequireAtLeast(request.Attack, 0, nameof(request.Attack));
        RequireAtLeast(request.Defense, 0, nameof(request.Defense));
        RequireRange(request.CritChancePercent, 0, Percent, nameof(request.CritChancePercent));
        RequireAtLeast(request.CritDamagePercent, Percent, nameof(request.CritDamagePercent));
        RequireAtLeast(request.AttackPercent, MinimumStatPercent, nameof(request.AttackPercent));
        RequireAtLeast(request.DefensePercent, MinimumStatPercent, nameof(request.DefensePercent));
        RequireAtLeast(request.ModifierPerMille, 0, nameof(request.ModifierPerMille));
    }

    private static void RequireAtLeast(int value, int minimum, string name)
    {
        if (value < minimum)
        {
            throw new ArgumentOutOfRangeException(
                name,
                value,
                $"Поле запроса урона «{name}» должно быть не меньше {minimum}.");
        }
    }

    private static void RequireRange(int value, int minimum, int maximum, string name)
    {
        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                name,
                value,
                $"Поле запроса урона «{name}» должно быть в диапазоне {minimum}…{maximum}.");
        }
    }

    private void LogResult(DamageRequest request, DamageResult result)
    {
        if (_logger is null || !_logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        _logger.Log(LogEvent.Create(
            LogLevel.Debug,
            "Battle",
            "Рассчитан урон удара.",
            fields: new Dictionary<string, string>
            {
                ["unitCount"] = request.UnitCount.ToString(),
                ["damageType"] = request.DamageType.ToString(),
                ["unitRoll"] = result.UnitRoll.ToString(),
                ["critRoll"] = result.CritRoll.ToString(),
                ["baseDamage"] = result.BaseDamage.ToString(),
                ["attackDefenseModifier"] = result.AttackDefenseModifierPerMille.ToString(),
                ["rawDamage"] = result.RawDamage.ToString(),
                ["resistPercent"] = result.ResistPercent.ToString(),
                ["resistedDamage"] = result.ResistedDamage.ToString(),
                ["modifiedDamage"] = result.ModifiedDamage.ToString(),
                ["critical"] = result.IsCritical ? "true" : "false",
                ["finalDamage"] = result.FinalDamage.ToString()
            }));
    }
}
