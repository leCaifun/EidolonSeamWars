using System.Collections.ObjectModel;

namespace Game.Core.Battle;

/// <summary>
/// Правила морали из <c>rules/morale.json</c> (GDD §7.5, ADR-011): диапазон уровней, капы,
/// иммунные классы юнитов, таблица уровней и источники морали. Иммутабельный набор чисел.
/// </summary>
public sealed class MoraleRules
{
    private readonly ReadOnlyDictionary<int, MoraleLevelRule> _levels;
    private readonly HashSet<string> _immuneClasses;

    /// <summary>
    /// Создаёт правила морали и проверяет полноту таблицы уровней.
    /// </summary>
    /// <param name="minLevel">Минимальный уровень (не больше 0).</param>
    /// <param name="maxLevel">Максимальный уровень (не меньше 0).</param>
    /// <param name="extraActionChanceCapPercent">Кап шанса дополнительного действия/пропуска, %.</param>
    /// <param name="statModifierCapPercent">Кап модификатора атаки/защиты, %.</param>
    /// <param name="maxExtraActionsPerUnitPerRound">Максимум дополнительных действий юнита за раунд.</param>
    /// <param name="immuneClasses">Классы юнитов с иммунитетом к морали (нежить, конструкты).</param>
    /// <param name="levels">Таблица уровней: по одной строке на каждый уровень диапазона.</param>
    /// <param name="sameFaction">Источник «только фракция героя», уровней.</param>
    /// <param name="perExtraFaction">Источник «за каждую лишнюю фракцию», уровней.</param>
    /// <param name="undeadAmongLiving">Источник «нежить среди живых», уровней.</param>
    /// <param name="marshalAura">Источник «аура Маршала», уровней.</param>
    /// <param name="defendingSettlement">Источник «оборона поселения», уровней.</param>
    /// <param name="allyStackDeath">Источник «смерть союзного стака» (за каждую смерть), уровней.</param>
    /// <param name="allyStackDeathDurationRounds">Длительность штрафа за смерть союзного стака, раундов.</param>
    /// <exception cref="ArgumentException">Таблица уровней неполна или содержит повторы.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Числовое значение вне диапазона.</exception>
    public MoraleRules(
        int minLevel,
        int maxLevel,
        int extraActionChanceCapPercent,
        int statModifierCapPercent,
        int maxExtraActionsPerUnitPerRound,
        IEnumerable<string> immuneClasses,
        IEnumerable<MoraleLevelRule> levels,
        int sameFaction,
        int perExtraFaction,
        int undeadAmongLiving,
        int marshalAura,
        int defendingSettlement,
        int allyStackDeath,
        int allyStackDeathDurationRounds)
    {
        ArgumentNullException.ThrowIfNull(immuneClasses);
        ArgumentNullException.ThrowIfNull(levels);

        if (minLevel > 0 || maxLevel < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minLevel),
                minLevel,
                "Диапазон морали должен включать 0: minLevel ≤ 0 ≤ maxLevel.");
        }

        if (extraActionChanceCapPercent < 0 || extraActionChanceCapPercent > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(extraActionChanceCapPercent),
                extraActionChanceCapPercent,
                "Кап шанса морали должен быть в диапазоне 0…100.");
        }

        if (statModifierCapPercent < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(statModifierCapPercent),
                statModifierCapPercent,
                "Кап модификатора статов не может быть отрицательным.");
        }

        if (maxExtraActionsPerUnitPerRound < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxExtraActionsPerUnitPerRound),
                maxExtraActionsPerUnitPerRound,
                "Лимит дополнительных действий не может быть отрицательным.");
        }

        if (allyStackDeathDurationRounds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(allyStackDeathDurationRounds),
                allyStackDeathDurationRounds,
                "Длительность штрафа не может быть отрицательной.");
        }

        MinLevel = minLevel;
        MaxLevel = maxLevel;
        ExtraActionChanceCapPercent = extraActionChanceCapPercent;
        StatModifierCapPercent = statModifierCapPercent;
        MaxExtraActionsPerUnitPerRound = maxExtraActionsPerUnitPerRound;
        SameFaction = sameFaction;
        PerExtraFaction = perExtraFaction;
        UndeadAmongLiving = undeadAmongLiving;
        MarshalAura = marshalAura;
        DefendingSettlement = defendingSettlement;
        AllyStackDeath = allyStackDeath;
        AllyStackDeathDurationRounds = allyStackDeathDurationRounds;
        _immuneClasses = new HashSet<string>(immuneClasses, StringComparer.Ordinal);

        Dictionary<int, MoraleLevelRule> table = new();

        foreach (MoraleLevelRule rule in levels)
        {
            if (rule.Level < minLevel || rule.Level > maxLevel)
            {
                throw new ArgumentException(
                    $"Уровень морали {rule.Level} вне диапазона {minLevel}…{maxLevel}.",
                    nameof(levels));
            }

            if (string.IsNullOrWhiteSpace(rule.NameKey))
            {
                throw new ArgumentException(
                    $"У уровня морали {rule.Level} нет ключа названия.",
                    nameof(levels));
            }

            if (rule.ChancePercent < 0 || rule.ChancePercent > 100)
            {
                throw new ArgumentException(
                    $"Шанс уровня морали {rule.Level} вне диапазона 0…100: {rule.ChancePercent}.",
                    nameof(levels));
            }

            if (!table.TryAdd(rule.Level, rule))
            {
                throw new ArgumentException(
                    $"Уровень морали {rule.Level} указан более одного раза.",
                    nameof(levels));
            }
        }

        for (int level = minLevel; level <= maxLevel; level++)
        {
            if (!table.ContainsKey(level))
            {
                throw new ArgumentException(
                    $"В таблице морали нет уровня {level}.",
                    nameof(levels));
            }
        }

        _levels = new ReadOnlyDictionary<int, MoraleLevelRule>(table);
    }

    /// <summary>Минимальный уровень морали.</summary>
    public int MinLevel { get; }

    /// <summary>Максимальный уровень морали.</summary>
    public int MaxLevel { get; }

    /// <summary>Кап шанса дополнительного действия/пропуска, %.</summary>
    public int ExtraActionChanceCapPercent { get; }

    /// <summary>Кап модификатора атаки/защиты, %.</summary>
    public int StatModifierCapPercent { get; }

    /// <summary>Максимум дополнительных действий юнита за раунд.</summary>
    public int MaxExtraActionsPerUnitPerRound { get; }

    /// <summary>Источник «только фракция героя», уровней.</summary>
    public int SameFaction { get; }

    /// <summary>Источник «за каждую лишнюю фракцию», уровней.</summary>
    public int PerExtraFaction { get; }

    /// <summary>Источник «нежить среди живых», уровней.</summary>
    public int UndeadAmongLiving { get; }

    /// <summary>Источник «аура Маршала», уровней.</summary>
    public int MarshalAura { get; }

    /// <summary>Источник «оборона поселения», уровней.</summary>
    public int DefendingSettlement { get; }

    /// <summary>Источник «смерть союзного стака» (за каждую смерть), уровней.</summary>
    public int AllyStackDeath { get; }

    /// <summary>Длительность штрафа за смерть союзного стака, раундов.</summary>
    public int AllyStackDeathDurationRounds { get; }

    /// <summary>Таблица уровней морали.</summary>
    public IReadOnlyDictionary<int, MoraleLevelRule> Levels => _levels;

    /// <summary>
    /// Проверяет, относится ли класс юнита к иммунным к морали.
    /// </summary>
    /// <param name="unitClass">Класс юнита из контента.</param>
    public bool IsImmuneClass(string unitClass)
    {
        return !string.IsNullOrEmpty(unitClass) && _immuneClasses.Contains(unitClass);
    }
}
