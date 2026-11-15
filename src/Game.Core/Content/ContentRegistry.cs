using System.Collections.ObjectModel;
using Game.Core.Content.Dto;
using Game.Core.Content.Localization;

namespace Game.Core.Content;

/// <summary>
/// Итоговый реестр контента после merge всех паков. Доступ к сущностям —
/// по стабильному строковому ID; правила и локализация — отдельными свойствами.
/// Реестр иммутабелен после создания.
/// </summary>
public sealed class ContentRegistry
{
    private readonly ReadOnlyDictionary<string, ContentEntry<FactionDto>> _factions;
    private readonly ReadOnlyDictionary<string, ContentEntry<UnitDto>> _units;
    private readonly ReadOnlyDictionary<string, ContentEntry<HeroClassDto>> _heroClasses;
    private readonly ReadOnlyDictionary<string, ContentEntry<HeroDto>> _heroes;
    private readonly ReadOnlyDictionary<string, ContentEntry<AbilityDto>> _abilities;
    private readonly ReadOnlyDictionary<string, ContentEntry<SpellDto>> _spells;
    private readonly ReadOnlyDictionary<string, ContentEntry<BuildingDto>> _buildings;
    private readonly ReadOnlyDictionary<string, ContentEntry<SettlementTemplateDto>> _settlements;
    private readonly ReadOnlyDictionary<string, ContentEntry<MapObjectDto>> _mapObjects;
    private readonly ReadOnlyDictionary<string, ContentEntry<ResourceDefinitionDto>> _resources;
    private readonly ReadOnlyCollection<ContentChange> _changes;
    private readonly ReadOnlyCollection<ModManifestDto> _manifests;

    /// <summary>
    /// Создаёт реестр из объединённых списков. Дубликаты ID не допускаются
    /// merge-слоем; при повторе сохраняется последняя запись.
    /// </summary>
    public ContentRegistry(
        IEnumerable<ContentEntry<FactionDto>> factions,
        IEnumerable<ContentEntry<UnitDto>> units,
        IEnumerable<ContentEntry<HeroClassDto>> heroClasses,
        IEnumerable<ContentEntry<HeroDto>> heroes,
        IEnumerable<ContentEntry<AbilityDto>> abilities,
        IEnumerable<ContentEntry<SpellDto>> spells,
        IEnumerable<ContentEntry<BuildingDto>> buildings,
        IEnumerable<ContentEntry<SettlementTemplateDto>> settlements,
        IEnumerable<ContentEntry<MapObjectDto>> mapObjects,
        IEnumerable<ContentEntry<ResourceDefinitionDto>> resources,
        ContentEntry<BattleRulesDto>? battleRules,
        ContentEntry<MoraleRulesDto>? moraleRules,
        ContentEntry<EconomyRulesDto>? economyRules,
        ContentEntry<EscapeRulesDto>? escapeRules,
        ContentEntry<RansomRulesDto>? ransomRules,
        ContentEntry<VeterancyRulesDto>? veterancyRules,
        ContentEntry<ProgressionRulesDto>? progressionRules,
        LocalizationSet? localization,
        IEnumerable<ContentChange> changes,
        IEnumerable<ModManifestDto> manifests)
    {
        ArgumentNullException.ThrowIfNull(factions);
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(heroClasses);
        ArgumentNullException.ThrowIfNull(heroes);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(spells);
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(settlements);
        ArgumentNullException.ThrowIfNull(mapObjects);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(manifests);

        _factions = ToDictionary(factions, entry => entry.Value.Id);
        _units = ToDictionary(units, entry => entry.Value.Id);
        _heroClasses = ToDictionary(heroClasses, entry => entry.Value.Id);
        _heroes = ToDictionary(heroes, entry => entry.Value.Id);
        _abilities = ToDictionary(abilities, entry => entry.Value.Id);
        _spells = ToDictionary(spells, entry => entry.Value.Id);
        _buildings = ToDictionary(buildings, entry => entry.Value.Id);
        _settlements = ToDictionary(settlements, entry => entry.Value.Id);
        _mapObjects = ToDictionary(mapObjects, entry => entry.Value.Id);
        _resources = ToDictionary(resources, entry => entry.Value.Id);
        _changes = Array.AsReadOnly(changes.ToArray());
        _manifests = Array.AsReadOnly(manifests.ToArray());
        BattleRules = battleRules;
        MoraleRules = moraleRules;
        EconomyRules = economyRules;
        EscapeRules = escapeRules;
        RansomRules = ransomRules;
        VeterancyRules = veterancyRules;
        ProgressionRules = progressionRules;
        Localization = localization;
    }

    /// <summary>Фракции по ID.</summary>
    public IReadOnlyDictionary<string, ContentEntry<FactionDto>> Factions => _factions;

    /// <summary>Юниты по ID.</summary>
    public IReadOnlyDictionary<string, ContentEntry<UnitDto>> Units => _units;

    /// <summary>Классы героев по ID.</summary>
    public IReadOnlyDictionary<string, ContentEntry<HeroClassDto>> HeroClasses => _heroClasses;

    /// <summary>Герои по ID.</summary>
    public IReadOnlyDictionary<string, ContentEntry<HeroDto>> Heroes => _heroes;

    /// <summary>Способности по ID.</summary>
    public IReadOnlyDictionary<string, ContentEntry<AbilityDto>> Abilities => _abilities;

    /// <summary>Заклинания по ID.</summary>
    public IReadOnlyDictionary<string, ContentEntry<SpellDto>> Spells => _spells;

    /// <summary>Здания по ID.</summary>
    public IReadOnlyDictionary<string, ContentEntry<BuildingDto>> Buildings => _buildings;

    /// <summary>Шаблоны поселений по ID.</summary>
    public IReadOnlyDictionary<string, ContentEntry<SettlementTemplateDto>> Settlements => _settlements;

    /// <summary>Объекты карты по ID.</summary>
    public IReadOnlyDictionary<string, ContentEntry<MapObjectDto>> MapObjects => _mapObjects;

    /// <summary>Ресурсы по ID.</summary>
    public IReadOnlyDictionary<string, ContentEntry<ResourceDefinitionDto>> Resources => _resources;

    /// <summary>Боевые правила или null.</summary>
    public ContentEntry<BattleRulesDto>? BattleRules { get; }

    /// <summary>Правила морали или null.</summary>
    public ContentEntry<MoraleRulesDto>? MoraleRules { get; }

    /// <summary>Правила экономики или null.</summary>
    public ContentEntry<EconomyRulesDto>? EconomyRules { get; }

    /// <summary>Правила побега или null.</summary>
    public ContentEntry<EscapeRulesDto>? EscapeRules { get; }

    /// <summary>Правила выкупа или null.</summary>
    public ContentEntry<RansomRulesDto>? RansomRules { get; }

    /// <summary>Правила ветеранства или null.</summary>
    public ContentEntry<VeterancyRulesDto>? VeterancyRules { get; }

    /// <summary>Правила прогрессии героя или null.</summary>
    public ContentEntry<ProgressionRulesDto>? ProgressionRules { get; }

    /// <summary>Объединённая локализация или null.</summary>
    public LocalizationSet? Localization { get; }

    /// <summary>Журнал добавлений и переопределений merge.</summary>
    public IReadOnlyList<ContentChange> Changes => _changes;

    /// <summary>Манифесты загруженных паков в порядке merge.</summary>
    public IReadOnlyList<ModManifestDto> Manifests => _manifests;

    /// <summary>Количество загруженных паков.</summary>
    public int PackCount => _manifests.Count;

    /// <summary>Общее количество сущностей в реестре.</summary>
    public int EntityCount =>
        _factions.Count + _units.Count + _heroClasses.Count + _heroes.Count +
        _abilities.Count + _spells.Count + _buildings.Count + _settlements.Count +
        _mapObjects.Count + _resources.Count;

    /// <summary>
    /// Возвращает юнит по ID или null.
    /// </summary>
    public UnitDto? FindUnit(string id)
    {
        return id is not null && _units.TryGetValue(id, out ContentEntry<UnitDto>? entry)
            ? entry.Value
            : null;
    }

    /// <summary>
    /// Возвращает фракцию по ID или null.
    /// </summary>
    public FactionDto? FindFaction(string id)
    {
        return id is not null && _factions.TryGetValue(id, out ContentEntry<FactionDto>? entry)
            ? entry.Value
            : null;
    }

    /// <summary>
    /// Возвращает класс героя по ID или null.
    /// </summary>
    public HeroClassDto? FindHeroClass(string id)
    {
        return id is not null && _heroClasses.TryGetValue(id, out ContentEntry<HeroClassDto>? entry)
            ? entry.Value
            : null;
    }

    /// <summary>
    /// Возвращает героя по ID или null.
    /// </summary>
    public HeroDto? FindHero(string id)
    {
        return id is not null && _heroes.TryGetValue(id, out ContentEntry<HeroDto>? entry)
            ? entry.Value
            : null;
    }

    /// <summary>
    /// Возвращает способность по ID или null.
    /// </summary>
    public AbilityDto? FindAbility(string id)
    {
        return id is not null && _abilities.TryGetValue(id, out ContentEntry<AbilityDto>? entry)
            ? entry.Value
            : null;
    }

    /// <summary>
    /// Возвращает заклинание по ID или null.
    /// </summary>
    public SpellDto? FindSpell(string id)
    {
        return id is not null && _spells.TryGetValue(id, out ContentEntry<SpellDto>? entry)
            ? entry.Value
            : null;
    }

    /// <summary>
    /// Возвращает здание по ID или null.
    /// </summary>
    public BuildingDto? FindBuilding(string id)
    {
        return id is not null && _buildings.TryGetValue(id, out ContentEntry<BuildingDto>? entry)
            ? entry.Value
            : null;
    }

    private static ReadOnlyDictionary<string, ContentEntry<T>> ToDictionary<T>(
        IEnumerable<ContentEntry<T>> entries,
        Func<ContentEntry<T>, string> idSelector)
        where T : class
    {
        Dictionary<string, ContentEntry<T>> result = new(StringComparer.Ordinal);

        foreach (ContentEntry<T> entry in entries)
        {
            result[idSelector(entry)] = entry;
        }

        return new ReadOnlyDictionary<string, ContentEntry<T>>(result);
    }
}
