using Game.Core.Diagnostics;
using Game.Core.Rng;

namespace Game.Core.Domain;

/// <summary>
/// Герой — главнокомандующий вне поля боя (ADR-007, GDD §9).
/// Хранит уровень, опыт, характеристики и артефакты; не имеет HP на поле.
/// Повышение уровня детерминировано и использует только стрим <c>levelup</c>.
/// </summary>
public sealed class Hero
{
    /// <summary>Базовая мана героя (GDD §9.2).</summary>
    public const int BaseMana = 10;

    /// <summary>Мана за каждую единицу знания (GDD §9.2).</summary>
    public const int ManaPerKnowledge = 5;

    /// <summary>Базовые очки движения (GDD §9.2).</summary>
    public const int BaseMovementPoints = 6;

    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт героя указанного класса.
    /// </summary>
    /// <param name="id">Стабильный строковый ID героя.</param>
    /// <param name="nameKey">Ключ локализации имени.</param>
    /// <param name="factionId">ID фракции героя.</param>
    /// <param name="heroClass">Класс героя из контента.</param>
    /// <param name="level">Текущий уровень, начиная с 1.</param>
    /// <param name="experience">Накопленный опыт.</param>
    /// <param name="stats">Характеристики; при null берутся стартовые характеристики класса.</param>
    /// <param name="artifactCount">Количество артефактов.</param>
    /// <param name="logger">Необязательный журнал доменных операций.</param>
    /// <exception cref="ArgumentException">Пустой ID, ключ или ID фракции.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Уровень, опыт или артефакты вне допустимого диапазона.</exception>
    public Hero(
        string id,
        string nameKey,
        string factionId,
        HeroClassDefinition heroClass,
        int level = 1,
        int experience = 0,
        HeroStats? stats = null,
        int artifactCount = 0,
        IGameLogger? logger = null)
    {
        Id = RequireText(id, nameof(id));
        NameKey = RequireText(nameKey, nameof(nameKey));
        FactionId = RequireText(factionId, nameof(factionId));
        HeroClass = heroClass ?? throw new ArgumentNullException(nameof(heroClass));

        if (level < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "Уровень героя не может быть меньше 1.");
        }

        if (experience < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(experience), experience, "Опыт не может быть отрицательным.");
        }

        if (artifactCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(artifactCount), artifactCount, "Число артефактов не может быть отрицательным.");
        }

        Level = level;
        Experience = experience;
        Stats = stats ?? heroClass.StartingStats;
        ArtifactCount = artifactCount;
        _logger = logger;
    }

    /// <summary>Стабильный строковый ID героя.</summary>
    public string Id { get; }

    /// <summary>Ключ локализации имени героя.</summary>
    public string NameKey { get; }

    /// <summary>ID фракции героя.</summary>
    public string FactionId { get; }

    /// <summary>Класс героя из контента.</summary>
    public HeroClassDefinition HeroClass { get; }

    /// <summary>Текущий уровень.</summary>
    public int Level { get; private set; }

    /// <summary>Накопленный опыт; порог следующего уровня задаётся <see cref="HeroProgression"/>.</summary>
    public int Experience { get; private set; }

    /// <summary>Нерастраченные очки навыка; в MVP копятся и не тратятся (ADR-025).</summary>
    public int SkillPoints { get; private set; }

    /// <summary>Текущие характеристики.</summary>
    public HeroStats Stats { get; private set; }

    /// <summary>Количество артефактов у героя.</summary>
    public int ArtifactCount { get; private set; }

    /// <summary>Ёмкость армии героя равна его лидерству.</summary>
    public int Leadership => Stats.Leadership;

    /// <summary>Максимальная мана: <c>10 + Знание × 5</c>.</summary>
    public int Mana => BaseMana + (Stats.Knowledge * ManaPerKnowledge);

    /// <summary>Очки движения: <c>6 + floor(Логистика / 2)</c>.</summary>
    public int MovementPoints => BaseMovementPoints + (Stats.Logistics / 2);

    /// <summary>Достигнут ли мягкий предел уровня сценария; предел задаёт <see cref="HeroProgression"/>.</summary>
    public bool IsAtSoftCap(HeroProgression progression)
    {
        ArgumentNullException.ThrowIfNull(progression);
        return progression.IsAtSoftCap(Level);
    }

    /// <summary>
    /// Добавляет опыт. Отрицательное значение запрещено.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Опыт отрицателен.</exception>
    /// <exception cref="OverflowException">Сумма не помещается в int.</exception>
    public void AddExperience(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Опыт не может быть отрицательным.");
        }

        Experience = checked(Experience + amount);
    }

    /// <summary>
    /// Применяет один уровень: +1 к указанной характеристике и начисление очков навыка.
    /// Пороговую проверку выполняет <see cref="HeroProgression"/>; прямой вызов
    /// из игрового кода запрещён.
    /// </summary>
    /// <param name="gainedStat">Характеристика, которая растёт.</param>
    /// <param name="skillPoints">Сколько очков навыка начислить.</param>
    /// <exception cref="ArgumentOutOfRangeException">Очки навыка отрицательны.</exception>
    internal void ApplyLevelUp(HeroStatKind gainedStat, int skillPoints)
    {
        if (skillPoints < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skillPoints), skillPoints, "Очки навыка не могут быть отрицательными.");
        }

        Level = checked(Level + 1);
        Stats = Stats.With(gainedStat, 1);
        SkillPoints = checked(SkillPoints + skillPoints);

        _logger?.Log(LogEvent.Create(
            LogLevel.Info,
            "Hero",
            $"Герой «{Id}» достиг уровня {Level}: +1 к характеристике {gainedStat}, очков навыка {SkillPoints}.",
            correlationId: Id,
            fields: new Dictionary<string, string>
            {
                ["heroId"] = Id,
                ["level"] = Level.ToString(),
                ["stat"] = gainedStat.ToString(),
                ["skillPoints"] = SkillPoints.ToString()
            }));
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Строковое поле героя не может быть пустым.", parameterName);
        }

        return value;
    }
}
