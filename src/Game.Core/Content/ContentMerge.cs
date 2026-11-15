using Game.Core.Content.Dto;
using Game.Core.Content.Localization;
using Game.Core.Diagnostics;

namespace Game.Core.Content;

/// <summary>
/// Объединяет паки в детерминированном порядке: топологическая сортировка
/// зависимостей, затем <c>loadOrder</c>, затем ID по алфавиту. Одинаковые ID
/// переопределяются последним паком (last-wins); каждое переопределение
/// попадает в журнал и лог.
/// </summary>
public sealed class ContentMerge
{
    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт merge-сервис.
    /// </summary>
    public ContentMerge(IGameLogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Планирует порядок паков без объединения данных.
    /// </summary>
    /// <param name="packs">Загруженные паки.</param>
    /// <param name="diagnostics">Коллекция для ошибок зависимостей и циклов.</param>
    /// <returns>Паки в порядке загрузки; при ошибках порядок может быть неполным.</returns>
    public static IReadOnlyList<ContentPack> PlanOrder(
        IReadOnlyList<ContentPack> packs,
        ContentDiagnosticCollection diagnostics)
    {
        ArgumentNullException.ThrowIfNull(packs);
        ArgumentNullException.ThrowIfNull(diagnostics);

        Dictionary<string, ContentPack> byId = new(StringComparer.Ordinal);

        foreach (ContentPack pack in packs)
        {
            string id = pack.Manifest.Id;

            if (string.IsNullOrWhiteSpace(id))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    "CONTENT_PACK_ID_REQUIRED",
                    "Манифест пака не содержит id.",
                    pack.ManifestFile));

                continue;
            }

            if (!byId.TryAdd(id, pack))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    "CONTENT_PACK_DUPLICATE",
                    $"Пак «{id}» загружен более одного раза.",
                    pack.ManifestFile,
                    id,
                    "id"));
            }
        }

        Dictionary<string, int> indegree = new(StringComparer.Ordinal);
        Dictionary<string, List<string>> outgoing = new(StringComparer.Ordinal);

        foreach (string id in byId.Keys)
        {
            indegree[id] = 0;
            outgoing[id] = new List<string>();
        }

        foreach (ContentPack pack in byId.Values)
        {
            foreach (string dependency in pack.Manifest.Dependencies)
            {
                if (string.IsNullOrWhiteSpace(dependency))
                {
                    diagnostics.Add(ContentDiagnostic.Error(
                        "CONTENT_DEPENDENCY_EMPTY",
                        $"Пак «{pack.Manifest.Id}» содержит пустую зависимость.",
                        pack.ManifestFile,
                        pack.Manifest.Id,
                        "dependencies"));

                    continue;
                }

                if (!byId.ContainsKey(dependency))
                {
                    diagnostics.Add(ContentDiagnostic.Error(
                        "CONTENT_DEPENDENCY_MISSING",
                        $"Пак «{pack.Manifest.Id}» зависит от отсутствующего пака «{dependency}».",
                        pack.ManifestFile,
                        pack.Manifest.Id,
                        "dependencies"));

                    continue;
                }

                indegree[pack.Manifest.Id]++;
                outgoing[dependency].Add(pack.Manifest.Id);
            }
        }

        PackOrderComparer comparer = new(byId);
        SortedSet<string> available = new(comparer);

        foreach (KeyValuePair<string, int> pair in indegree)
        {
            if (pair.Value == 0)
            {
                available.Add(pair.Key);
            }
        }

        List<ContentPack> ordered = new();
        HashSet<string> emitted = new(StringComparer.Ordinal);

        while (available.Count > 0)
        {
            string id = available.Min ?? throw new InvalidOperationException("Пустой набор доступных паков.");
            available.Remove(id);

            if (!emitted.Add(id))
            {
                continue;
            }

            ordered.Add(byId[id]);

            foreach (string next in outgoing[id])
            {
                indegree[next]--;

                if (indegree[next] == 0)
                {
                    available.Add(next);
                }
            }
        }

        if (ordered.Count != byId.Count)
        {
            List<string> unresolved = byId.Keys
                .Where(id => !emitted.Contains(id))
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();

            diagnostics.Add(ContentDiagnostic.Error(
                "CONTENT_DEPENDENCY_CYCLE",
                $"Циклическая зависимость паков: {string.Join(", ", unresolved)}.",
                field: "dependencies",
                entityId: string.Join(",", unresolved)));
        }

        return ordered.AsReadOnly();
    }

    /// <summary>
    /// Объединяет паки и строит реестр.
    /// </summary>
    /// <param name="packs">Загруженные паки.</param>
    public ContentMergeResult Merge(IReadOnlyList<ContentPack> packs)
    {
        ArgumentNullException.ThrowIfNull(packs);

        ContentDiagnosticCollection diagnostics = new();
        List<ContentChange> changes = new();
        IReadOnlyList<ContentPack> ordered = PlanOrder(packs, diagnostics);

        if (diagnostics.HasErrors)
        {
            return new ContentMergeResult(null, diagnostics, changes.AsReadOnly());
        }

        List<ContentEntry<FactionDto>> factions = MergeCategory(
            ordered, pack => pack.Factions, "faction", changes, diagnostics);
        List<ContentEntry<UnitDto>> units = MergeCategory(
            ordered, pack => pack.Units, "unit", changes, diagnostics);
        List<ContentEntry<HeroClassDto>> heroClasses = MergeCategory(
            ordered, pack => pack.HeroClasses, "heroClass", changes, diagnostics);
        List<ContentEntry<HeroDto>> heroes = MergeCategory(
            ordered, pack => pack.Heroes, "hero", changes, diagnostics);
        List<ContentEntry<AbilityDto>> abilities = MergeCategory(
            ordered, pack => pack.Abilities, "ability", changes, diagnostics);
        List<ContentEntry<SpellDto>> spells = MergeCategory(
            ordered, pack => pack.Spells, "spell", changes, diagnostics);
        List<ContentEntry<BuildingDto>> buildings = MergeCategory(
            ordered, pack => pack.Buildings, "building", changes, diagnostics);
        List<ContentEntry<SettlementTemplateDto>> settlements = MergeCategory(
            ordered, pack => pack.Settlements, "settlement", changes, diagnostics);
        List<ContentEntry<MapObjectDto>> mapObjects = MergeCategory(
            ordered, pack => pack.MapObjects, "mapObject", changes, diagnostics);
        List<ContentEntry<ResourceDefinitionDto>> resources = MergeCategory(
            ordered, pack => pack.Resources, "resource", changes, diagnostics);

        ContentEntry<BattleRulesDto>? battle = null;
        ContentEntry<MoraleRulesDto>? morale = null;
        ContentEntry<EconomyRulesDto>? economy = null;
        ContentEntry<EscapeRulesDto>? escape = null;
        ContentEntry<RansomRulesDto>? ransom = null;
        ContentEntry<VeterancyRulesDto>? veterancy = null;
        ContentEntry<ProgressionRulesDto>? progression = null;
        ContentEntry<LanguagesManifestDto>? languages = null;

        foreach (ContentPack pack in ordered)
        {
            battle = MergeRule(battle, pack.BattleRules, "rules.battle", changes);
            morale = MergeRule(morale, pack.MoraleRules, "rules.morale", changes);
            economy = MergeRule(economy, pack.EconomyRules, "rules.economy", changes);
            escape = MergeRule(escape, pack.EscapeRules, "rules.escape", changes);
            ransom = MergeRule(ransom, pack.RansomRules, "rules.ransom", changes);
            veterancy = MergeRule(veterancy, pack.VeterancyRules, "rules.veterancy", changes);
            progression = MergeRule(progression, pack.ProgressionRules, "rules.progression", changes);
            languages = MergeRule(languages, pack.LanguagesManifest, "languages", changes);
        }

        Dictionary<string, LocalizationTable> tables = new(StringComparer.Ordinal);
        Dictionary<string, string> tablePacks = new(StringComparer.Ordinal);

        foreach (ContentPack pack in ordered)
        {
            foreach (LocalizationTable table in pack.LocalizationTables)
            {
                if (tables.TryGetValue(table.Language, out LocalizationTable? existing))
                {
                    string replacedPack = tablePacks[table.Language];
                    LocalizationTable merged = existing.WithOverrides(
                        table.Status,
                        table.Fallback ?? existing.Fallback,
                        table.Entries,
                        table.SourceFile);
                    tables[table.Language] = merged;
                    tablePacks[table.Language] = pack.Manifest.Id;
                    RecordChange(
                        changes,
                        ContentChangeKind.Replace,
                        "localization",
                        table.Language,
                        pack.Manifest.Id,
                        replacedPack);
                }
                else
                {
                    tables[table.Language] = table;
                    tablePacks[table.Language] = pack.Manifest.Id;
                    RecordChange(
                        changes,
                        ContentChangeKind.Add,
                        "localization",
                        table.Language,
                        pack.Manifest.Id,
                        null);
                }
            }
        }

        LocalizationSet? localization = BuildLocalizationSet(languages, tables.Values, diagnostics);

        ContentRegistry registry = new(
            factions,
            units,
            heroClasses,
            heroes,
            abilities,
            spells,
            buildings,
            settlements,
            mapObjects,
            resources,
            battle,
            morale,
            economy,
            escape,
            ransom,
            veterancy,
            progression,
            localization,
            changes,
            ordered.Select(pack => pack.Manifest));

        _logger?.Log(LogEvent.Create(
            diagnostics.HasErrors ? LogLevel.Warning : LogLevel.Info,
            "Content",
            $"Merge контента: паков {ordered.Count}, сущностей {registry.EntityCount}, языков {localization?.Count ?? 0}, переопределений {CountReplacements(changes)}.",
            fields: new Dictionary<string, string>
            {
                ["packs"] = ordered.Count.ToString(),
                ["entities"] = registry.EntityCount.ToString(),
                ["languages"] = (localization?.Count ?? 0).ToString(),
                ["replacements"] = CountReplacements(changes).ToString()
            }));

        return new ContentMergeResult(registry, diagnostics, changes.AsReadOnly());
    }

    private static LocalizationSet? BuildLocalizationSet(
        ContentEntry<LanguagesManifestDto>? languages,
        IEnumerable<LocalizationTable> tables,
        ContentDiagnosticCollection diagnostics)
    {
        if (languages is null)
        {
            return null;
        }

        LanguagesManifestDto dto = languages.Value;
        List<LanguageDefinition> definitions = new();

        foreach (LocalizationLanguageDto language in dto.Languages)
        {
            if (!LocalizationStatusParser.TryParse(language.Status, out LocalizationStatus status))
            {
                status = LocalizationStatus.Draft;
            }

            definitions.Add(new LanguageDefinition(
                language.Code,
                language.NameKey,
                language.Script,
                status,
                language.VisibleInSettings,
                string.IsNullOrWhiteSpace(language.Fallback) ? null : language.Fallback,
                language.RequiresFontReview));
        }

        LocalizationManifest manifest = new(
            dto.DefaultLanguage,
            dto.FallbackLanguage,
            dto.RequiredLanguages,
            definitions);

        return new LocalizationSet(manifest, tables);
    }

    private List<ContentEntry<T>> MergeCategory<T>(
        IReadOnlyList<ContentPack> ordered,
        Func<ContentPack, IReadOnlyList<ContentEntry<T>>> selector,
        string category,
        List<ContentChange> changes,
        ContentDiagnosticCollection diagnostics)
        where T : class
    {
        List<ContentEntry<T>> result = new();
        Dictionary<string, int> indexes = new(StringComparer.Ordinal);

        foreach (ContentPack pack in ordered)
        {
            foreach (ContentEntry<T> entry in selector(pack))
            {
                string id = GetId(entry.Value);

                if (indexes.TryGetValue(id, out int index))
                {
                    ContentEntry<T> previous = result[index];

                    if (string.Equals(previous.SourcePack, entry.SourcePack, StringComparison.Ordinal))
                    {
                        diagnostics.Add(ContentDiagnostic.Error(
                            "CONTENT_ID_DUPLICATE",
                            $"ID «{id}» повторяется внутри пака «{entry.SourcePack}».",
                            entry.SourceFile,
                            id,
                            "id"));

                        continue;
                    }

                    result[index] = entry;
                    RecordChange(
                        changes,
                        ContentChangeKind.Replace,
                        category,
                        id,
                        entry.SourcePack,
                        previous.SourcePack);
                }
                else
                {
                    indexes[id] = result.Count;
                    result.Add(entry);
                    RecordChange(
                        changes,
                        ContentChangeKind.Add,
                        category,
                        id,
                        entry.SourcePack,
                        null);
                }
            }
        }

        return result;
    }

    private ContentEntry<T>? MergeRule<T>(
        ContentEntry<T>? current,
        ContentEntry<T>? next,
        string category,
        List<ContentChange> changes)
        where T : class
    {
        if (next is null)
        {
            return current;
        }

        string id = GetId(next.Value);

        if (current is null)
        {
            RecordChange(changes, ContentChangeKind.Add, category, id, next.SourcePack, null);
            return next;
        }

        RecordChange(
            changes,
            ContentChangeKind.Replace,
            category,
            id,
            next.SourcePack,
            current.SourcePack);

        return next;
    }

    private void RecordChange(
        List<ContentChange> changes,
        ContentChangeKind kind,
        string category,
        string id,
        string sourcePack,
        string? replacedPack)
    {
        ContentChange change = new()
        {
            Kind = kind,
            Category = category,
            EntityId = id,
            SourcePack = sourcePack,
            ReplacedPack = replacedPack
        };

        changes.Add(change);

        if (kind == ContentChangeKind.Replace)
        {
            _logger?.Log(LogEvent.Create(
                LogLevel.Info,
                "Content",
                $"Переопределение {category} «{id}»: {replacedPack} → {sourcePack}.",
                fields: new Dictionary<string, string>
                {
                    ["category"] = category,
                    ["entityId"] = id,
                    ["sourcePack"] = sourcePack,
                    ["replacedPack"] = replacedPack ?? string.Empty
                }));
        }
        else
        {
            _logger?.Log(LogEvent.Create(
                LogLevel.Debug,
                "Content",
                $"Добавлен {category} «{id}» из пака «{sourcePack}».",
                fields: new Dictionary<string, string>
                {
                    ["category"] = category,
                    ["entityId"] = id,
                    ["sourcePack"] = sourcePack
                }));
        }
    }

    private static string GetId<T>(T value)
        where T : class
    {
        return value switch
        {
            FactionDto faction => faction.Id,
            UnitDto unit => unit.Id,
            HeroClassDto heroClass => heroClass.Id,
            HeroDto hero => hero.Id,
            AbilityDto ability => ability.Id,
            SpellDto spell => spell.Id,
            BuildingDto building => building.Id,
            SettlementTemplateDto settlement => settlement.Id,
            MapObjectDto mapObject => mapObject.Id,
            ResourceDefinitionDto resource => resource.Id,
            BattleRulesDto battle => battle.Id,
            MoraleRulesDto morale => morale.Id,
            EconomyRulesDto economy => economy.Id,
            EscapeRulesDto escape => escape.Id,
            RansomRulesDto ransom => ransom.Id,
            VeterancyRulesDto veterancy => veterancy.Id,
            ProgressionRulesDto progression => progression.Id,
            LanguagesManifestDto languages => languages.Id,
            _ => string.Empty
        };
    }

    private static int CountReplacements(IEnumerable<ContentChange> changes)
    {
        int count = 0;

        foreach (ContentChange change in changes)
        {
            if (change.Kind == ContentChangeKind.Replace)
            {
                count++;
            }
        }

        return count;
    }

    private sealed class PackOrderComparer : IComparer<string>
    {
        private readonly IReadOnlyDictionary<string, ContentPack> _packs;

        public PackOrderComparer(IReadOnlyDictionary<string, ContentPack> packs)
        {
            _packs = packs;
        }

        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            int byLoadOrder = _packs[x].Manifest.LoadOrder.CompareTo(_packs[y].Manifest.LoadOrder);

            return byLoadOrder != 0 ? byLoadOrder : string.CompareOrdinal(x, y);
        }
    }
}
