using System.Collections.ObjectModel;

namespace Game.Core.Domain;

/// <summary>
/// Контентное описание класса героя (GDD §9.3). Конкретные классы приходят
/// из JSON-контента на этапе 03; код не содержит названий фракций и классов.
/// Веса левелапа детерминированно задают распределение прироста характеристик.
/// </summary>
public sealed class HeroClassDefinition
{
    private readonly ReadOnlyDictionary<HeroStatKind, int> _levelUpWeights;

    /// <summary>
    /// Создаёт описание класса героя и проверяет инварианты.
    /// </summary>
    /// <param name="id">Стабильный строковый ID.</param>
    /// <param name="schemaVersion">Версия схемы записи.</param>
    /// <param name="nameKey">Ключ локализации названия класса.</param>
    /// <param name="factionId">ID фракции-владельца.</param>
    /// <param name="startingStats">Стартовые характеристики.</param>
    /// <param name="levelUpWeights">Веса прироста по характеристикам; хотя бы один положительный.</param>
    /// <param name="commandAbilityId">ID командной способности 1/бой.</param>
    /// <param name="spellId">ID стартового заклинания.</param>
    /// <exception cref="ArgumentException">Пустая строка контента или неизвестная характеристика.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Вес отрицателен или сумма весов равна нулю.</exception>
    public HeroClassDefinition(
        string id,
        int schemaVersion,
        string nameKey,
        string factionId,
        HeroStats startingStats,
        IReadOnlyDictionary<HeroStatKind, int> levelUpWeights,
        string commandAbilityId,
        string spellId)
    {
        Id = RequireText(id, nameof(id));
        SchemaVersion = RequirePositive(schemaVersion, nameof(schemaVersion));
        NameKey = RequireText(nameKey, nameof(nameKey));
        FactionId = RequireText(factionId, nameof(factionId));
        StartingStats = startingStats;
        CommandAbilityId = RequireText(commandAbilityId, nameof(commandAbilityId));
        SpellId = RequireText(spellId, nameof(spellId));
        _levelUpWeights = CopyWeights(levelUpWeights);
    }

    /// <summary>Стабильный строковый ID класса.</summary>
    public string Id { get; }

    /// <summary>Версия схемы записи.</summary>
    public int SchemaVersion { get; }

    /// <summary>Ключ локализации названия класса.</summary>
    public string NameKey { get; }

    /// <summary>ID фракции-владельца.</summary>
    public string FactionId { get; }

    /// <summary>Стартовые характеристики.</summary>
    public HeroStats StartingStats { get; }

    /// <summary>Веса прироста характеристик при повышении уровня.</summary>
    public IReadOnlyDictionary<HeroStatKind, int> LevelUpWeights => _levelUpWeights;

    /// <summary>ID командной способности класса, 1/бой.</summary>
    public string CommandAbilityId { get; }

    /// <summary>ID стартового заклинания класса.</summary>
    public string SpellId { get; }

    /// <summary>
    /// Сумма весов левелапа. Используется детерминированным броском в <see cref="LevelUp"/>.
    /// </summary>
    public int TotalLevelUpWeight
    {
        get
        {
            int total = 0;
            foreach (HeroStatKind kind in Enum.GetValues<HeroStatKind>())
            {
                total = checked(total + _levelUpWeights[kind]);
            }

            return total;
        }
    }

    /// <summary>
    /// Возвращает вес указанной характеристики; отсутствующая характеристика имеет вес 0.
    /// </summary>
    public int GetWeight(HeroStatKind kind)
    {
        return _levelUpWeights.TryGetValue(kind, out int weight) ? weight : 0;
    }

    private static ReadOnlyDictionary<HeroStatKind, int> CopyWeights(IReadOnlyDictionary<HeroStatKind, int> weights)
    {
        ArgumentNullException.ThrowIfNull(weights);

        Dictionary<HeroStatKind, int> copy = new();
        int total = 0;

        foreach (HeroStatKind kind in Enum.GetValues<HeroStatKind>())
        {
            if (!weights.TryGetValue(kind, out int weight))
            {
                weight = 0;
            }

            if (weight < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(weights),
                    weight,
                    $"Вес характеристики {kind} не может быть отрицательным.");
            }

            copy[kind] = weight;
            total = checked(total + weight);
        }

        if (total == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weights),
                total,
                "Хотя бы один вес левелапа должен быть положительным.");
        }

        return new ReadOnlyDictionary<HeroStatKind, int>(copy);
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Строковое поле класса героя не может быть пустым.", parameterName);
        }

        return value;
    }

    private static int RequirePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Значение должно быть положительным.");
        }

        return value;
    }
}
