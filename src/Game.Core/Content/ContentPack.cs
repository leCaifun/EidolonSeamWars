using Game.Core.Content.Dto;
using Game.Core.Content.Localization;

namespace Game.Core.Content;

/// <summary>
/// Загруженный контентный пак. Содержит DTO всех категорий вместе с файлами-источниками.
/// Пак не проверяет ссылки между сущностями — это делает <see cref="ContentValidator"/>
/// после merge всех паков.
/// </summary>
public sealed class ContentPack
{
    /// <summary>Корневой каталог пака в терминах <see cref="IContentFileSystem"/>.</summary>
    public required string RootDirectory { get; init; }

    /// <summary>Относительный путь файла манифеста.</summary>
    public required string ManifestFile { get; init; }

    /// <summary>Манифест пака.</summary>
    public required ModManifestDto Manifest { get; init; }

    /// <summary>Фракции.</summary>
    public required IReadOnlyList<ContentEntry<FactionDto>> Factions { get; init; }

    /// <summary>Юниты.</summary>
    public required IReadOnlyList<ContentEntry<UnitDto>> Units { get; init; }

    /// <summary>Классы героев.</summary>
    public required IReadOnlyList<ContentEntry<HeroClassDto>> HeroClasses { get; init; }

    /// <summary>Стартовые герои.</summary>
    public required IReadOnlyList<ContentEntry<HeroDto>> Heroes { get; init; }

    /// <summary>Способности.</summary>
    public required IReadOnlyList<ContentEntry<AbilityDto>> Abilities { get; init; }

    /// <summary>Заклинания.</summary>
    public required IReadOnlyList<ContentEntry<SpellDto>> Spells { get; init; }

    /// <summary>Здания.</summary>
    public required IReadOnlyList<ContentEntry<BuildingDto>> Buildings { get; init; }

    /// <summary>Шаблоны поселений.</summary>
    public required IReadOnlyList<ContentEntry<SettlementTemplateDto>> Settlements { get; init; }

    /// <summary>Объекты карты.</summary>
    public required IReadOnlyList<ContentEntry<MapObjectDto>> MapObjects { get; init; }

    /// <summary>Описания семи ресурсов.</summary>
    public required IReadOnlyList<ContentEntry<ResourceDefinitionDto>> Resources { get; init; }

    /// <summary>Рукотворные тестовые и сценарные карты.</summary>
    public required IReadOnlyList<ContentEntry<TestMapDto>> TestMaps { get; init; }

    /// <summary>Правила биомов; null, если пак их не задаёт.</summary>
    public required ContentEntry<BiomesRulesDto>? BiomesRules { get; init; }

    /// <summary>Правила генерации карты; null, если пак их не задаёт.</summary>
    public required ContentEntry<MapGenerationRulesDto>? MapGenerationRules { get; init; }

    /// <summary>Боевые правила; null, если пак их не задаёт.</summary>
    public required ContentEntry<BattleRulesDto>? BattleRules { get; init; }

    /// <summary>Правила морали; null, если пак их не задаёт.</summary>
    public required ContentEntry<MoraleRulesDto>? MoraleRules { get; init; }

    /// <summary>Правила экономики; null, если пак их не задаёт.</summary>
    public required ContentEntry<EconomyRulesDto>? EconomyRules { get; init; }

    /// <summary>Правила побега; null, если пак их не задаёт.</summary>
    public required ContentEntry<EscapeRulesDto>? EscapeRules { get; init; }

    /// <summary>Правила выкупа; null, если пак их не задаёт.</summary>
    public required ContentEntry<RansomRulesDto>? RansomRules { get; init; }

    /// <summary>Правила ветеранства; null, если пак их не задаёт.</summary>
    public required ContentEntry<VeterancyRulesDto>? VeterancyRules { get; init; }

    /// <summary>Правила прогрессии героя; null, если пак их не задаёт.</summary>
    public required ContentEntry<ProgressionRulesDto>? ProgressionRules { get; init; }

    /// <summary>Манифест языков; null, если пак не содержит локализации.</summary>
    public required ContentEntry<LanguagesManifestDto>? LanguagesManifest { get; init; }

    /// <summary>Языковые таблицы пака.</summary>
    public required IReadOnlyList<LocalizationTable> LocalizationTables { get; init; }

    /// <summary>Общее количество сущностей без правил и локализации.</summary>
    public int EntityCount =>
        Factions.Count + Units.Count + HeroClasses.Count + Heroes.Count +
        Abilities.Count + Spells.Count + Buildings.Count + Settlements.Count +
        MapObjects.Count + Resources.Count + TestMaps.Count;
}
