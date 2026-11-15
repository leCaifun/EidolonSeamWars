using System.Text.Json;
using Game.Core.Content.Dto;
using Game.Core.Content.Internal;
using Game.Core.Diagnostics;

namespace Game.Core.Content;

/// <summary>
/// Загружает один контентный пак: манифест, сущности, правила и локализацию.
/// Не выполняет merge и не проверяет ссылки между сущностями. Все ошибки
/// чтения и схемы возвращаются как диагностика с путём файла и полем.
/// </summary>
public sealed class ContentLoader
{
    private readonly IContentFileSystem _fileSystem;
    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт загрузчик пака.
    /// </summary>
    /// <exception cref="ArgumentNullException">Файловая абстракция не задана.</exception>
    public ContentLoader(IContentFileSystem fileSystem, IGameLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Загружает пак из корневого каталога.
    /// </summary>
    /// <param name="rootDirectory">Корень пака в терминах <see cref="IContentFileSystem"/>.</param>
    public ContentLoadResult Load(string rootDirectory)
    {
        ArgumentNullException.ThrowIfNull(rootDirectory);

        ContentDiagnosticCollection diagnostics = new();
        IReadOnlyList<string> relativeFiles;

        try
        {
            relativeFiles = _fileSystem.ListFiles(rootDirectory)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "CONTENT_ROOT_READ",
                $"Не удалось прочитать каталог контента: {exception.Message}",
                rootDirectory));

            return new ContentLoadResult(null, diagnostics);
        }

        string? manifestRelativePath = relativeFiles.FirstOrDefault(
            path => path.Replace('\\', '/').Equals("manifest.json", StringComparison.Ordinal));

        if (manifestRelativePath is null)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "CONTENT_MANIFEST_MISSING",
                "В корне пака отсутствует обязательный manifest.json.",
                ContentJsonReader.BuildSourcePath(rootDirectory, "manifest.json")));

            return new ContentLoadResult(null, diagnostics);
        }

        ModManifestDto? manifest = ReadManifest(rootDirectory, manifestRelativePath, diagnostics);

        if (manifest is null)
        {
            return new ContentLoadResult(null, diagnostics);
        }

        string sourcePack = string.IsNullOrWhiteSpace(manifest.Id) ? "unknown" : manifest.Id;
        List<ContentEntry<FactionDto>> factions = new();
        List<ContentEntry<UnitDto>> units = new();
        List<ContentEntry<HeroClassDto>> heroClasses = new();
        List<ContentEntry<HeroDto>> heroes = new();
        List<ContentEntry<AbilityDto>> abilities = new();
        List<ContentEntry<SpellDto>> spells = new();
        List<ContentEntry<BuildingDto>> buildings = new();
        List<ContentEntry<SettlementTemplateDto>> settlements = new();
        List<ContentEntry<MapObjectDto>> mapObjects = new();
        List<ContentEntry<ResourceDefinitionDto>> resources = new();
        Dictionary<string, string> idSources = new(StringComparer.Ordinal);
        ContentEntry<BattleRulesDto>? battle = null;
        ContentEntry<MoraleRulesDto>? morale = null;
        ContentEntry<EconomyRulesDto>? economy = null;
        ContentEntry<EscapeRulesDto>? escape = null;
        ContentEntry<RansomRulesDto>? ransom = null;
        ContentEntry<VeterancyRulesDto>? veterancy = null;
        ContentEntry<ProgressionRulesDto>? progression = null;
        HashSet<string> ruleFiles = new(StringComparer.OrdinalIgnoreCase);
        List<string> localizationFiles = new();

        foreach (string relativePath in relativeFiles)
        {
            string normalized = relativePath.Replace('\\', '/');

            if (!normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("manifest.json", StringComparison.Ordinal))
            {
                continue;
            }

            if (normalized.StartsWith("localization/", StringComparison.Ordinal))
            {
                localizationFiles.Add(relativePath);
                continue;
            }

            if (normalized.Equals("resources.json", StringComparison.Ordinal))
            {
                ReadCollection<ResourcesFileDto, ResourceDefinitionDto>(
                    rootDirectory,
                    relativePath,
                    "resources",
                    sourcePack,
                    diagnostics,
                    idSources,
                    resources,
                    file => file.Resources,
                    item => item.Id);
                continue;
            }

            if (normalized.StartsWith("factions/", StringComparison.Ordinal))
            {
                ReadSingle(
                    rootDirectory,
                    relativePath,
                    sourcePack,
                    diagnostics,
                    idSources,
                    factions,
                    item => item.Id);
                continue;
            }

            if (normalized.StartsWith("units/", StringComparison.Ordinal))
            {
                ReadCollection<UnitsFileDto, UnitDto>(
                    rootDirectory,
                    relativePath,
                    "units",
                    sourcePack,
                    diagnostics,
                    idSources,
                    units,
                    file => file.Units,
                    item => item.Id);
                continue;
            }

            if (normalized.StartsWith("hero_classes/", StringComparison.Ordinal))
            {
                ReadSingle(
                    rootDirectory,
                    relativePath,
                    sourcePack,
                    diagnostics,
                    idSources,
                    heroClasses,
                    item => item.Id);
                continue;
            }

            if (normalized.StartsWith("heroes/", StringComparison.Ordinal))
            {
                ReadSingle(
                    rootDirectory,
                    relativePath,
                    sourcePack,
                    diagnostics,
                    idSources,
                    heroes,
                    item => item.Id);
                continue;
            }

            if (normalized.StartsWith("abilities/", StringComparison.Ordinal))
            {
                ReadCollection<AbilitiesFileDto, AbilityDto>(
                    rootDirectory,
                    relativePath,
                    "abilities",
                    sourcePack,
                    diagnostics,
                    idSources,
                    abilities,
                    file => file.Abilities,
                    item => item.Id);
                continue;
            }

            if (normalized.StartsWith("spells/", StringComparison.Ordinal))
            {
                ReadCollection<SpellsFileDto, SpellDto>(
                    rootDirectory,
                    relativePath,
                    "spells",
                    sourcePack,
                    diagnostics,
                    idSources,
                    spells,
                    file => file.Spells,
                    item => item.Id);
                continue;
            }

            if (normalized.StartsWith("buildings/", StringComparison.Ordinal))
            {
                ReadCollection<BuildingsFileDto, BuildingDto>(
                    rootDirectory,
                    relativePath,
                    "buildings",
                    sourcePack,
                    diagnostics,
                    idSources,
                    buildings,
                    file => file.Buildings,
                    item => item.Id);
                continue;
            }

            if (normalized.StartsWith("settlements/", StringComparison.Ordinal))
            {
                ReadSingle(
                    rootDirectory,
                    relativePath,
                    sourcePack,
                    diagnostics,
                    idSources,
                    settlements,
                    item => item.Id);
                continue;
            }

            if (normalized.StartsWith("map_objects/", StringComparison.Ordinal))
            {
                ReadCollection<MapObjectsFileDto, MapObjectDto>(
                    rootDirectory,
                    relativePath,
                    "objects",
                    sourcePack,
                    diagnostics,
                    idSources,
                    mapObjects,
                    file => file.Objects,
                    item => item.Id);
                continue;
            }

            if (normalized.StartsWith("rules/", StringComparison.Ordinal))
            {
                ReadRule(
                    rootDirectory,
                    relativePath,
                    sourcePack,
                    diagnostics,
                    ruleFiles,
                    ref battle,
                    ref morale,
                    ref economy,
                    ref escape,
                    ref ransom,
                    ref veterancy,
                    ref progression);
                continue;
            }

            diagnostics.Add(ContentDiagnostic.Warning(
                "CONTENT_FILE_UNKNOWN",
                $"Файл «{normalized}» не относится к известным каталогам контента и пропущен.",
                ContentJsonReader.BuildSourcePath(rootDirectory, relativePath)));
        }

        LocalizationLoader localizationLoader = new(_fileSystem);
        LocalizationLoadResult localization = localizationLoader.Load(
            rootDirectory,
            localizationFiles,
            sourcePack);
        diagnostics.AddRange(localization.Diagnostics.Items);

        ContentPack pack = new()
        {
            RootDirectory = rootDirectory,
            ManifestFile = ContentJsonReader.BuildSourcePath(rootDirectory, manifestRelativePath),
            Manifest = manifest,
            Factions = factions.AsReadOnly(),
            Units = units.AsReadOnly(),
            HeroClasses = heroClasses.AsReadOnly(),
            Heroes = heroes.AsReadOnly(),
            Abilities = abilities.AsReadOnly(),
            Spells = spells.AsReadOnly(),
            Buildings = buildings.AsReadOnly(),
            Settlements = settlements.AsReadOnly(),
            MapObjects = mapObjects.AsReadOnly(),
            Resources = resources.AsReadOnly(),
            BattleRules = battle,
            MoraleRules = morale,
            EconomyRules = economy,
            EscapeRules = escape,
            RansomRules = ransom,
            VeterancyRules = veterancy,
            ProgressionRules = progression,
            LanguagesManifest = localization.Manifest,
            LocalizationTables = localization.Tables
        };

        _logger?.Log(LogEvent.Create(
            diagnostics.HasErrors ? LogLevel.Warning : LogLevel.Info,
            "Content",
            diagnostics.HasErrors
                ? $"Пак «{sourcePack}» загружен с ошибками: сущностей {pack.EntityCount}, языков {pack.LocalizationTables.Count}, ошибок {diagnostics.ErrorCount}."
                : $"Пак «{sourcePack}» загружен: сущностей {pack.EntityCount}, языков {pack.LocalizationTables.Count}, правил {CountRules(pack)}.",
            fields: new Dictionary<string, string>
            {
                ["packId"] = sourcePack,
                ["entities"] = pack.EntityCount.ToString(),
                ["languages"] = pack.LocalizationTables.Count.ToString(),
                ["errors"] = diagnostics.ErrorCount.ToString(),
                ["warnings"] = diagnostics.WarningCount.ToString()
            }));

        return new ContentLoadResult(pack, diagnostics);
    }

    private ModManifestDto? ReadManifest(
        string rootDirectory,
        string relativePath,
        ContentDiagnosticCollection diagnostics)
    {
        if (!ContentJsonReader.TryReadWithRoot<ModManifestDto>(
                _fileSystem,
                rootDirectory,
                relativePath,
                diagnostics,
                out ModManifestDto? manifest,
                out JsonElement root)
            || manifest is null)
        {
            return null;
        }

        string source = ContentJsonReader.BuildSourcePath(rootDirectory, relativePath);
        RequirePropertyKind(root, "version", JsonValueKind.String, source, diagnostics);
        RequirePropertyKind(root, "gameVersionRange", JsonValueKind.String, source, diagnostics);
        RequirePropertyKind(root, "dependencies", JsonValueKind.Array, source, diagnostics);
        RequirePropertyKind(root, "loadOrder", JsonValueKind.Number, source, diagnostics);

        return manifest;
    }

    private void ReadSingle<T>(
        string rootDirectory,
        string relativePath,
        string sourcePack,
        ContentDiagnosticCollection diagnostics,
        Dictionary<string, string> idSources,
        List<ContentEntry<T>> target,
        Func<T, string> idSelector)
        where T : class
    {
        T? value = ContentJsonReader.TryRead<T>(_fileSystem, rootDirectory, relativePath, diagnostics);

        if (value is null)
        {
            return;
        }

        string source = ContentJsonReader.BuildSourcePath(rootDirectory, relativePath);
        CheckRequiredFields(value, source, diagnostics);
        AddEntry(value, source, sourcePack, diagnostics, idSources, target, idSelector);
    }

    private void ReadCollection<TFile, TEntry>(
        string rootDirectory,
        string relativePath,
        string property,
        string sourcePack,
        ContentDiagnosticCollection diagnostics,
        Dictionary<string, string> idSources,
        List<ContentEntry<TEntry>> target,
        Func<TFile, IReadOnlyList<TEntry>> entriesSelector,
        Func<TEntry, string> idSelector)
        where TFile : class
        where TEntry : class
    {
        if (!ContentJsonReader.TryReadWithRoot<TFile>(
                _fileSystem,
                rootDirectory,
                relativePath,
                diagnostics,
                out TFile? file,
                out JsonElement root)
            || file is null)
        {
            return;
        }

        string source = ContentJsonReader.BuildSourcePath(rootDirectory, relativePath);

        if (!root.TryGetProperty(property, out JsonElement element)
            || element.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "CONTENT_FIELD_REQUIRED",
                $"Обязательный массив «{property}» отсутствует или имеет неверный тип.",
                source,
                field: property));

            return;
        }

        IReadOnlyList<TEntry> entries = entriesSelector(file);

        if (entries.Count == 0)
        {
            diagnostics.Add(ContentDiagnostic.Warning(
                "CONTENT_COLLECTION_EMPTY",
                $"Массив «{property}» пуст.",
                source,
                field: property));
        }

        foreach (TEntry entry in entries)
        {
            CheckRequiredFields(entry, source, diagnostics);
            AddEntry(entry, source, sourcePack, diagnostics, idSources, target, idSelector);
        }
    }

    private void ReadRule(
        string rootDirectory,
        string relativePath,
        string sourcePack,
        ContentDiagnosticCollection diagnostics,
        HashSet<string> ruleFiles,
        ref ContentEntry<BattleRulesDto>? battle,
        ref ContentEntry<MoraleRulesDto>? morale,
        ref ContentEntry<EconomyRulesDto>? economy,
        ref ContentEntry<EscapeRulesDto>? escape,
        ref ContentEntry<RansomRulesDto>? ransom,
        ref ContentEntry<VeterancyRulesDto>? veterancy,
        ref ContentEntry<ProgressionRulesDto>? progression)
    {
        string source = ContentJsonReader.BuildSourcePath(rootDirectory, relativePath);
        string normalized = relativePath.Replace('\\', '/');
        int slashIndex = normalized.LastIndexOf('/');
        string fileName = slashIndex >= 0 ? normalized[(slashIndex + 1)..] : normalized;
        int dotIndex = fileName.LastIndexOf('.');
        string name = dotIndex > 0 ? fileName[..dotIndex] : fileName;

        if (!ruleFiles.Add(name))
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "CONTENT_RULE_DUPLICATE",
                $"Файл правила «{name}» встречается более одного раза в паке.",
                source));

            return;
        }

        switch (name)
        {
            case "battle":
                battle = ReadRuleFile<BattleRulesDto>(rootDirectory, relativePath, sourcePack, diagnostics);
                break;
            case "morale":
                morale = ReadRuleFile<MoraleRulesDto>(rootDirectory, relativePath, sourcePack, diagnostics);
                break;
            case "economy":
                economy = ReadRuleFile<EconomyRulesDto>(rootDirectory, relativePath, sourcePack, diagnostics);
                break;
            case "escape":
                escape = ReadRuleFile<EscapeRulesDto>(rootDirectory, relativePath, sourcePack, diagnostics);
                break;
            case "ransom":
                ransom = ReadRuleFile<RansomRulesDto>(rootDirectory, relativePath, sourcePack, diagnostics);
                break;
            case "veterancy":
                veterancy = ReadRuleFile<VeterancyRulesDto>(rootDirectory, relativePath, sourcePack, diagnostics);
                break;
            case "progression":
                progression = ReadRuleFile<ProgressionRulesDto>(rootDirectory, relativePath, sourcePack, diagnostics);
                break;
            default:
                diagnostics.Add(ContentDiagnostic.Warning(
                    "CONTENT_RULE_UNKNOWN",
                    $"Файл правила «{name}» не поддерживается этапом 03 и пропущен.",
                    source));
                break;
        }
    }

    private ContentEntry<T>? ReadRuleFile<T>(
        string rootDirectory,
        string relativePath,
        string sourcePack,
        ContentDiagnosticCollection diagnostics)
        where T : class
    {
        T? value = ContentJsonReader.TryRead<T>(_fileSystem, rootDirectory, relativePath, diagnostics);

        if (value is null)
        {
            return null;
        }

        string source = ContentJsonReader.BuildSourcePath(rootDirectory, relativePath);
        return new ContentEntry<T>(value, source, sourcePack);
    }

    private static void AddEntry<T>(
        T value,
        string source,
        string sourcePack,
        ContentDiagnosticCollection diagnostics,
        Dictionary<string, string> idSources,
        List<ContentEntry<T>> target,
        Func<T, string> idSelector)
        where T : class
    {
        string id = idSelector(value);

        if (string.IsNullOrWhiteSpace(id))
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "CONTENT_ID_REQUIRED",
                "У сущности отсутствует обязательный ID.",
                source,
                field: "id"));

            return;
        }

        if (idSources.TryGetValue(id, out string? existingSource))
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "CONTENT_ID_DUPLICATE",
                $"ID «{id}» уже объявлен в файле {existingSource}.",
                source,
                id,
                "id"));

            return;
        }

        idSources[id] = source;
        target.Add(new ContentEntry<T>(value, source, sourcePack));
    }

    private static void CheckRequiredFields<T>(
        T value,
        string source,
        ContentDiagnosticCollection diagnostics)
        where T : class
    {
        switch (value)
        {
            case FactionDto faction:
                Require(faction.Id, "id", source, faction.Id, diagnostics);
                Require(faction.NameKey, "nameKey", source, faction.Id, diagnostics);
                Require(faction.DescriptionKey, "descriptionKey", source, faction.Id, diagnostics);
                Require(faction.HeroClassId, "heroClassId", source, faction.Id, diagnostics);
                break;
            case UnitDto unit:
                Require(unit.Id, "id", source, unit.Id, diagnostics);
                Require(unit.FactionId, "factionId", source, unit.Id, diagnostics);
                Require(unit.UnitClass, "unitClass", source, unit.Id, diagnostics);
                Require(unit.MovementType, "movementType", source, unit.Id, diagnostics);
                Require(unit.DamageType, "damageType", source, unit.Id, diagnostics);
                Require(unit.NameKey, "nameKey", source, unit.Id, diagnostics);
                Require(unit.DescriptionKey, "descriptionKey", source, unit.Id, diagnostics);
                break;
            case HeroClassDto heroClass:
                Require(heroClass.Id, "id", source, heroClass.Id, diagnostics);
                Require(heroClass.FactionId, "factionId", source, heroClass.Id, diagnostics);
                Require(heroClass.NameKey, "nameKey", source, heroClass.Id, diagnostics);
                Require(heroClass.DescriptionKey, "descriptionKey", source, heroClass.Id, diagnostics);
                Require(heroClass.PassiveAbilityId, "passiveAbilityId", source, heroClass.Id, diagnostics);
                Require(heroClass.CommandAbilityId, "commandAbilityId", source, heroClass.Id, diagnostics);
                Require(heroClass.SpellId, "spellId", source, heroClass.Id, diagnostics);
                break;
            case HeroDto hero:
                Require(hero.Id, "id", source, hero.Id, diagnostics);
                Require(hero.FactionId, "factionId", source, hero.Id, diagnostics);
                Require(hero.ClassId, "classId", source, hero.Id, diagnostics);
                Require(hero.NameKey, "nameKey", source, hero.Id, diagnostics);
                Require(hero.DescriptionKey, "descriptionKey", source, hero.Id, diagnostics);
                break;
            case AbilityDto ability:
                Require(ability.Id, "id", source, ability.Id, diagnostics);
                Require(ability.NameKey, "nameKey", source, ability.Id, diagnostics);
                Require(ability.DescriptionKey, "descriptionKey", source, ability.Id, diagnostics);
                Require(ability.Kind, "kind", source, ability.Id, diagnostics);
                Require(ability.Trigger, "trigger", source, ability.Id, diagnostics);
                break;
            case SpellDto spell:
                Require(spell.Id, "id", source, spell.Id, diagnostics);
                Require(spell.NameKey, "nameKey", source, spell.Id, diagnostics);
                Require(spell.DescriptionKey, "descriptionKey", source, spell.Id, diagnostics);
                Require(spell.School, "school", source, spell.Id, diagnostics);
                Require(spell.Target, "target", source, spell.Id, diagnostics);
                break;
            case BuildingDto building:
                Require(building.Id, "id", source, building.Id, diagnostics);
                Require(building.FactionId, "factionId", source, building.Id, diagnostics);
                Require(building.NameKey, "nameKey", source, building.Id, diagnostics);
                Require(building.DescriptionKey, "descriptionKey", source, building.Id, diagnostics);
                break;
            case SettlementTemplateDto settlement:
                Require(settlement.Id, "id", source, settlement.Id, diagnostics);
                Require(settlement.FactionId, "factionId", source, settlement.Id, diagnostics);
                Require(settlement.NameKey, "nameKey", source, settlement.Id, diagnostics);
                Require(settlement.DescriptionKey, "descriptionKey", source, settlement.Id, diagnostics);
                break;
            case MapObjectDto mapObject:
                Require(mapObject.Id, "id", source, mapObject.Id, diagnostics);
                Require(mapObject.Type, "type", source, mapObject.Id, diagnostics);
                Require(mapObject.NameKey, "nameKey", source, mapObject.Id, diagnostics);
                Require(mapObject.DescriptionKey, "descriptionKey", source, mapObject.Id, diagnostics);
                break;
            case ResourceDefinitionDto resource:
                Require(resource.Id, "id", source, resource.Id, diagnostics);
                Require(resource.NameKey, "nameKey", source, resource.Id, diagnostics);
                Require(resource.Role, "role", source, resource.Id, diagnostics);
                break;
        }
    }

    private static void Require(
        string value,
        string field,
        string source,
        string? entityId,
        ContentDiagnosticCollection diagnostics)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        diagnostics.Add(ContentDiagnostic.Error(
            "CONTENT_FIELD_REQUIRED",
            $"Обязательное поле «{field}» не заполнено.",
            source,
            entityId,
            field));
    }

    private static void RequirePropertyKind(
        JsonElement root,
        string property,
        JsonValueKind kind,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        if (root.TryGetProperty(property, out JsonElement element) && element.ValueKind == kind)
        {
            return;
        }

        diagnostics.Add(ContentDiagnostic.Error(
            "CONTENT_FIELD_REQUIRED",
            $"Обязательное поле «{property}» отсутствует или имеет неверный тип.",
            source,
            field: property));
    }

    private static int CountRules(ContentPack pack)
    {
        int count = 0;

        if (pack.BattleRules is not null)
        {
            count++;
        }

        if (pack.MoraleRules is not null)
        {
            count++;
        }

        if (pack.EconomyRules is not null)
        {
            count++;
        }

        if (pack.EscapeRules is not null)
        {
            count++;
        }

        if (pack.RansomRules is not null)
        {
            count++;
        }

        if (pack.VeterancyRules is not null)
        {
            count++;
        }

        if (pack.ProgressionRules is not null)
        {
            count++;
        }

        return count;
    }
}
