using Game.Core.Content.Dto;
using Game.Core.Diagnostics;
using Game.Core.Rng;

namespace Game.Core.Domain;

/// <summary>
/// Прогрессия героя по ADR-025: опыт, пороги уровней, повышение характеристик
/// и очки навыка. Все числа берутся из <c>rules/progression.json</c>; мягкий
/// предел уровня задаёт сценарий, а не домен.
/// Кривая: <c>XpToNext(level) = coefficient × level × (level + 1)</c>.
/// </summary>
public sealed class HeroProgression
{
    private readonly int _experienceCoefficient;
    private readonly int _skillPointsPerLevel;
    private readonly int _scenarioSoftCap;
    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт прогрессию с явными параметрами.
    /// </summary>
    /// <param name="experienceCoefficient">Коэффициент квадратичной кривой опыта, больше нуля.</param>
    /// <param name="skillPointsPerLevel">Очки навыка за уровень, не меньше нуля.</param>
    /// <param name="scenarioSoftCap">Мягкий предел уровня сценария; 0 или меньше — без предела.</param>
    /// <param name="logger">Необязательный журнал.</param>
    /// <exception cref="ArgumentOutOfRangeException">Коэффициент или очки навыка вне диапазона.</exception>
    public HeroProgression(
        int experienceCoefficient,
        int skillPointsPerLevel,
        int scenarioSoftCap,
        IGameLogger? logger = null)
    {
        if (experienceCoefficient < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(experienceCoefficient),
                experienceCoefficient,
                "Коэффициент опыта должен быть положительным.");
        }

        if (skillPointsPerLevel < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillPointsPerLevel),
                skillPointsPerLevel,
                "Очки навыка за уровень не могут быть отрицательными.");
        }

        _experienceCoefficient = experienceCoefficient;
        _skillPointsPerLevel = skillPointsPerLevel;
        _scenarioSoftCap = scenarioSoftCap;
        _logger = logger;
    }

    /// <summary>Коэффициент квадратичной кривой опыта.</summary>
    public int ExperienceCoefficient => _experienceCoefficient;

    /// <summary>Очки навыка за уровень.</summary>
    public int SkillPointsPerLevel => _skillPointsPerLevel;

    /// <summary>Мягкий предел уровня сценария; 0 или меньше — без предела.</summary>
    public int ScenarioSoftCap => _scenarioSoftCap;

    /// <summary>
    /// Создаёт прогрессию из правил контента <c>rules/progression.json</c>.
    /// </summary>
    /// <param name="rules">Правила прогрессии.</param>
    /// <param name="logger">Необязательный журнал.</param>
    /// <exception cref="ArgumentNullException">Правила не заданы.</exception>
    /// <exception cref="InvalidOperationException">Формула опыта не поддерживается.</exception>
    public static HeroProgression FromRules(ProgressionRulesDto rules, IGameLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(rules);

        if (!string.Equals(rules.XpToNextFormula.Type, "quadratic", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Формула опыта «{rules.XpToNextFormula.Type}» не поддерживается; ожидается quadratic.");
        }

        return new HeroProgression(
            rules.XpToNextFormula.Coefficient,
            rules.SkillPointsPerLevel,
            rules.ScenarioSoftCap,
            logger);
    }

    /// <summary>
    /// Опыт, необходимый для перехода с уровня <paramref name="level"/> на следующий.
    /// </summary>
    /// <param name="level">Текущий уровень, не меньше 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">Уровень меньше 1.</exception>
    /// <exception cref="OverflowException">Значение не помещается в <see cref="int"/>.</exception>
    public int ExperienceToNextLevel(int level)
    {
        if (level < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "Уровень героя не может быть меньше 1.");
        }

        long value = (long)_experienceCoefficient * level * (level + 1);

        return checked((int)value);
    }

    /// <summary>
    /// Суммарный опыт, необходимый для достижения уровня <paramref name="level"/>.
    /// Для уровня 1 порог равен нулю.
    /// </summary>
    /// <param name="level">Целевой уровень, не меньше 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">Уровень меньше 1.</exception>
    /// <exception cref="OverflowException">Значение не помещается в <see cref="int"/>.</exception>
    public int TotalExperienceForLevel(int level)
    {
        if (level < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "Уровень героя не может быть меньше 1.");
        }

        if (level == 1)
        {
            return 0;
        }

        long steps = level - 1L;
        long sum = steps * (steps + 1) * (steps + 2) / 3;
        long value = (long)_experienceCoefficient * sum;

        return checked((int)value);
    }

    /// <summary>
    /// Определяет уровень, соответствующий накопленному опыту, без учёта мягкого предела.
    /// </summary>
    /// <param name="experience">Накопленный опыт, не меньше нуля.</param>
    /// <exception cref="ArgumentOutOfRangeException">Опыт отрицателен.</exception>
    public int LevelForExperience(int experience)
    {
        if (experience < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(experience), experience, "Опыт не может быть отрицательным.");
        }

        int level = 1;

        while (TotalExperienceForLevel(level + 1) <= experience)
        {
            level++;
        }

        return level;
    }

    /// <summary>
    /// Проверяет, достигнут ли мягкий предел сценария.
    /// </summary>
    /// <param name="level">Текущий уровень.</param>
    public bool IsAtSoftCap(int level)
    {
        return _scenarioSoftCap > 0 && level >= _scenarioSoftCap;
    }

    /// <summary>
    /// Начисляет опыт и повышает уровни, пока хватает опыта и не достигнут мягкий предел.
    /// Каждый уровень даёт +1 к случайной характеристике по весам класса и очки навыка.
    /// </summary>
    /// <param name="hero">Герой, не может быть null.</param>
    /// <param name="amount">Количество опыта, не может быть отрицательным.</param>
    /// <param name="levelUpRandom">Стрим <c>levelup</c>, не может быть null.</param>
    /// <exception cref="ArgumentNullException">Герой или генератор не заданы.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Опыт отрицателен.</exception>
    public HeroProgressionResult AwardExperience(
        Hero hero,
        int amount,
        SeededRandom levelUpRandom)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(levelUpRandom);

        hero.AddExperience(amount);

        List<HeroStatKind> gained = new();
        bool stoppedBySoftCap = false;

        while (true)
        {
            if (IsAtSoftCap(hero.Level))
            {
                stoppedBySoftCap = hero.Experience >= TotalExperienceForLevel(hero.Level + 1);
                break;
            }

            int required = TotalExperienceForLevel(hero.Level + 1);

            if (hero.Experience < required)
            {
                break;
            }

            HeroStatKind stat = LevelUp.RollStat(hero.HeroClass, levelUpRandom);
            hero.ApplyLevelUp(stat, _skillPointsPerLevel);
            gained.Add(stat);
        }

        HeroProgressionResult result = new(
            gained.AsReadOnly(),
            checked(gained.Count * _skillPointsPerLevel),
            stoppedBySoftCap);

        _logger?.Log(LogEvent.Create(
            LogLevel.Info,
            "Hero",
            gained.Count > 0
                ? $"Герой «{hero.Id}» получил {amount} опыта и {gained.Count} уровне(й); всего уровень {hero.Level}, очков навыка {hero.SkillPoints}."
                : $"Герой «{hero.Id}» получил {amount} опыта; уровень остался {hero.Level}.",
            correlationId: hero.Id,
            fields: new Dictionary<string, string>
            {
                ["heroId"] = hero.Id,
                ["experienceGained"] = amount.ToString(),
                ["experienceTotal"] = hero.Experience.ToString(),
                ["level"] = hero.Level.ToString(),
                ["levelsGained"] = gained.Count.ToString(),
                ["skillPoints"] = hero.SkillPoints.ToString(),
                ["softCap"] = _scenarioSoftCap.ToString()
            }));

        return result;
    }
}
