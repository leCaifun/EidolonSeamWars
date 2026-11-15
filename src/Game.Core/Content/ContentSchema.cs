using System.Text.RegularExpressions;

namespace Game.Core.Content;

/// <summary>
/// Общие правила схемы контента этапа 03: поддерживаемая версия, допустимые
/// строковые значения и стабильные ID. Код не содержит имён конкретных
/// фракций, юнитов или зданий — только форматы и перечисления.
/// </summary>
public static class ContentSchema
{
    /// <summary>Единственная поддерживаемая версия схемы контента.</summary>
    public const int SupportedSchemaVersion = 1;

    /// <summary>Минимальный уровень поселения по GDD §2.2.</summary>
    public const int MinSettlementLevel = 2;

    /// <summary>Максимальный уровень поселения по GDD §2.2.</summary>
    public const int MaxSettlementLevel = 4;

    /// <summary>Максимальное число слотов армии героя по GDD §6.1.</summary>
    public const int MaxArmySlots = 5;

    private static readonly Regex IdPattern = new(
        "^[a-z][a-z0-9_]*(\\.[a-z0-9_]+)+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> MovementTypesInternal = new(StringComparer.Ordinal)
    {
        "foot",
        "shooter",
        "cavalry",
        "beast",
        "flyer",
        "siege"
    };

    private static readonly HashSet<string> DamageTypesInternal = new(StringComparer.Ordinal)
    {
        "physical",
        "magical",
        "poison",
        "fire",
        "firearm"
    };

    private static readonly HashSet<string> MapObjectTypesInternal = new(StringComparer.Ordinal)
    {
        "mine",
        "dwelling",
        "echoCache",
        "patrol"
    };

    private static readonly HashSet<string> AbilityKindsInternal = new(StringComparer.Ordinal)
    {
        "passive",
        "active",
        "command"
    };

    private static readonly HashSet<string> AbilityTriggersInternal = new(StringComparer.Ordinal)
    {
        "onAction",
        "onApplyStatus",
        "onBattleEnd",
        "onCalculateAttack",
        "onCalculateCritChance",
        "onCalculateDamage",
        "onCalculateDamageTaken",
        "onCalculateDefense",
        "onCalculateMorale",
        "onCalculateMovement",
        "onCommand",
        "onDamageDealt"
    };

    private static readonly HashSet<string> AbilityTargetsInternal = new(StringComparer.Ordinal)
    {
        "allAllies",
        "allyStack",
        "aura",
        "corpses",
        "enemyArea",
        "enemyStack"
    };

    private static readonly HashSet<string> LocalizationStatusesInternal = new(StringComparer.Ordinal)
    {
        "complete",
        "draft",
        "experimental"
    };

    private static readonly HashSet<string> FontReviewLanguagesInternal = new(StringComparer.Ordinal)
    {
        "la",
        "cu",
        "rue",
        "szl",
        "csb",
        "hsb",
        "dsb",
        "cnr"
    };

    private static readonly HashSet<string> ResourceIdsInternal = new(StringComparer.Ordinal)
    {
        "gold",
        "wood",
        "ore",
        "crystal",
        "sulfur",
        "mercury",
        "essence"
    };

    /// <summary>Допустимые токены типа передвижения.</summary>
    public static IReadOnlySet<string> MovementTypes => MovementTypesInternal;

    /// <summary>Допустимые токены типа урона.</summary>
    public static IReadOnlySet<string> DamageTypes => DamageTypesInternal;

    /// <summary>Допустимые типы объектов карты этапа 03.</summary>
    public static IReadOnlySet<string> MapObjectTypes => MapObjectTypesInternal;

    /// <summary>Допустимые виды способностей.</summary>
    public static IReadOnlySet<string> AbilityKinds => AbilityKindsInternal;

    /// <summary>Допустимые триггеры способностей.</summary>
    public static IReadOnlySet<string> AbilityTriggers => AbilityTriggersInternal;

    /// <summary>Допустимые цели способностей и заклинаний.</summary>
    public static IReadOnlySet<string> AbilityTargets => AbilityTargetsInternal;

    /// <summary>Допустимые статусы языковых файлов.</summary>
    public static IReadOnlySet<string> LocalizationStatuses => LocalizationStatusesInternal;

    /// <summary>Языки, для которых обязательна проверка шрифтов (ADR-026).</summary>
    public static IReadOnlySet<string> FontReviewLanguages => FontReviewLanguagesInternal;

    /// <summary>Семь ресурсов MVP (ADR-005).</summary>
    public static IReadOnlySet<string> ResourceIds => ResourceIdsInternal;

    /// <summary>
    /// Проверяет формат стабильного ID: нижний регистр, сегменты через точку,
    /// минимум два сегмента, цифры и подчёркивания после первого символа.
    /// </summary>
    public static bool IsValidId(string? id)
    {
        return !string.IsNullOrWhiteSpace(id) && IdPattern.IsMatch(id);
    }

    /// <summary>Проверяет, является ли строка ID одного из семи ресурсов.</summary>
    public static bool IsResourceId(string? id)
    {
        return id is not null && ResourceIdsInternal.Contains(id);
    }
}
