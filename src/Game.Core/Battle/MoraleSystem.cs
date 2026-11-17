using Game.Core.Diagnostics;
using Game.Core.Domain;
using Game.Core.Rng;

namespace Game.Core.Battle;

/// <summary>
/// Мораль по GDD §7.5 и ADR-011, ADR-037: уровень −4…+4, экспоненциальные шанс дополнительного
/// действия/пропуска и модификатор атаки/защиты с капами 35 % и 16 %, иммунитет нежити и конструктов.
/// Числа берутся из таблицы <see cref="MoraleRules"/> и ограничиваются капами; формулы
/// <c>min(35, 4×2^(|L|−1))</c> и <c>2×2^(|L|−1)</c> проверяются тестами.
/// </summary>
/// <remarks>
/// Контракт реплея: бросок морали использует стрим <c>battle</c> и расходуется ровно один раз —
/// только если юнит не иммунен, уровень не нулевой и (для положительной морали) лимит и запрет
/// цепочек дополнительных действий не исчерпаны. Остальные случаи бросков не расходуют.
/// </remarks>
public sealed class MoraleSystem
{
    /// <summary>Класс юнита «нежить» (токен контента) для источника «нежить среди живых».</summary>
    public const string UndeadClass = "undead";

    private const int PerMille = 1000;
    private const int Percent = 100;
    private const int ChanceToPerMille = PerMille / Percent;

    private readonly MoraleRules _rules;
    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт систему морали.
    /// </summary>
    /// <param name="rules">Правила морали.</param>
    /// <param name="logger">Необязательный журнал; категория <c>Battle</c>.</param>
    /// <exception cref="ArgumentNullException">Правила не заданы.</exception>
    public MoraleSystem(MoraleRules rules, IGameLogger? logger = null)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _logger = logger;
    }

    /// <summary>Ограничивает уровень морали диапазоном правил.</summary>
    public int ClampLevel(int level)
    {
        return Math.Clamp(level, _rules.MinLevel, _rules.MaxLevel);
    }

    /// <summary>
    /// Строит входы морали по составу армии. «Своя фракция» даёт бонус, только если в армии
    /// исключительно юниты фракции героя; каждая лишняя фракция даёт штраф. Без героя бонуса нет,
    /// лишними считаются все фракции кроме первой.
    /// </summary>
    /// <param name="armyUnits">Определения юнитов всех стаков армии (по одному на стак).</param>
    /// <param name="heroFactionId">Фракция героя; <c>null</c> или пусто — армия без героя.</param>
    /// <param name="marshalAura">Действует аура Маршала.</param>
    /// <param name="defendingSettlement">Армия обороняет поселение.</param>
    /// <param name="activeAllyStackDeaths">Число действующих штрафов за смерть союзных стаков.</param>
    /// <exception cref="ArgumentNullException">Список юнитов не задан.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Число смертей отрицательно.</exception>
    public MoraleContext BuildContext(
        IEnumerable<UnitDef> armyUnits,
        string? heroFactionId,
        bool marshalAura,
        bool defendingSettlement,
        int activeAllyStackDeaths)
    {
        ArgumentNullException.ThrowIfNull(armyUnits);

        if (activeAllyStackDeaths < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(activeAllyStackDeaths),
                activeAllyStackDeaths,
                "Число смертей союзных стаков не может быть отрицательным.");
        }

        List<UnitDef> units = armyUnits.ToList();
        HashSet<string> factions = new(units.Select(unit => unit.FactionId), StringComparer.Ordinal);

        int foreign;
        bool heroFactionOnly;

        if (string.IsNullOrEmpty(heroFactionId))
        {
            foreign = Math.Max(0, factions.Count - 1);
            heroFactionOnly = false;
        }
        else
        {
            foreign = factions.Count(faction => !string.Equals(faction, heroFactionId, StringComparison.Ordinal));
            heroFactionOnly = units.Count > 0 && foreign == 0;
        }

        bool hasUndead = units.Any(unit => string.Equals(unit.UnitClass, UndeadClass, StringComparison.Ordinal));
        bool hasLiving = units.Any(unit => !IsImmune(unit));

        return new MoraleContext(
            heroFactionOnly,
            foreign,
            hasUndead && hasLiving,
            marshalAura,
            defendingSettlement,
            activeAllyStackDeaths);
    }

    /// <summary>
    /// Считает уровень морали армии как сумму источников с клампом −4…+4.
    /// </summary>
    /// <param name="context">Входы морали.</param>
    /// <exception cref="ArgumentOutOfRangeException">Отрицательные счётчики во входах.</exception>
    public int ComputeLevel(MoraleContext context)
    {
        if (context.ForeignFactionCount < 0 || context.ActiveAllyStackDeaths < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(context),
                context,
                "Счётчики фракций и смертей союзников не могут быть отрицательными.");
        }

        long sum = 0;

        if (context.HeroFactionOnly)
        {
            sum += _rules.SameFaction;
        }

        sum += (long)_rules.PerExtraFaction * context.ForeignFactionCount;

        if (context.UndeadAmongLiving)
        {
            sum += _rules.UndeadAmongLiving;
        }

        if (context.MarshalAura)
        {
            sum += _rules.MarshalAura;
        }

        if (context.DefendingSettlement)
        {
            sum += _rules.DefendingSettlement;
        }

        sum += (long)_rules.AllyStackDeath * context.ActiveAllyStackDeaths;

        return (int)Math.Clamp(sum, _rules.MinLevel, _rules.MaxLevel);
    }

    /// <summary>Иммунен ли юнит к морали (нежить и конструкты, ADR-011).</summary>
    public bool IsImmune(UnitDef unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return _rules.IsImmuneClass(unit.UnitClass);
    }

    /// <summary>Эффективный уровень морали юнита: 0 для иммунных, иначе кламп.</summary>
    public int GetEffectiveLevel(UnitDef unit, int armyLevel)
    {
        return IsImmune(unit) ? 0 : ClampLevel(armyLevel);
    }

    /// <summary>
    /// Шанс дополнительного действия (уровень &gt; 0) или пропуска (уровень &lt; 0), %; не выше капа.
    /// </summary>
    public int GetChancePercent(int level)
    {
        return Math.Min(_rules.ExtraActionChanceCapPercent, _rules.Levels[ClampLevel(level)].ChancePercent);
    }

    /// <summary>
    /// Модификатор атаки и защиты, %; знак совпадает со знаком уровня, модуль не выше капа.
    /// </summary>
    public int GetStatModifierPercent(int level)
    {
        int raw = _rules.Levels[ClampLevel(level)].StatModifierPercent;
        int magnitude = Math.Min(_rules.StatModifierCapPercent, Math.Abs(raw));
        return raw < 0 ? -magnitude : magnitude;
    }

    /// <summary>Ключ локализации описательного названия уровня.</summary>
    public string GetLevelNameKey(int level)
    {
        return _rules.Levels[ClampLevel(level)].NameKey;
    }

    /// <summary>
    /// Бросает мораль в начале хода юнита.
    /// </summary>
    /// <param name="unit">Юнит, чей ход начинается.</param>
    /// <param name="armyLevel">Уровень морали армии.</param>
    /// <param name="extraActionsUsedThisRound">Сколько дополнительных действий юнит уже получил в раунде.</param>
    /// <param name="isExtraAction">Ход сам является дополнительным действием (цепочки запрещены).</param>
    /// <param name="battleRandom">Стрим <c>battle</c>.</param>
    /// <returns>Исход броска.</returns>
    /// <exception cref="ArgumentNullException">Юнит или стрим не заданы.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Счётчик дополнительных действий отрицателен.</exception>
    public MoraleRoll RollAction(
        UnitDef unit,
        int armyLevel,
        int extraActionsUsedThisRound,
        bool isExtraAction,
        SeededRandom battleRandom)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(battleRandom);

        if (extraActionsUsedThisRound < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(extraActionsUsedThisRound),
                extraActionsUsedThisRound,
                "Число использованных дополнительных действий не может быть отрицательным.");
        }

        int level = GetEffectiveLevel(unit, armyLevel);

        if (level == 0)
        {
            return MoraleRoll.None;
        }

        if (level > 0
            && (isExtraAction || extraActionsUsedThisRound >= _rules.MaxExtraActionsPerUnitPerRound))
        {
            return MoraleRoll.None;
        }

        int chance = GetChancePercent(level);
        int roll = battleRandom.RollPerMille();
        bool triggered = roll < chance * ChanceToPerMille;

        MoraleRoll result = !triggered
            ? MoraleRoll.None
            : level > 0 ? MoraleRoll.ExtraAction : MoraleRoll.Skip;

        LogRoll(unit, level, chance, roll, result);

        return result;
    }

    private void LogRoll(UnitDef unit, int level, int chance, int roll, MoraleRoll result)
    {
        if (_logger is null || !_logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        _logger.Log(LogEvent.Create(
            LogLevel.Debug,
            "Battle",
            "Бросок морали.",
            fields: new Dictionary<string, string>
            {
                ["unitId"] = unit.Id,
                ["moraleLevel"] = level.ToString(),
                ["chancePercent"] = chance.ToString(),
                ["roll"] = roll.ToString(),
                ["result"] = result.ToString()
            }));
    }
}
