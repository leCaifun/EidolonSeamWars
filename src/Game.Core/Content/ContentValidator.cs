using Game.Core.Content.Dto;
using Game.Core.Content.Localization;
using Game.Core.Diagnostics;

namespace Game.Core.Content;

/// <summary>
/// Проверяет объединённый контент: обязательные правила, форматы ID, ссылки,
/// числовые диапазоны, поселения, армии героев, правила и локализацию.
/// Сообщения содержат файл, ID и поле, чтобы ошибку можно было исправить без чтения кода.
/// </summary>
public sealed class ContentValidator
{
    private const int HeroLessLeadershipCapacity = 50;

    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт валидатор.
    /// </summary>
    public ContentValidator(IGameLogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Валидирует реестр контента.
    /// </summary>
    /// <param name="registry">Итоговый реестр после merge.</param>
    public ContentValidationResult Validate(ContentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        ContentDiagnosticCollection diagnostics = new();

        ValidateManifests(registry, diagnostics);
        ValidateIdUniqueness(registry, diagnostics);
        ValidateFactions(registry, diagnostics);
        ValidateUnits(registry, diagnostics);
        ValidateAbilities(registry, diagnostics);
        ValidateSpells(registry, diagnostics);
        ValidateHeroClasses(registry, diagnostics);
        ValidateHeroes(registry, diagnostics);
        ValidateBuildings(registry, diagnostics);
        ValidateSettlements(registry, diagnostics);
        ValidateMapObjects(registry, diagnostics);
        ValidateResources(registry, diagnostics);
        ValidateBattleRules(registry, diagnostics);
        ValidateMoraleRules(registry, diagnostics);
        ValidateEconomyRules(registry, diagnostics);
        ValidateEscapeRules(registry, diagnostics);
        ValidateRansomRules(registry, diagnostics);
        ValidateVeterancyRules(registry, diagnostics);
        ValidateProgressionRules(registry, diagnostics);
        ValidateBiomes(registry, diagnostics);
        ValidateMapGenerationRules(registry, diagnostics);
        ValidateTestMaps(registry, diagnostics);
        ValidateLocalization(registry, diagnostics);

        _logger?.Log(LogEvent.Create(
            diagnostics.HasErrors ? LogLevel.Error : LogLevel.Info,
            "Content",
            $"Валидация контента: сущностей {registry.EntityCount}, паков {registry.PackCount}, ошибок {diagnostics.ErrorCount}, предупреждений {diagnostics.WarningCount}.",
            fields: new Dictionary<string, string>
            {
                ["entities"] = registry.EntityCount.ToString(),
                ["packs"] = registry.PackCount.ToString(),
                ["errors"] = diagnostics.ErrorCount.ToString(),
                ["warnings"] = diagnostics.WarningCount.ToString()
            }));

        return new ContentValidationResult(diagnostics, registry.EntityCount);
    }

    private static void ValidateManifests(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        if (registry.PackCount == 0)
        {
            Error(diagnostics, "CONTENT_PACKS_EMPTY", "Не загружено ни одного контентного пака.");
            return;
        }

        foreach (ModManifestDto manifest in registry.Manifests)
        {
            string id = manifest.Id;

            if (string.IsNullOrWhiteSpace(id))
            {
                Error(diagnostics, "CONTENT_PACK_ID_REQUIRED", "Манифест пака не содержит id.");
            }

            if (string.IsNullOrWhiteSpace(manifest.Version))
            {
                Error(diagnostics, "CONTENT_PACK_VERSION_REQUIRED", $"У пака «{id}» не указана версия.", entityId: id, field: "version");
            }

            if (string.IsNullOrWhiteSpace(manifest.GameVersionRange))
            {
                Error(diagnostics, "CONTENT_PACK_RANGE_REQUIRED", $"У пака «{id}» не указан gameVersionRange.", entityId: id, field: "gameVersionRange");
            }

            if (manifest.LoadOrder < 0)
            {
                Error(diagnostics, "CONTENT_PACK_LOAD_ORDER_INVALID", $"У пака «{id}» отрицательный loadOrder.", entityId: id, field: "loadOrder");
            }

            if (manifest.SchemaVersion != ContentSchema.SupportedSchemaVersion)
            {
                Error(
                    diagnostics,
                    "CONTENT_SCHEMA_VERSION_UNSUPPORTED",
                    $"У пака «{id}» неподдерживаемая версия схемы {manifest.SchemaVersion}, ожидается {ContentSchema.SupportedSchemaVersion}.",
                    entityId: id,
                    field: "schemaVersion");
            }
        }
    }

    private static void ValidateIdUniqueness(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        Dictionary<string, string> seen = new(StringComparer.Ordinal);

        AddIds(registry.Factions, "фракция", seen, diagnostics);
        AddIds(registry.Units, "юнит", seen, diagnostics);
        AddIds(registry.HeroClasses, "класс героя", seen, diagnostics);
        AddIds(registry.Heroes, "герой", seen, diagnostics);
        AddIds(registry.Abilities, "способность", seen, diagnostics);
        AddIds(registry.Spells, "заклинание", seen, diagnostics);
        AddIds(registry.Buildings, "здание", seen, diagnostics);
        AddIds(registry.Settlements, "поселение", seen, diagnostics);
        AddIds(registry.MapObjects, "объект карты", seen, diagnostics);
        AddIds(registry.Resources, "ресурс", seen, diagnostics);
        AddIds(registry.TestMaps, "рукотворная карта", seen, diagnostics);
    }

    private static void AddIds<T>(
        IReadOnlyDictionary<string, ContentEntry<T>> entries,
        string category,
        Dictionary<string, string> seen,
        ContentDiagnosticCollection diagnostics)
        where T : class
    {
        foreach (ContentEntry<T> entry in entries.Values)
        {
            string id = GetId(entry.Value);

            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            if (seen.TryGetValue(id, out string? existing))
            {
                Error(
                    diagnostics,
                    "CONTENT_ID_DUPLICATE",
                    $"ID «{id}» уже используется как {existing}.",
                    entry.SourceFile,
                    id,
                    "id");

                continue;
            }

            seen[id] = category;
        }
    }

    private static void ValidateFactions(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        foreach (ContentEntry<FactionDto> entry in registry.Factions.Values)
        {
            FactionDto faction = entry.Value;
            string source = entry.SourceFile;

            RequireEntityId(faction.Id, source, diagnostics);

            if (faction.MaxSettlementLevel < ContentSchema.MinSettlementLevel
                || faction.MaxSettlementLevel > ContentSchema.MaxSettlementLevel)
            {
                Error(
                    diagnostics,
                    "CONTENT_FACTION_MAX_LEVEL",
                    $"maxSettlementLevel должен быть {ContentSchema.MinSettlementLevel}…{ContentSchema.MaxSettlementLevel}, указано {faction.MaxSettlementLevel}.",
                    source,
                    faction.Id,
                    "maxSettlementLevel");
            }

            ValidateColor(faction.Colors.Primary, "colors.primary", faction.Id, source, diagnostics);
            ValidateColor(faction.Colors.Secondary, "colors.secondary", faction.Id, source, diagnostics);
            ValidateColor(faction.Colors.Accent, "colors.accent", faction.Id, source, diagnostics);
            ValidateColor(faction.Colors.Background, "colors.background", faction.Id, source, diagnostics);
            ValidateColor(faction.Colors.Frame, "colors.frame", faction.Id, source, diagnostics);

            if (RequireReference(
                    registry.HeroClasses,
                    faction.HeroClassId,
                    "CONTENT_REF_HERO_CLASS_MISSING",
                    "класс героя",
                    source,
                    faction.Id,
                    "heroClassId",
                    diagnostics)
                && registry.FindHeroClass(faction.HeroClassId) is { } heroClass
                && !string.Equals(heroClass.FactionId, faction.Id, StringComparison.Ordinal))
            {
                Error(
                    diagnostics,
                    "CONTENT_FACTION_HERO_CLASS_MISMATCH",
                    $"Класс героя «{heroClass.Id}» принадлежит фракции «{heroClass.FactionId}», а не «{faction.Id}».",
                    source,
                    faction.Id,
                    "heroClassId");
            }

            ValidateTags(faction.Tags, "tags", faction.Id, source, diagnostics);
        }
    }

    private static void ValidateUnits(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        foreach (ContentEntry<UnitDto> entry in registry.Units.Values)
        {
            UnitDto unit = entry.Value;
            string source = entry.SourceFile;

            RequireEntityId(unit.Id, source, diagnostics);

            if (!RequireReference(
                    registry.Factions,
                    unit.FactionId,
                    "CONTENT_REF_FACTION_MISSING",
                    "фракция",
                    source,
                    unit.Id,
                    "factionId",
                    diagnostics))
            {
                // Ссылка уже отмечена ошибкой; остальные проверки юнита всё равно полезны.
            }

            RequireRange(unit.Tier, 1, 5, "tier", unit.Id, source, diagnostics, "CONTENT_UNIT_TIER");
            RequireAtLeast(unit.LeadershipCost, 1, "leadershipCost", unit.Id, source, diagnostics, "CONTENT_UNIT_LEADERSHIP");
            RequireAtLeast(unit.Attack, 0, "attack", unit.Id, source, diagnostics, "CONTENT_UNIT_ATTACK");
            RequireAtLeast(unit.Defense, 0, "defense", unit.Id, source, diagnostics, "CONTENT_UNIT_DEFENSE");
            RequireAtLeast(unit.Initiative, 1, "initiative", unit.Id, source, diagnostics, "CONTENT_UNIT_INITIATIVE");
            RequireAtLeast(unit.Speed, 1, "speed", unit.Id, source, diagnostics, "CONTENT_UNIT_SPEED");
            RequireAtLeast(unit.DamageMin, 0, "damageMin", unit.Id, source, diagnostics, "CONTENT_UNIT_DAMAGE_MIN");
            RequireAtLeast(unit.DamageMax, 1, "damageMax", unit.Id, source, diagnostics, "CONTENT_UNIT_DAMAGE_MAX");

            if (unit.DamageMin > unit.DamageMax)
            {
                Error(
                    diagnostics,
                    "CONTENT_UNIT_DAMAGE_ORDER",
                    $"damageMin {unit.DamageMin} больше damageMax {unit.DamageMax}.",
                    source,
                    unit.Id,
                    "damageMin");
            }

            RequireRange(unit.CritChancePercent, 0, 75, "critChancePercent", unit.Id, source, diagnostics, "CONTENT_UNIT_CRIT_CHANCE");
            RequireRange(unit.CritDamagePercent, 100, 300, "critDamagePercent", unit.Id, source, diagnostics, "CONTENT_UNIT_CRIT_DAMAGE");
            RequireAtLeast(unit.HitPoints, 1, "hitPoints", unit.Id, source, diagnostics, "CONTENT_UNIT_HIT_POINTS");

            if (!ContentSchema.MovementTypes.Contains(unit.MovementType))
            {
                Error(
                    diagnostics,
                    "CONTENT_UNIT_MOVEMENT_TYPE",
                    $"Неизвестный movementType «{unit.MovementType}»; допустимы {Join(ContentSchema.MovementTypes)}.",
                    source,
                    unit.Id,
                    "movementType");
            }

            if (!ContentSchema.DamageTypes.Contains(unit.DamageType))
            {
                Error(
                    diagnostics,
                    "CONTENT_UNIT_DAMAGE_TYPE",
                    $"Неизвестный damageType «{unit.DamageType}»; допустимы {Join(ContentSchema.DamageTypes)}.",
                    source,
                    unit.Id,
                    "damageType");
            }

            ValidateResistances(unit, source, diagnostics);
            ValidateAbilityIds(unit.Abilities, registry, unit.Id, "abilities", source, diagnostics);
            ValidateResourceMap(unit.RecruitCost, "recruitCost", unit.Id, source, diagnostics);

            if (unit.RecruitCost.Count == 0)
            {
                Warning(
                    diagnostics,
                    "CONTENT_UNIT_RECRUIT_COST_EMPTY",
                    "У юнита не задана стоимость найма.",
                    source,
                    unit.Id,
                    "recruitCost");
            }

            if (unit.Ammo < 0)
            {
                Error(
                    diagnostics,
                    "CONTENT_UNIT_AMMO",
                    $"Зарезервированный боезапас не может быть отрицательным: {unit.Ammo}.",
                    source,
                    unit.Id,
                    "ammo");
            }

            if (unit.PromotionIds.Count > 0)
            {
                Warning(
                    diagnostics,
                    "CONTENT_PROMOTION_NOT_IMPLEMENTED",
                    "Промоушены M2 указаны в контенте, но этап 03 их не загружает.",
                    source,
                    unit.Id,
                    "promotionIds");
            }
        }
    }

    private static void ValidateAbilities(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        foreach (ContentEntry<AbilityDto> entry in registry.Abilities.Values)
        {
            AbilityDto ability = entry.Value;
            string source = entry.SourceFile;

            RequireEntityId(ability.Id, source, diagnostics);

            if (!ContentSchema.AbilityKinds.Contains(ability.Kind))
            {
                Error(
                    diagnostics,
                    "CONTENT_ABILITY_KIND",
                    $"Неизвестный kind «{ability.Kind}»; допустимы {Join(ContentSchema.AbilityKinds)}.",
                    source,
                    ability.Id,
                    "kind");
            }

            if (!ContentSchema.AbilityTriggers.Contains(ability.Trigger))
            {
                Error(
                    diagnostics,
                    "CONTENT_ABILITY_TRIGGER",
                    $"Неизвестный trigger «{ability.Trigger}»; допустимы {Join(ContentSchema.AbilityTriggers)}.",
                    source,
                    ability.Id,
                    "trigger");
            }

            if (ability.Target is not null && !ContentSchema.AbilityTargets.Contains(ability.Target))
            {
                Error(
                    diagnostics,
                    "CONTENT_ABILITY_TARGET",
                    $"Неизвестный target «{ability.Target}»; допустимы {Join(ContentSchema.AbilityTargets)}.",
                    source,
                    ability.Id,
                    "target");
            }

            ValidateOptionalNonNegative(ability.UsesPerBattle, "usesPerBattle", ability.Id, source, diagnostics, "CONTENT_ABILITY_USES");
            ValidateOptionalNonNegative(ability.CooldownRounds, "cooldownRounds", ability.Id, source, diagnostics, "CONTENT_ABILITY_COOLDOWN");
            ValidateOptionalNonNegative(ability.DurationRounds, "durationRounds", ability.Id, source, diagnostics, "CONTENT_ABILITY_DURATION");
            ValidateOptionalNonNegative(ability.Radius, "radius", ability.Id, source, diagnostics, "CONTENT_ABILITY_RADIUS");
            ValidateOptionalNonNegative(ability.Range, "range", ability.Id, source, diagnostics, "CONTENT_ABILITY_RANGE");

            if (ability.Effects.Count == 0)
            {
                Warning(
                    diagnostics,
                    "CONTENT_ABILITY_EFFECTS_EMPTY",
                    "Способность не содержит эффектов.",
                    source,
                    ability.Id,
                    "effects");
            }

            for (int index = 0; index < ability.Effects.Count; index++)
            {
                ValidateEffect(
                    ability.Effects[index],
                    registry,
                    ability.Id,
                    $"effects[{index}]",
                    source,
                    diagnostics);
            }
        }
    }

    private static void ValidateSpells(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        foreach (ContentEntry<SpellDto> entry in registry.Spells.Values)
        {
            SpellDto spell = entry.Value;
            string source = entry.SourceFile;

            RequireEntityId(spell.Id, source, diagnostics);

            if (string.IsNullOrWhiteSpace(spell.School))
            {
                Error(diagnostics, "CONTENT_SPELL_SCHOOL", "Школа заклинания не может быть пустой.", source, spell.Id, "school");
            }

            RequireAtLeast(spell.ManaCost, 0, "manaCost", spell.Id, source, diagnostics, "CONTENT_SPELL_MANA");

            if (!ContentSchema.AbilityTargets.Contains(spell.Target))
            {
                Error(
                    diagnostics,
                    "CONTENT_SPELL_TARGET",
                    $"Неизвестный target «{spell.Target}»; допустимы {Join(ContentSchema.AbilityTargets)}.",
                    source,
                    spell.Id,
                    "target");
            }

            ValidateOptionalNonNegative(spell.DurationRounds, "durationRounds", spell.Id, source, diagnostics, "CONTENT_SPELL_DURATION");

            if (spell.Effects.Count == 0)
            {
                Warning(
                    diagnostics,
                    "CONTENT_SPELL_EFFECTS_EMPTY",
                    "Заклинание не содержит эффектов.",
                    source,
                    spell.Id,
                    "effects");
            }

            for (int index = 0; index < spell.Effects.Count; index++)
            {
                ValidateEffect(
                    spell.Effects[index],
                    registry,
                    spell.Id,
                    $"effects[{index}]",
                    source,
                    diagnostics);
            }
        }
    }

    private static void ValidateEffect(
        AbilityEffectDto effect,
        ContentRegistry registry,
        string entityId,
        string fieldPrefix,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        if (string.IsNullOrWhiteSpace(effect.Type))
        {
            Error(
                diagnostics,
                "CONTENT_EFFECT_TYPE_REQUIRED",
                "У эффекта отсутствует type.",
                source,
                entityId,
                $"{fieldPrefix}.type");
        }

        if (!string.IsNullOrWhiteSpace(effect.DamageType)
            && !ContentSchema.DamageTypes.Contains(effect.DamageType))
        {
            Error(
                diagnostics,
                "CONTENT_EFFECT_DAMAGE_TYPE",
                $"Неизвестный damageType «{effect.DamageType}»; допустимы {Join(ContentSchema.DamageTypes)}.",
                source,
                entityId,
                $"{fieldPrefix}.damageType");
        }

        if (!string.IsNullOrWhiteSpace(effect.UnitId))
        {
            RequireReference(
                registry.Units,
                effect.UnitId,
                "CONTENT_REF_UNIT_MISSING",
                "юнит",
                source,
                entityId,
                $"{fieldPrefix}.unitId",
                diagnostics);
        }

        ValidateOptionalNonNegative(effect.Min, $"{fieldPrefix}.min", entityId, source, diagnostics, "CONTENT_EFFECT_MIN");
        ValidateOptionalNonNegative(effect.Max, $"{fieldPrefix}.max", entityId, source, diagnostics, "CONTENT_EFFECT_MAX");

        if (effect.Min is int min && effect.Max is int max && min > max)
        {
            Error(
                diagnostics,
                "CONTENT_EFFECT_RANGE",
                $"min {min} больше max {max}.",
                source,
                entityId,
                $"{fieldPrefix}.min");
        }

        if (effect.Count is int count && count <= 0)
        {
            Error(
                diagnostics,
                "CONTENT_EFFECT_COUNT",
                $"count должен быть положительным, указано {count}.",
                source,
                entityId,
                $"{fieldPrefix}.count");
        }

        if (effect.MaxTier is int maxTier && (maxTier < 1 || maxTier > 5))
        {
            Error(
                diagnostics,
                "CONTENT_EFFECT_MAX_TIER",
                $"maxTier должен быть 1…5, указано {maxTier}.",
                source,
                entityId,
                $"{fieldPrefix}.maxTier");
        }

        ValidateOptionalNonNegative(effect.MaxCountPerStack, $"{fieldPrefix}.maxCountPerStack", entityId, source, diagnostics, "CONTENT_EFFECT_MAX_COUNT");
        ValidateOptionalNonNegative(effect.DurationRounds, $"{fieldPrefix}.durationRounds", entityId, source, diagnostics, "CONTENT_EFFECT_DURATION");
        ValidateOptionalNonNegative(effect.MinHexes, $"{fieldPrefix}.minHexes", entityId, source, diagnostics, "CONTENT_EFFECT_MIN_HEXES");
    }

    private static void ValidateHeroClasses(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        foreach (ContentEntry<HeroClassDto> entry in registry.HeroClasses.Values)
        {
            HeroClassDto heroClass = entry.Value;
            string source = entry.SourceFile;

            RequireEntityId(heroClass.Id, source, diagnostics);

            if (RequireReference(
                    registry.Factions,
                    heroClass.FactionId,
                    "CONTENT_REF_FACTION_MISSING",
                    "фракция",
                    source,
                    heroClass.Id,
                    "factionId",
                    diagnostics)
                && registry.FindFaction(heroClass.FactionId) is { } faction
                && !string.Equals(faction.HeroClassId, heroClass.Id, StringComparison.Ordinal))
            {
                Error(
                    diagnostics,
                    "CONTENT_HERO_CLASS_FACTION_MISMATCH",
                    $"Фракция «{faction.Id}» указывает класс «{faction.HeroClassId}», а не «{heroClass.Id}».",
                    source,
                    heroClass.Id,
                    "factionId");
            }

            ValidateStats(heroClass.StartingStats, "startingStats", heroClass.Id, source, diagnostics, requireLeadership: true);
            ValidateStatWeights(heroClass.StatWeights, heroClass.Id, source, diagnostics);

            RequireReference(
                registry.Abilities,
                heroClass.PassiveAbilityId,
                "CONTENT_REF_ABILITY_MISSING",
                "способность",
                source,
                heroClass.Id,
                "passiveAbilityId",
                diagnostics);
            RequireReference(
                registry.Abilities,
                heroClass.CommandAbilityId,
                "CONTENT_REF_ABILITY_MISSING",
                "способность",
                source,
                heroClass.Id,
                "commandAbilityId",
                diagnostics);
            RequireReference(
                registry.Spells,
                heroClass.SpellId,
                "CONTENT_REF_SPELL_MISSING",
                "заклинание",
                source,
                heroClass.Id,
                "spellId",
                diagnostics);
        }
    }

    private static void ValidateHeroes(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        foreach (ContentEntry<HeroDto> entry in registry.Heroes.Values)
        {
            HeroDto hero = entry.Value;
            string source = entry.SourceFile;

            RequireEntityId(hero.Id, source, diagnostics);

            RequireReference(
                registry.Factions,
                hero.FactionId,
                "CONTENT_REF_FACTION_MISSING",
                "фракция",
                source,
                hero.Id,
                "factionId",
                diagnostics);

            bool classExists = RequireReference(
                registry.HeroClasses,
                hero.ClassId,
                "CONTENT_REF_HERO_CLASS_MISSING",
                "класс героя",
                source,
                hero.Id,
                "classId",
                diagnostics);

            if (classExists
                && registry.FindHeroClass(hero.ClassId) is { } heroClass
                && !string.Equals(heroClass.FactionId, hero.FactionId, StringComparison.Ordinal))
            {
                Error(
                    diagnostics,
                    "CONTENT_HERO_CLASS_MISMATCH",
                    $"Класс «{heroClass.Id}» принадлежит фракции «{heroClass.FactionId}», а герой — «{hero.FactionId}».",
                    source,
                    hero.Id,
                    "classId");
            }

            RequireAtLeast(hero.Level, 1, "level", hero.Id, source, diagnostics, "CONTENT_HERO_LEVEL");
            RequireAtLeast(hero.Xp, 0, "xp", hero.Id, source, diagnostics, "CONTENT_HERO_XP");
            RequireAtLeast(hero.SkillPoints, 0, "skillPoints", hero.Id, source, diagnostics, "CONTENT_HERO_SKILL_POINTS");
            ValidateStats(hero.Stats, "stats", hero.Id, source, diagnostics, requireLeadership: true);
            ValidateHeroArmy(hero, registry, source, diagnostics);
        }
    }

    private static void ValidateHeroArmy(
        HeroDto hero,
        ContentRegistry registry,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        if (hero.Army.Count == 0)
        {
            Warning(
                diagnostics,
                "CONTENT_HERO_ARMY_EMPTY",
                "Стартовая армия героя пуста.",
                source,
                hero.Id,
                "army");

            return;
        }

        if (hero.Army.Count > ContentSchema.MaxArmySlots)
        {
            Error(
                diagnostics,
                "CONTENT_HERO_ARMY_SLOTS",
                $"Слотов армии {hero.Army.Count}, максимум {ContentSchema.MaxArmySlots}.",
                source,
                hero.Id,
                "army");
        }

        HashSet<string> seenUnits = new(StringComparer.Ordinal);
        long leadership = 0;

        for (int index = 0; index < hero.Army.Count; index++)
        {
            HeroArmySlotDto slot = hero.Army[index];
            string field = $"army[{index}]";

            if (slot.Count <= 0)
            {
                Error(
                    diagnostics,
                    "CONTENT_HERO_ARMY_COUNT",
                    $"Количество юнитов должно быть положительным, указано {slot.Count}.",
                    source,
                    hero.Id,
                    $"{field}.count");
            }

            if (!seenUnits.Add(slot.UnitId))
            {
                Error(
                    diagnostics,
                    "CONTENT_HERO_ARMY_DUPLICATE",
                    $"Юнит «{slot.UnitId}» указан в армии более одного раза.",
                    source,
                    hero.Id,
                    $"{field}.unitId");
            }

            if (!RequireReference(
                    registry.Units,
                    slot.UnitId,
                    "CONTENT_REF_UNIT_MISSING",
                    "юнит",
                    source,
                    hero.Id,
                    $"{field}.unitId",
                    diagnostics))
            {
                continue;
            }

            UnitDto unit = registry.FindUnit(slot.UnitId)!;

            if (!string.Equals(unit.FactionId, hero.FactionId, StringComparison.Ordinal))
            {
                Error(
                    diagnostics,
                    "CONTENT_HERO_ARMY_FACTION",
                    $"Юнит «{unit.Id}» принадлежит фракции «{unit.FactionId}», а герой — «{hero.FactionId}».",
                    source,
                    hero.Id,
                    $"{field}.unitId");
            }

            if (slot.Count > 0)
            {
                leadership = checked(leadership + ((long)unit.LeadershipCost * slot.Count));
            }
        }

        if (leadership > hero.Stats.Leadership)
        {
            Error(
                diagnostics,
                "CONTENT_HERO_ARMY_LEADERSHIP",
                $"Стартовая армия требует {leadership} лидерства, у героя {hero.Stats.Leadership}.",
                source,
                hero.Id,
                "army");
        }
    }

    private static void ValidateBuildings(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        Dictionary<string, string> names = new(StringComparer.Ordinal);

        foreach (ContentEntry<BuildingDto> entry in registry.Buildings.Values)
        {
            BuildingDto building = entry.Value;
            string source = entry.SourceFile;

            RequireEntityId(building.Id, source, diagnostics);

            bool factionExists = RequireReference(
                registry.Factions,
                building.FactionId,
                "CONTENT_REF_FACTION_MISSING",
                "фракция",
                source,
                building.Id,
                "factionId",
                diagnostics);

            if (factionExists && registry.FindFaction(building.FactionId) is { } faction)
            {
                RequireRange(
                    building.RequiredLevel,
                    1,
                    faction.MaxSettlementLevel,
                    "requiredLevel",
                    building.Id,
                    source,
                    diagnostics,
                    "CONTENT_BUILDING_REQUIRED_LEVEL");
            }

            ValidateResourceMap(building.Cost, "cost", building.Id, source, diagnostics);
            ValidateResourceMap(building.DailyIncome, "dailyIncome", building.Id, source, diagnostics);

            string uniqueKey = $"{building.FactionId}\u001f{building.NameKey}";

            if (names.TryGetValue(uniqueKey, out string? existingId))
            {
                Error(
                    diagnostics,
                    "CONTENT_BUILDING_NAME_DUPLICATE",
                    $"Название «{building.NameKey}» уже используется зданием «{existingId}» этой фракции.",
                    source,
                    building.Id,
                    "nameKey");
            }
            else
            {
                names[uniqueKey] = building.Id;
            }

            if (building.Recruitment is { } recruitment)
            {
                ValidateRecruitment(recruitment, building.FactionId, building.Id, "recruitment", source, registry, diagnostics);
            }
        }
    }

    private static void ValidateRecruitment(
        RecruitmentDto recruitment,
        string? expectedFactionId,
        string entityId,
        string fieldPrefix,
        string source,
        ContentRegistry registry,
        ContentDiagnosticCollection diagnostics)
    {
        if (!RequireReference(
                registry.Units,
                recruitment.UnitId,
                "CONTENT_REF_UNIT_MISSING",
                "юнит",
                source,
                entityId,
                $"{fieldPrefix}.unitId",
                diagnostics))
        {
            return;
        }

        UnitDto unit = registry.FindUnit(recruitment.UnitId)!;

        if (expectedFactionId is not null
            && !string.Equals(unit.FactionId, expectedFactionId, StringComparison.Ordinal))
        {
            Warning(
                diagnostics,
                "CONTENT_RECRUITMENT_FACTION_MISMATCH",
                $"Нанимаемый юнит «{unit.Id}» принадлежит фракции «{unit.FactionId}», а владелец — «{expectedFactionId}».",
                source,
                entityId,
                $"{fieldPrefix}.unitId");
        }

        if (recruitment.GrowthPerDayMilli <= 0)
        {
            Error(
                diagnostics,
                "CONTENT_RECRUITMENT_GROWTH",
                $"growthPerDayMilli должен быть положительным, указано {recruitment.GrowthPerDayMilli}.",
                source,
                entityId,
                $"{fieldPrefix}.growthPerDayMilli");
        }

        if (recruitment.StockCap <= 0)
        {
            Error(
                diagnostics,
                "CONTENT_RECRUITMENT_CAP",
                $"stockCap должен быть положительным, указано {recruitment.StockCap}.",
                source,
                entityId,
                $"{fieldPrefix}.stockCap");
        }
    }

    private static void ValidateSettlements(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        foreach (ContentEntry<SettlementTemplateDto> entry in registry.Settlements.Values)
        {
            SettlementTemplateDto settlement = entry.Value;
            string source = entry.SourceFile;

            RequireEntityId(settlement.Id, source, diagnostics);

            bool factionExists = RequireReference(
                registry.Factions,
                settlement.FactionId,
                "CONTENT_REF_FACTION_MISSING",
                "фракция",
                source,
                settlement.Id,
                "factionId",
                diagnostics);

            RequireRange(
                settlement.MaxLevel,
                ContentSchema.MinSettlementLevel,
                ContentSchema.MaxSettlementLevel,
                "maxLevel",
                settlement.Id,
                source,
                diagnostics,
                "CONTENT_SETTLEMENT_MAX_LEVEL");

            if (factionExists && registry.FindFaction(settlement.FactionId) is { } faction
                && settlement.MaxLevel != faction.MaxSettlementLevel)
            {
                Error(
                    diagnostics,
                    "CONTENT_SETTLEMENT_FACTION_LEVEL",
                    $"maxLevel поселения {settlement.MaxLevel} не совпадает с maxSettlementLevel фракции {faction.MaxSettlementLevel}.",
                    source,
                    settlement.Id,
                    "maxLevel");
            }

            ValidateSettlementLevels(settlement, source, diagnostics);
            ValidateSettlementUpgrades(settlement, source, diagnostics);
            ValidateDefaultBuildings(settlement, registry, source, diagnostics);
        }
    }

    private static void ValidateSettlementLevels(
        SettlementTemplateDto settlement,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        Dictionary<int, SettlementLevelDto> byLevel = new();

        foreach (SettlementLevelDto level in settlement.Levels)
        {
            if (byLevel.ContainsKey(level.Level))
            {
                Error(
                    diagnostics,
                    "CONTENT_SETTLEMENT_LEVEL_DUPLICATE",
                    $"Уровень {level.Level} объявлен более одного раза.",
                    source,
                    settlement.Id,
                    "levels");
            }
            else
            {
                byLevel[level.Level] = level;
            }
        }

        int previousSlots = 0;

        for (int level = 1; level <= settlement.MaxLevel; level++)
        {
            if (!byLevel.TryGetValue(level, out SettlementLevelDto? definition))
            {
                Error(
                    diagnostics,
                    "CONTENT_SETTLEMENT_LEVEL_MISSING",
                    $"Отсутствует описание уровня {level}.",
                    source,
                    settlement.Id,
                    "levels");

                continue;
            }

            if (definition.BuildSlots <= 0)
            {
                Error(
                    diagnostics,
                    "CONTENT_SETTLEMENT_SLOTS",
                    $"У уровня {level} число слотов должно быть положительным, указано {definition.BuildSlots}.",
                    source,
                    settlement.Id,
                    $"levels[{level}].buildSlots");
            }

            if (level > 1 && definition.BuildSlots < previousSlots)
            {
                Error(
                    diagnostics,
                    "CONTENT_SETTLEMENT_SLOTS_ORDER",
                    $"Число слотов уровня {level} ({definition.BuildSlots}) меньше, чем у предыдущего уровня ({previousSlots}).",
                    source,
                    settlement.Id,
                    $"levels[{level}].buildSlots");
            }

            previousSlots = definition.BuildSlots;
        }

        foreach (int level in byLevel.Keys)
        {
            if (level < 1 || level > settlement.MaxLevel)
            {
                Error(
                    diagnostics,
                    "CONTENT_SETTLEMENT_LEVEL_RANGE",
                    $"Уровень {level} вне диапазона 1…{settlement.MaxLevel}.",
                    source,
                    settlement.Id,
                    "levels");
            }
        }
    }

    private static void ValidateSettlementUpgrades(
        SettlementTemplateDto settlement,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        Dictionary<int, SettlementUpgradeDto> byFromLevel = new();

        foreach (SettlementUpgradeDto upgrade in settlement.Upgrades)
        {
            if (byFromLevel.ContainsKey(upgrade.FromLevel))
            {
                Error(
                    diagnostics,
                    "CONTENT_SETTLEMENT_UPGRADE_DUPLICATE",
                    $"Улучшение с уровня {upgrade.FromLevel} объявлено более одного раза.",
                    source,
                    settlement.Id,
                    "upgrades");
            }
            else
            {
                byFromLevel[upgrade.FromLevel] = upgrade;
            }

            if (upgrade.ToLevel != upgrade.FromLevel + 1)
            {
                Error(
                    diagnostics,
                    "CONTENT_SETTLEMENT_UPGRADE_STEP",
                    $"Улучшение {upgrade.FromLevel}→{upgrade.ToLevel} должно вести на следующий уровень.",
                    source,
                    settlement.Id,
                    "upgrades");
            }

            if (upgrade.Days <= 0)
            {
                Error(
                    diagnostics,
                    "CONTENT_SETTLEMENT_UPGRADE_DAYS",
                    $"Длительность улучшения должна быть положительной, указано {upgrade.Days}.",
                    source,
                    settlement.Id,
                    "upgrades.days");
            }

            ValidateResourceMap(upgrade.Cost, "upgrades.cost", settlement.Id, source, diagnostics);

            if (upgrade.Cost.Count == 0)
            {
                Warning(
                    diagnostics,
                    "CONTENT_SETTLEMENT_UPGRADE_COST_EMPTY",
                    "Улучшение не имеет стоимости.",
                    source,
                    settlement.Id,
                    "upgrades.cost");
            }
        }

        for (int level = 1; level < settlement.MaxLevel; level++)
        {
            if (!byFromLevel.ContainsKey(level))
            {
                Error(
                    diagnostics,
                    "CONTENT_SETTLEMENT_UPGRADE_MISSING",
                    $"Отсутствует улучшение с уровня {level}.",
                    source,
                    settlement.Id,
                    "upgrades");
            }
        }
    }

    private static void ValidateDefaultBuildings(
        SettlementTemplateDto settlement,
        ContentRegistry registry,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        if (settlement.DefaultBuildings.Count == 0)
        {
            Warning(
                diagnostics,
                "CONTENT_SETTLEMENT_DEFAULT_BUILDINGS_EMPTY",
                "У поселения нет зданий по умолчанию.",
                source,
                settlement.Id,
                "defaultBuildings");

            return;
        }

        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (string buildingId in settlement.DefaultBuildings)
        {
            if (!seen.Add(buildingId))
            {
                Error(
                    diagnostics,
                    "CONTENT_SETTLEMENT_DEFAULT_BUILDING_DUPLICATE",
                    $"Здание «{buildingId}» указано более одного раза.",
                    source,
                    settlement.Id,
                    "defaultBuildings");
            }

            if (!RequireReference(
                    registry.Buildings,
                    buildingId,
                    "CONTENT_REF_BUILDING_MISSING",
                    "здание",
                    source,
                    settlement.Id,
                    "defaultBuildings",
                    diagnostics))
            {
                continue;
            }

            BuildingDto building = registry.FindBuilding(buildingId)!;

            if (!string.Equals(building.FactionId, settlement.FactionId, StringComparison.Ordinal))
            {
                Error(
                    diagnostics,
                    "CONTENT_SETTLEMENT_DEFAULT_BUILDING_FACTION",
                    $"Здание «{building.Id}» принадлежит фракции «{building.FactionId}», а поселение — «{settlement.FactionId}».",
                    source,
                    settlement.Id,
                    "defaultBuildings");
            }
        }

        SettlementLevelDto? firstLevel = settlement.Levels.FirstOrDefault(level => level.Level == 1);

        if (firstLevel is not null && settlement.DefaultBuildings.Count > firstLevel.BuildSlots)
        {
            Error(
                diagnostics,
                "CONTENT_SETTLEMENT_DEFAULT_BUILDINGS_SLOTS",
                $"Зданий по умолчанию {settlement.DefaultBuildings.Count}, а слотов на уровне 1 — {firstLevel.BuildSlots}.",
                source,
                settlement.Id,
                "defaultBuildings");
        }
    }

    private static void ValidateMapObjects(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        foreach (ContentEntry<MapObjectDto> entry in registry.MapObjects.Values)
        {
            MapObjectDto mapObject = entry.Value;
            string source = entry.SourceFile;

            RequireEntityId(mapObject.Id, source, diagnostics);

            if (!ContentSchema.MapObjectTypes.Contains(mapObject.Type))
            {
                Error(
                    diagnostics,
                    "CONTENT_MAP_OBJECT_TYPE",
                    $"Неизвестный type «{mapObject.Type}»; допустимы {Join(ContentSchema.MapObjectTypes)}.",
                    source,
                    mapObject.Id,
                    "type");

                continue;
            }

            switch (mapObject.Type)
            {
                case "mine":
                    ValidateMine(mapObject, source, diagnostics);
                    break;
                case "dwelling":
                    ValidateDwelling(mapObject, registry, source, diagnostics);
                    break;
                case "echoCache":
                    ValidateEchoCache(mapObject, source, diagnostics);
                    break;
                case "patrol":
                    ValidatePatrol(mapObject, registry, source, diagnostics);
                    break;
                case "settlement":
                    ValidateSettlementObject(mapObject, registry, source, diagnostics);
                    break;
            }
        }
    }

    private static void ValidateMine(
        MapObjectDto mapObject,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        if (mapObject.IncomePerDay.Count == 0)
        {
            Error(
                diagnostics,
                "CONTENT_MINE_INCOME_EMPTY",
                "У шахты отсутствует incomePerDay.",
                source,
                mapObject.Id,
                "incomePerDay");

            return;
        }

        foreach (KeyValuePair<string, int> pair in mapObject.IncomePerDay)
        {
            if (!ContentSchema.IsResourceId(pair.Key))
            {
                Error(
                    diagnostics,
                    "CONTENT_RESOURCE_UNKNOWN",
                    $"Неизвестный ресурс «{pair.Key}»; допустимы {Join(ContentSchema.ResourceIds)}.",
                    source,
                    mapObject.Id,
                    $"incomePerDay.{pair.Key}");
            }

            if (pair.Value <= 0)
            {
                Error(
                    diagnostics,
                    "CONTENT_MINE_INCOME_VALUE",
                    $"Доход шахты должен быть положительным, указано {pair.Value}.",
                    source,
                    mapObject.Id,
                    $"incomePerDay.{pair.Key}");
            }
        }
    }

    private static void ValidateDwelling(
        MapObjectDto mapObject,
        ContentRegistry registry,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        RequireReference(
            registry.Factions,
            mapObject.FactionId,
            "CONTENT_REF_FACTION_MISSING",
            "фракция",
            source,
            mapObject.Id,
            "factionId",
            diagnostics);

        if (mapObject.Recruitment is not { } recruitment)
        {
            Error(
                diagnostics,
                "CONTENT_DWELLING_RECRUITMENT",
                "У внешнего жилища отсутствует recruitment.",
                source,
                mapObject.Id,
                "recruitment");

            return;
        }

        ValidateRecruitment(recruitment, mapObject.FactionId, mapObject.Id, "recruitment", source, registry, diagnostics);
    }

    private static void ValidateSettlementObject(
        MapObjectDto mapObject,
        ContentRegistry registry,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        RequireReference(
            registry.Factions,
            mapObject.FactionId,
            "CONTENT_REF_FACTION_MISSING",
            "фракция",
            source,
            mapObject.Id,
            "factionId",
            diagnostics);

        if (!RequireReference(
                registry.Settlements,
                mapObject.TemplateId,
                "CONTENT_REF_SETTLEMENT_MISSING",
                "шаблон поселения",
                source,
                mapObject.Id,
                "templateId",
                diagnostics))
        {
            return;
        }

        SettlementTemplateDto? template = mapObject.TemplateId is null
            ? null
            : registry.Settlements[mapObject.TemplateId].Value;

        int level = mapObject.InitialLevel ?? 1;

        if (level < 1)
        {
            Error(
                diagnostics,
                "CONTENT_SETTLEMENT_OBJECT_LEVEL",
                $"initialLevel должен быть не меньше 1, указано {level}.",
                source,
                mapObject.Id,
                "initialLevel");

            return;
        }

        if (template is not null && level > template.MaxLevel)
        {
            Error(
                diagnostics,
                "CONTENT_SETTLEMENT_OBJECT_LEVEL_MAX",
                $"initialLevel {level} больше maxLevel {template.MaxLevel} шаблона «{template.Id}».",
                source,
                mapObject.Id,
                "initialLevel");
        }
    }

    private static void ValidateEchoCache(
        MapObjectDto mapObject,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        if (mapObject.Options.Count == 0)
        {
            Error(
                diagnostics,
                "CONTENT_ECHO_OPTIONS_EMPTY",
                "У эхо-схрона отсутствуют варианты взаимодействия.",
                source,
                mapObject.Id,
                "options");

            return;
        }

        HashSet<string> optionIds = new(StringComparer.Ordinal);

        for (int index = 0; index < mapObject.Options.Count; index++)
        {
            MapObjectOptionDto option = mapObject.Options[index];
            string field = $"options[{index}]";

            if (string.IsNullOrWhiteSpace(option.Id))
            {
                Error(
                    diagnostics,
                    "CONTENT_ECHO_OPTION_ID",
                    "У варианта отсутствует локальный id.",
                    source,
                    mapObject.Id,
                    $"{field}.id");
            }
            else if (!optionIds.Add(option.Id))
            {
                Error(
                    diagnostics,
                    "CONTENT_ECHO_OPTION_DUPLICATE",
                    $"Вариант «{option.Id}» объявлен более одного раза.",
                    source,
                    mapObject.Id,
                    $"{field}.id");
            }

            if (string.IsNullOrWhiteSpace(option.NameKey))
            {
                Error(
                    diagnostics,
                    "CONTENT_ECHO_OPTION_NAME",
                    "У варианта отсутствует nameKey.",
                    source,
                    mapObject.Id,
                    $"{field}.nameKey");
            }

            if (string.IsNullOrWhiteSpace(option.DescriptionKey))
            {
                Error(
                    diagnostics,
                    "CONTENT_ECHO_OPTION_DESC",
                    "У варианта отсутствует descriptionKey.",
                    source,
                    mapObject.Id,
                    $"{field}.descriptionKey");
            }

            ValidateResourceMap(option.Cost, $"{field}.cost", mapObject.Id, source, diagnostics);
            ValidateRewardMap(option.Reward, $"{field}.reward", mapObject.Id, source, diagnostics);
            ValidateRewardMap(option.DelayedReward, $"{field}.delayedReward", mapObject.Id, source, diagnostics);

            if (option.DelayedReward.Count > 0)
            {
                if (option.DelayedRewardDays is not int days || days <= 0)
                {
                    Error(
                        diagnostics,
                        "CONTENT_ECHO_DELAYED_DAYS",
                        "Для delayedReward обязателен положительный delayedRewardDays.",
                        source,
                        mapObject.Id,
                        $"{field}.delayedRewardDays");
                }
            }
            else if (option.DelayedRewardDays is not null)
            {
                Warning(
                    diagnostics,
                    "CONTENT_ECHO_DELAYED_EMPTY",
                    "delayedRewardDays задан без delayedReward.",
                    source,
                    mapObject.Id,
                    $"{field}.delayedRewardDays");
            }
        }
    }

    private static void ValidatePatrol(
        MapObjectDto mapObject,
        ContentRegistry registry,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        RequireReference(
            registry.Factions,
            mapObject.FactionId,
            "CONTENT_REF_FACTION_MISSING",
            "фракция",
            source,
            mapObject.Id,
            "factionId",
            diagnostics);

        int capacity = HeroLessLeadershipCapacity;

        if (!string.IsNullOrWhiteSpace(mapObject.HeroId))
        {
            if (RequireReference(
                    registry.Heroes,
                    mapObject.HeroId,
                    "CONTENT_REF_HERO_MISSING",
                    "герой",
                    source,
                    mapObject.Id,
                    "heroId",
                    diagnostics)
                && registry.FindHero(mapObject.HeroId) is { } hero)
            {
                capacity = hero.Stats.Leadership;

                if (!string.Equals(hero.FactionId, mapObject.FactionId, StringComparison.Ordinal))
                {
                    Error(
                        diagnostics,
                        "CONTENT_PATROL_HERO_FACTION",
                        $"Герой «{hero.Id}» принадлежит фракции «{hero.FactionId}», а патруль — «{mapObject.FactionId}».",
                        source,
                        mapObject.Id,
                        "heroId");
                }
            }
        }

        if (mapObject.Army.Count == 0)
        {
            Warning(
                diagnostics,
                "CONTENT_PATROL_ARMY_EMPTY",
                "Армия патруля пуста.",
                source,
                mapObject.Id,
                "army");

            return;
        }

        if (mapObject.Army.Count > ContentSchema.MaxArmySlots)
        {
            Error(
                diagnostics,
                "CONTENT_PATROL_ARMY_SLOTS",
                $"Слотов армии {mapObject.Army.Count}, максимум {ContentSchema.MaxArmySlots}.",
                source,
                mapObject.Id,
                "army");
        }

        HashSet<string> seenUnits = new(StringComparer.Ordinal);
        long leadership = 0;

        for (int index = 0; index < mapObject.Army.Count; index++)
        {
            HeroArmySlotDto slot = mapObject.Army[index];
            string field = $"army[{index}]";

            if (slot.Count <= 0)
            {
                Error(
                    diagnostics,
                    "CONTENT_PATROL_ARMY_COUNT",
                    $"Количество юнитов должно быть положительным, указано {slot.Count}.",
                    source,
                    mapObject.Id,
                    $"{field}.count");
            }

            if (!seenUnits.Add(slot.UnitId))
            {
                Error(
                    diagnostics,
                    "CONTENT_PATROL_ARMY_DUPLICATE",
                    $"Юнит «{slot.UnitId}» указан в армии более одного раза.",
                    source,
                    mapObject.Id,
                    $"{field}.unitId");
            }

            if (!RequireReference(
                    registry.Units,
                    slot.UnitId,
                    "CONTENT_REF_UNIT_MISSING",
                    "юнит",
                    source,
                    mapObject.Id,
                    $"{field}.unitId",
                    diagnostics))
            {
                continue;
            }

            UnitDto unit = registry.FindUnit(slot.UnitId)!;

            if (!string.Equals(unit.FactionId, mapObject.FactionId, StringComparison.Ordinal))
            {
                Warning(
                    diagnostics,
                    "CONTENT_PATROL_ARMY_FACTION",
                    $"Юнит «{unit.Id}» принадлежит фракции «{unit.FactionId}», а патруль — «{mapObject.FactionId}».",
                    source,
                    mapObject.Id,
                    $"{field}.unitId");
            }

            if (slot.Count > 0)
            {
                leadership = checked(leadership + ((long)unit.LeadershipCost * slot.Count));
            }
        }

        if (leadership > capacity)
        {
            Error(
                diagnostics,
                "CONTENT_PATROL_ARMY_LEADERSHIP",
                $"Армия патруля требует {leadership} лидерства, доступно {capacity}.",
                source,
                mapObject.Id,
                "army");
        }
    }

    private static void ValidateResources(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        if (registry.Resources.Count != ContentSchema.ResourceIds.Count)
        {
            Error(
                diagnostics,
                "CONTENT_RESOURCE_COUNT",
                $"Ресурсов должно быть ровно {ContentSchema.ResourceIds.Count}, указано {registry.Resources.Count}.");
        }

        foreach (string resourceId in ContentSchema.ResourceIds)
        {
            if (!registry.Resources.ContainsKey(resourceId))
            {
                Error(
                    diagnostics,
                    "CONTENT_RESOURCE_MISSING",
                    $"Отсутствует обязательный ресурс «{resourceId}».",
                    entityId: resourceId,
                    field: "resources");
            }
        }

        foreach (ContentEntry<ResourceDefinitionDto> entry in registry.Resources.Values)
        {
            ResourceDefinitionDto resource = entry.Value;

            if (!ContentSchema.IsResourceId(resource.Id))
            {
                Error(
                    diagnostics,
                    "CONTENT_RESOURCE_UNKNOWN",
                    $"Неизвестный ресурс «{resource.Id}»; допустимы {Join(ContentSchema.ResourceIds)}.",
                    entry.SourceFile,
                    resource.Id,
                    "id");

                continue;
            }

            string expectedRole = resource.Id == "gold" ? "currency" : "material";

            if (!string.Equals(resource.Role, expectedRole, StringComparison.Ordinal))
            {
                Error(
                    diagnostics,
                    "CONTENT_RESOURCE_ROLE",
                    $"Роль ресурса «{resource.Id}» должна быть «{expectedRole}», указано «{resource.Role}».",
                    entry.SourceFile,
                    resource.Id,
                    "role");
            }
        }
    }

    private static void ValidateBattleRules(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        if (registry.BattleRules is not { } entry)
        {
            Error(diagnostics, "CONTENT_RULE_MISSING", "Отсутствуют правила боя rules/battle.json.");
            return;
        }

        BattleRulesDto rules = entry.Value;
        string source = entry.SourceFile;

        RequireAtLeast(rules.FieldWidth, 1, "fieldWidth", rules.Id, source, diagnostics, "CONTENT_BATTLE_FIELD_WIDTH");
        RequireAtLeast(rules.FieldHeight, 1, "fieldHeight", rules.Id, source, diagnostics, "CONTENT_BATTLE_FIELD_HEIGHT");
        RequireAtLeast(rules.MaxArmySlots, 1, "maxArmySlots", rules.Id, source, diagnostics, "CONTENT_BATTLE_MAX_SLOTS");

        if (rules.ArmySlots < 1 || rules.ArmySlots > rules.MaxArmySlots)
        {
            Error(
                diagnostics,
                "CONTENT_BATTLE_SLOTS",
                $"armySlots должен быть 1…{rules.MaxArmySlots}, указано {rules.ArmySlots}.",
                source,
                rules.Id,
                "armySlots");
        }

        RequireRange(rules.DefendDefenseBonusPercent, 0, 100, "defendDefenseBonusPercent", rules.Id, source, diagnostics, "CONTENT_BATTLE_DEFEND_BONUS");
        RequireRange(rules.DefendResistBonusPercent, 0, 100, "defendResistBonusPercent", rules.Id, source, diagnostics, "CONTENT_BATTLE_DEFEND_RESIST");
        RequireAtLeast(rules.DefendInitiativePenalty, 0, "defendInitiativePenalty", rules.Id, source, diagnostics, "CONTENT_BATTLE_DEFEND_INITIATIVE");
        RequireAtLeast(rules.RetaliationPerRound, 0, "retaliationPerRound", rules.Id, source, diagnostics, "CONTENT_BATTLE_RETALIATION");
        RequireAtLeast(rules.AttackDefenseModifierPerPoint, 0, "attackDefenseModifierPerPoint", rules.Id, source, diagnostics, "CONTENT_BATTLE_MODIFIER");

        if (rules.AttackDefenseModifierMin > rules.AttackDefenseModifierMax)
        {
            Error(
                diagnostics,
                "CONTENT_BATTLE_MODIFIER_RANGE",
                $"attackDefenseModifierMin {rules.AttackDefenseModifierMin} больше attackDefenseModifierMax {rules.AttackDefenseModifierMax}.",
                source,
                rules.Id,
                "attackDefenseModifierMin");
        }

        RequireRange(rules.CritChanceCapPercent, 0, 100, "critChanceCapPercent", rules.Id, source, diagnostics, "CONTENT_BATTLE_CRIT_CHANCE");
        RequireAtLeast(rules.CritDamageCapPercent, 100, "critDamageCapPercent", rules.Id, source, diagnostics, "CONTENT_BATTLE_CRIT_DAMAGE");

        HashSet<string> damageTypes = new(StringComparer.Ordinal);

        foreach (string damageType in rules.DamageTypes)
        {
            if (!ContentSchema.DamageTypes.Contains(damageType))
            {
                Error(
                    diagnostics,
                    "CONTENT_BATTLE_DAMAGE_TYPE",
                    $"Неизвестный тип урона «{damageType}»; допустимы {Join(ContentSchema.DamageTypes)}.",
                    source,
                    rules.Id,
                    "damageTypes");
            }
            else if (!damageTypes.Add(damageType))
            {
                Error(
                    diagnostics,
                    "CONTENT_BATTLE_DAMAGE_TYPE_DUPLICATE",
                    $"Тип урона «{damageType}» указан более одного раза.",
                    source,
                    rules.Id,
                    "damageTypes");
            }
        }

        if (!damageTypes.SetEquals(ContentSchema.DamageTypes))
        {
            Error(
                diagnostics,
                "CONTENT_BATTLE_DAMAGE_TYPE_SET",
                $"Список damageTypes должен содержать все пять типов: {Join(ContentSchema.DamageTypes)}.",
                source,
                rules.Id,
                "damageTypes");
        }
    }

    private static void ValidateMoraleRules(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        if (registry.MoraleRules is not { } entry)
        {
            Error(diagnostics, "CONTENT_RULE_MISSING", "Отсутствуют правила морали rules/morale.json.");
            return;
        }

        MoraleRulesDto rules = entry.Value;
        string source = entry.SourceFile;

        if (rules.MinLevel > 0 || rules.MaxLevel < 0 || rules.MinLevel >= rules.MaxLevel)
        {
            Error(
                diagnostics,
                "CONTENT_MORALE_RANGE",
                $"Диапазон морали должен включать 0 и быть непустым: {rules.MinLevel}…{rules.MaxLevel}.",
                source,
                rules.Id,
                "minLevel");
        }

        RequireRange(rules.ExtraActionChanceCapPercent, 0, 100, "extraActionChanceCapPercent", rules.Id, source, diagnostics, "CONTENT_MORALE_CHANCE_CAP");
        RequireRange(rules.StatModifierCapPercent, 0, 100, "statModifierCapPercent", rules.Id, source, diagnostics, "CONTENT_MORALE_STAT_CAP");
        RequireAtLeast(rules.MaxExtraActionsPerUnitPerRound, 0, "maxExtraActionsPerUnitPerRound", rules.Id, source, diagnostics, "CONTENT_MORALE_EXTRA_ACTIONS");
        ValidateTags(rules.ImmuneTags, "immuneTags", rules.Id, source, diagnostics);

        Dictionary<int, MoraleLevelDto> byLevel = new();

        foreach (MoraleLevelDto level in rules.Levels)
        {
            if (level.Level < rules.MinLevel || level.Level > rules.MaxLevel)
            {
                Error(
                    diagnostics,
                    "CONTENT_MORALE_LEVEL_RANGE",
                    $"Уровень морали {level.Level} вне диапазона {rules.MinLevel}…{rules.MaxLevel}.",
                    source,
                    rules.Id,
                    "levels");
            }

            if (byLevel.ContainsKey(level.Level))
            {
                Error(
                    diagnostics,
                    "CONTENT_MORALE_LEVEL_DUPLICATE",
                    $"Уровень морали {level.Level} объявлен более одного раза.",
                    source,
                    rules.Id,
                    "levels");
            }
            else
            {
                byLevel[level.Level] = level;
            }

            RequireRange(level.ChancePercent, 0, rules.ExtraActionChanceCapPercent, "levels.chancePercent", rules.Id, source, diagnostics, "CONTENT_MORALE_LEVEL_CHANCE");
            RequireRange(level.StatModifierPercent, -rules.StatModifierCapPercent, rules.StatModifierCapPercent, "levels.statModifierPercent", rules.Id, source, diagnostics, "CONTENT_MORALE_LEVEL_STAT");
        }

        for (int level = rules.MinLevel; level <= rules.MaxLevel; level++)
        {
            if (!byLevel.ContainsKey(level))
            {
                Error(
                    diagnostics,
                    "CONTENT_MORALE_LEVEL_MISSING",
                    $"Отсутствует уровень морали {level}.",
                    source,
                    rules.Id,
                    "levels");
            }
        }
    }

    private static void ValidateEconomyRules(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        if (registry.EconomyRules is not { } entry)
        {
            Error(diagnostics, "CONTENT_RULE_MISSING", "Отсутствуют правила экономики rules/economy.json.");
            return;
        }

        EconomyRulesDto rules = entry.Value;
        string source = entry.SourceFile;

        ValidateResourceMap(rules.BaseSettlementIncomePerDay, "baseSettlementIncomePerDay", rules.Id, source, diagnostics);
        RequireAtLeast(rules.GrowthCapDays, 1, "growthCapDays", rules.Id, source, diagnostics, "CONTENT_ECONOMY_CAP_DAYS");
        RequireAtLeast(rules.CaptureResetsLevel, 1, "captureResetsLevel", rules.Id, source, diagnostics, "CONTENT_ECONOMY_CAPTURE_LEVEL");

        foreach (KeyValuePair<string, int> pair in rules.GrowthPerDayMilliByTier)
        {
            if (!int.TryParse(pair.Key, out int tier) || tier < 1 || tier > 5)
            {
                Error(
                    diagnostics,
                    "CONTENT_ECONOMY_TIER",
                    $"Ключ growthPerDayMilliByTier «{pair.Key}» должен быть тиром 1…5.",
                    source,
                    rules.Id,
                    "growthPerDayMilliByTier");
            }

            if (pair.Value <= 0)
            {
                Error(
                    diagnostics,
                    "CONTENT_ECONOMY_GROWTH",
                    $"Суточный прирост для тира {pair.Key} должен быть положительным, указано {pair.Value}.",
                    source,
                    rules.Id,
                    $"growthPerDayMilliByTier.{pair.Key}");
            }
        }

        for (int tier = 1; tier <= 5; tier++)
        {
            if (!rules.GrowthPerDayMilliByTier.ContainsKey(tier.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            {
                Error(
                    diagnostics,
                    "CONTENT_ECONOMY_TIER_MISSING",
                    $"Отсутствует прирост для тира {tier}.",
                    source,
                    rules.Id,
                    "growthPerDayMilliByTier");
            }
        }

        ValidateNonEmptyUnique(rules.DailyTickOrder, "dailyTickOrder", rules.Id, source, diagnostics, "CONTENT_ECONOMY_TICK_ORDER");
    }

    private static void ValidateEscapeRules(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        if (registry.EscapeRules is not { } entry)
        {
            Error(diagnostics, "CONTENT_RULE_MISSING", "Отсутствуют правила побега rules/escape.json.");
            return;
        }

        EscapeRulesDto rules = entry.Value;
        string source = entry.SourceFile;

        RequireRange(rules.BaseChancePercent, 0, 100, "baseChancePercent", rules.Id, source, diagnostics, "CONTENT_ESCAPE_BASE");
        RequireRange(rules.MinChancePercent, 0, 100, "minChancePercent", rules.Id, source, diagnostics, "CONTENT_ESCAPE_MIN");
        RequireRange(rules.MaxChancePercent, 0, 100, "maxChancePercent", rules.Id, source, diagnostics, "CONTENT_ESCAPE_MAX");

        if (rules.MinChancePercent > rules.MaxChancePercent)
        {
            Error(
                diagnostics,
                "CONTENT_ESCAPE_CHANCE_RANGE",
                $"minChancePercent {rules.MinChancePercent} больше maxChancePercent {rules.MaxChancePercent}.",
                source,
                rules.Id,
                "minChancePercent");
        }

        RequireAtLeast(rules.HeroLogisticsMultiplier, 0, "heroLogisticsMultiplier", rules.Id, source, diagnostics, "CONTENT_ESCAPE_HERO_LOGISTICS");
        RequireAtLeast(rules.EnemyLogisticsMultiplier, 0, "enemyLogisticsMultiplier", rules.Id, source, diagnostics, "CONTENT_ESCAPE_ENEMY_LOGISTICS");
        RequireAtLeast(rules.ArtifactsLostMin, 0, "artifactsLostMin", rules.Id, source, diagnostics, "CONTENT_ESCAPE_ARTIFACTS_MIN");
        RequireAtLeast(rules.ArtifactsLostMax, 0, "artifactsLostMax", rules.Id, source, diagnostics, "CONTENT_ESCAPE_ARTIFACTS_MAX");

        if (rules.ArtifactsLostMin > rules.ArtifactsLostMax)
        {
            Error(
                diagnostics,
                "CONTENT_ESCAPE_ARTIFACT_RANGE",
                $"artifactsLostMin {rules.ArtifactsLostMin} больше artifactsLostMax {rules.ArtifactsLostMax}.",
                source,
                rules.Id,
                "artifactsLostMin");
        }

        RequireAtLeast(rules.CaptivityDays, 1, "captivityDays", rules.Id, source, diagnostics, "CONTENT_ESCAPE_CAPTIVITY");
        RequireRange(rules.HerolessArmyLossPercent, 0, 100, "herolessArmyLossPercent", rules.Id, source, diagnostics, "CONTENT_ESCAPE_HEROLESS");
    }

    private static void ValidateRansomRules(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        if (registry.RansomRules is not { } entry)
        {
            Error(diagnostics, "CONTENT_RULE_MISSING", "Отсутствуют правила выкупа rules/ransom.json.");
            return;
        }

        RansomRulesDto rules = entry.Value;
        string source = entry.SourceFile;

        RequireAtLeast(rules.UnitPriceMultiplierPercent, 0, "unitPriceMultiplierPercent", rules.Id, source, diagnostics, "CONTENT_RANSOM_UNIT_PRICE");
        RequireAtLeast(rules.HeroLevelCost, 0, "heroLevelCost", rules.Id, source, diagnostics, "CONTENT_RANSOM_HERO_LEVEL");
        RequireAtLeast(rules.ArtifactCost, 0, "artifactCost", rules.Id, source, diagnostics, "CONTENT_RANSOM_ARTIFACT");
        RequireAtLeast(rules.MovePointsAfter, 0, "movePointsAfter", rules.Id, source, diagnostics, "CONTENT_RANSOM_MOVE_POINTS");
    }

    private static void ValidateVeterancyRules(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        if (registry.VeterancyRules is not { } entry)
        {
            Error(diagnostics, "CONTENT_RULE_MISSING", "Отсутствуют правила ветеранства rules/veterancy.json.");
            return;
        }

        VeterancyRulesDto rules = entry.Value;
        string source = entry.SourceFile;

        if (rules.Thresholds.Count < 2)
        {
            Error(
                diagnostics,
                "CONTENT_VETERANCY_THRESHOLDS",
                "Порогов ветеранства должно быть не меньше двух.",
                source,
                rules.Id,
                "thresholds");
        }

        int previous = -1;

        for (int index = 0; index < rules.Thresholds.Count; index++)
        {
            int threshold = rules.Thresholds[index];

            if (threshold < 0)
            {
                Error(
                    diagnostics,
                    "CONTENT_VETERANCY_THRESHOLD_NEGATIVE",
                    $"Порог {index} не может быть отрицательным: {threshold}.",
                    source,
                    rules.Id,
                    "thresholds");
            }

            if (threshold <= previous)
            {
                Error(
                    diagnostics,
                    "CONTENT_VETERANCY_THRESHOLDS_ORDER",
                    $"Пороги должны строго возрастать: {previous} → {threshold}.",
                    source,
                    rules.Id,
                    "thresholds");
            }

            previous = threshold;
        }

        if (rules.Thresholds.Count > 0 && rules.Thresholds[0] != 0)
        {
            Error(
                diagnostics,
                "CONTENT_VETERANCY_FIRST_THRESHOLD",
                "Первый порог ветеранства обязан быть 0.",
                source,
                rules.Id,
                "thresholds");
        }

        if (rules.RankNameKeys.Count != rules.Thresholds.Count)
        {
            Error(
                diagnostics,
                "CONTENT_VETERANCY_RANK_KEYS",
                $"Ключей названий рангов {rules.RankNameKeys.Count}, порогов {rules.Thresholds.Count}.",
                source,
                rules.Id,
                "rankNameKeys");
        }

        for (int index = 0; index < rules.RankNameKeys.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(rules.RankNameKeys[index]))
            {
                Error(
                    diagnostics,
                    "CONTENT_VETERANCY_RANK_KEY_EMPTY",
                    $"Пустой ключ названия ранга {index}.",
                    source,
                    rules.Id,
                    "rankNameKeys");
            }
        }

        RequireAtLeast(rules.PerRankAboveFirst.Attack, 0, "perRankAboveFirst.attack", rules.Id, source, diagnostics, "CONTENT_VETERANCY_ATTACK");
        RequireAtLeast(rules.PerRankAboveFirst.Defense, 0, "perRankAboveFirst.defense", rules.Id, source, diagnostics, "CONTENT_VETERANCY_DEFENSE");
        RequireAtLeast(rules.PerRankAboveFirst.CritChancePercent, 0, "perRankAboveFirst.critChancePercent", rules.Id, source, diagnostics, "CONTENT_VETERANCY_CRIT_CHANCE");
        RequireAtLeast(rules.PerRankAboveFirst.CritDamagePercent, 0, "perRankAboveFirst.critDamagePercent", rules.Id, source, diagnostics, "CONTENT_VETERANCY_CRIT_DAMAGE");
    }

    private static void ValidateProgressionRules(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        if (registry.ProgressionRules is not { } entry)
        {
            Error(diagnostics, "CONTENT_RULE_MISSING", "Отсутствуют правила прогрессии rules/progression.json.");
            return;
        }

        ProgressionRulesDto rules = entry.Value;
        string source = entry.SourceFile;

        if (!string.Equals(rules.XpToNextFormula.Type, "quadratic", StringComparison.Ordinal))
        {
            Error(
                diagnostics,
                "CONTENT_PROGRESSION_FORMULA_TYPE",
                $"Тип формулы опыта «{rules.XpToNextFormula.Type}» не поддерживается; ожидается quadratic.",
                source,
                rules.Id,
                "xpToNextFormula.type");
        }

        RequireAtLeast(rules.XpToNextFormula.Coefficient, 1, "xpToNextFormula.coefficient", rules.Id, source, diagnostics, "CONTENT_PROGRESSION_COEFFICIENT");

        if (string.IsNullOrWhiteSpace(rules.XpToNextFormula.Expression))
        {
            Error(
                diagnostics,
                "CONTENT_PROGRESSION_EXPRESSION",
                "Выражение формулы опыта не может быть пустым.",
                source,
                rules.Id,
                "xpToNextFormula.expression");
        }

        RequireAtLeast(rules.SkillPointsPerLevel, 0, "skillPointsPerLevel", rules.Id, source, diagnostics, "CONTENT_PROGRESSION_SKILL_POINTS");
        RequireAtLeast(rules.ScenarioSoftCap, 0, "scenarioSoftCap", rules.Id, source, diagnostics, "CONTENT_PROGRESSION_SOFT_CAP");
    }

    private static void ValidateBiomes(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        if (registry.BiomesRules is not { } entry)
        {
            Error(diagnostics, "CONTENT_RULE_MISSING", "Отсутствуют правила биомов rules/biomes.json.");
            return;
        }

        BiomesRulesDto rules = entry.Value;
        string source = entry.SourceFile;

        if (rules.Biomes.Count == 0)
        {
            Error(diagnostics, "CONTENT_BIOMES_EMPTY", "Список биомов пуст.", source, rules.Id, "biomes");
            return;
        }

        HashSet<string> ids = new(StringComparer.Ordinal);

        foreach (BiomeDto biome in rules.Biomes)
        {
            RequireEntityId(biome.Id, source, diagnostics);

            if (!string.IsNullOrWhiteSpace(biome.Id) && !ids.Add(biome.Id))
            {
                Error(
                    diagnostics,
                    "CONTENT_BIOME_DUPLICATE",
                    $"Биом «{biome.Id}» объявлен более одного раза.",
                    source,
                    biome.Id,
                    "biomes");
            }

            RequireKey(biome.NameKey, "nameKey", biome.Id, source, diagnostics, "CONTENT_BIOME_NAME_KEY");
            RequireKey(biome.TileId, "tileId", biome.Id, source, diagnostics, "CONTENT_BIOME_TILE");

            RequireAtLeast(
                biome.MovementCostPerHex,
                0,
                "movementCostPerHex",
                biome.Id,
                source,
                diagnostics,
                "CONTENT_BIOME_MOVEMENT_COST");
            RequireAtLeast(
                biome.VisibilityRange,
                0,
                "visibilityRange",
                biome.Id,
                source,
                diagnostics,
                "CONTENT_BIOME_VISIBILITY");

            // ADR-034: по местности — не менее 2 очков движения, по дороге — ровно 1.
            if (biome.Passable && biome.Road && biome.MovementCostPerHex != 1)
            {
                Error(
                    diagnostics,
                    "CONTENT_BIOME_ROAD_COST",
                    "Дорога обязана иметь movementCostPerHex, равный 1.",
                    source,
                    biome.Id,
                    "movementCostPerHex");
            }
            else if (biome.Passable && !biome.Road && biome.MovementCostPerHex < 2)
            {
                Error(
                    diagnostics,
                    "CONTENT_BIOME_PASSABLE_COST",
                    "Проходимый биом (не дорога) обязан иметь movementCostPerHex не меньше 2.",
                    source,
                    biome.Id,
                    "movementCostPerHex");
            }

            if (biome.FlyerPassable && biome.FlyerMovementCost < 2)
            {
                Error(
                    diagnostics,
                    "CONTENT_BIOME_FLYER_COST",
                    "Проходимый для летающих биом обязан иметь flyerMovementCost не меньше 2.",
                    source,
                    biome.Id,
                    "flyerMovementCost");
            }
        }
    }

    private static void ValidateMapGenerationRules(
        ContentRegistry registry,
        ContentDiagnosticCollection diagnostics)
    {
        if (registry.MapGenerationRules is not { } entry)
        {
            Error(
                diagnostics,
                "CONTENT_RULE_MISSING",
                "Отсутствуют правила генерации карты rules/map_generation.json.");
            return;
        }

        MapGenerationRulesDto rules = entry.Value;
        string source = entry.SourceFile;

        RequireAtLeast(rules.GridWidth, 1, "gridWidth", rules.Id, source, diagnostics, "CONTENT_MAP_WIDTH");
        RequireAtLeast(rules.GridHeight, 1, "gridHeight", rules.Id, source, diagnostics, "CONTENT_MAP_HEIGHT");

        if (!ContentSchema.OffsetTypes.Contains(rules.OffsetType))
        {
            Error(
                diagnostics,
                "CONTENT_MAP_OFFSET_TYPE",
                $"Неизвестный offsetType «{rules.OffsetType}»; допустимы {Join(ContentSchema.OffsetTypes)}.",
                source,
                rules.Id,
                "offsetType");
        }

        if (!RequireReference(
                registry.Biomes,
                rules.DefaultBiome,
                "CONTENT_REF_BIOME_MISSING",
                "биом",
                source,
                rules.Id,
                "defaultBiome",
                diagnostics))
        {
            // Ошибка уже зафиксирована.
        }

        int distributionSum = 0;

        foreach (KeyValuePair<string, int> pair in rules.BiomeDistribution)
        {
            if (!RequireReference(
                    registry.Biomes,
                    pair.Key,
                    "CONTENT_REF_BIOME_MISSING",
                    "биом",
                    source,
                    rules.Id,
                    $"biomeDistribution.{pair.Key}",
                    diagnostics))
            {
                continue;
            }

            if (pair.Value < 0)
            {
                Error(
                    diagnostics,
                    "CONTENT_MAP_DISTRIBUTION_VALUE",
                    $"Вес биома «{pair.Key}» не может быть отрицательным: {pair.Value}.",
                    source,
                    rules.Id,
                    $"biomeDistribution.{pair.Key}");
            }

            distributionSum = checked(distributionSum + pair.Value);
        }

        if (rules.BiomeDistribution.Count > 0 && distributionSum != 100)
        {
            Error(
                diagnostics,
                "CONTENT_MAP_DISTRIBUTION_SUM",
                $"Сумма biomeDistribution должна быть 100, указано {distributionSum}.",
                source,
                rules.Id,
                "biomeDistribution");
        }

        RequireAtLeast(
            rules.PlayerStartPositions.MinDistanceBetweenPlayers,
            0,
            "playerStartPositions.minDistanceBetweenPlayers",
            rules.Id,
            source,
            diagnostics,
            "CONTENT_MAP_START_DISTANCE");
        RequireAtLeast(
            rules.PlayerStartPositions.DistanceFromBorder,
            0,
            "playerStartPositions.distanceFromBorder",
            rules.Id,
            source,
            diagnostics,
            "CONTENT_MAP_BORDER_DISTANCE");

        RequireAtLeast(rules.ObjectCounts.Mines, 0, "objectCounts.mines", rules.Id, source, diagnostics, "CONTENT_MAP_OBJECT_COUNT");
        RequireAtLeast(rules.ObjectCounts.Dwellings, 0, "objectCounts.dwellings", rules.Id, source, diagnostics, "CONTENT_MAP_OBJECT_COUNT");
        RequireAtLeast(rules.ObjectCounts.EchoCaches, 0, "objectCounts.echoCaches", rules.Id, source, diagnostics, "CONTENT_MAP_OBJECT_COUNT");
        RequireAtLeast(rules.ObjectCounts.Patrols, 0, "objectCounts.patrols", rules.Id, source, diagnostics, "CONTENT_MAP_OBJECT_COUNT");

        RequireAtLeast(rules.ArmyMovementPointsPerDay, 1, "armyMovementPointsPerDay", rules.Id, source, diagnostics, "CONTENT_MAP_ARMY_MP");
        RequireAtLeast(rules.FogVisibilityRadius, 0, "fogVisibilityRadius", rules.Id, source, diagnostics, "CONTENT_MAP_FOG_VISIBILITY");
        RequireAtLeast(rules.FogExploredRadius, 0, "fogExploredRadius", rules.Id, source, diagnostics, "CONTENT_MAP_FOG_EXPLORED");
    }

    private static void ValidateTestMaps(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        foreach (ContentEntry<TestMapDto> entry in registry.TestMaps.Values)
        {
            TestMapDto map = entry.Value;
            string source = entry.SourceFile;

            RequireEntityId(map.Id, source, diagnostics);
            RequireKey(map.NameKey, "nameKey", map.Id, source, diagnostics, "CONTENT_TEST_MAP_NAME_KEY");
            RequireAtLeast(map.Width, 1, "width", map.Id, source, diagnostics, "CONTENT_TEST_MAP_WIDTH");
            RequireAtLeast(map.Height, 1, "height", map.Id, source, diagnostics, "CONTENT_TEST_MAP_HEIGHT");

            if (!ContentSchema.OffsetTypes.Contains(map.OffsetType))
            {
                Error(
                    diagnostics,
                    "CONTENT_TEST_MAP_OFFSET_TYPE",
                    $"Неизвестный offsetType «{map.OffsetType}»; допустимы {Join(ContentSchema.OffsetTypes)}.",
                    source,
                    map.Id,
                    "offsetType");
            }

            if (map.Legend.Count == 0)
            {
                Error(diagnostics, "CONTENT_TEST_MAP_LEGEND_EMPTY", "Отсутствует legend.", source, map.Id, "legend");
            }

            foreach (KeyValuePair<string, string> pair in map.Legend)
            {
                if (pair.Key.Length != 1)
                {
                    Error(
                        diagnostics,
                        "CONTENT_TEST_MAP_LEGEND_KEY",
                        $"Ключ legend «{pair.Key}» должен быть одним символом.",
                        source,
                        map.Id,
                        $"legend.{pair.Key}");
                }

                RequireReference(
                    registry.Biomes,
                    pair.Value,
                    "CONTENT_REF_BIOME_MISSING",
                    "биом",
                    source,
                    map.Id,
                    $"legend.{pair.Key}",
                    diagnostics);
            }

            if (map.Layout.Count != map.Height)
            {
                Error(
                    diagnostics,
                    "CONTENT_TEST_MAP_LAYOUT_ROWS",
                    $"Строк layout {map.Layout.Count}, ожидается height {map.Height}.",
                    source,
                    map.Id,
                    "layout");
            }

            for (int row = 0; row < map.Layout.Count; row++)
            {
                string line = map.Layout[row];

                if (line.Length != map.Width)
                {
                    Error(
                        diagnostics,
                        "CONTENT_TEST_MAP_LAYOUT_WIDTH",
                        $"Длина строки {row} равна {line.Length}, ожидается width {map.Width}.",
                        source,
                        map.Id,
                        $"layout[{row}]");
                }

                foreach (char symbol in line)
                {
                    if (!map.Legend.ContainsKey(symbol.ToString()))
                    {
                        Error(
                            diagnostics,
                            "CONTENT_TEST_MAP_LAYOUT_SYMBOL",
                            $"Символ «{symbol}» в строке {row} отсутствует в legend.",
                            source,
                            map.Id,
                            $"layout[{row}]");

                        break;
                    }
                }
            }

            foreach (TestMapObjectDto mapObject in map.Objects)
            {
                if (!IsInside(mapObject.Col, mapObject.Row, map.Width, map.Height))
                {
                    Error(
                        diagnostics,
                        "CONTENT_TEST_MAP_OBJECT_BOUNDS",
                        $"Объект «{mapObject.ObjectId}» вне карты: ({mapObject.Col}, {mapObject.Row}).",
                        source,
                        map.Id,
                        "objects");
                }

                RequireReference(
                    registry.MapObjects,
                    mapObject.ObjectId,
                    "CONTENT_REF_MAP_OBJECT_MISSING",
                    "объект карты",
                    source,
                    map.Id,
                    "objects.objectId",
                    diagnostics);
            }

            if (map.StartPositions is { } starts)
            {
                ValidateTestMapStart(starts.Player, registry, map, source, "startPositions.player", diagnostics);
                ValidateTestMapStart(starts.Enemy, registry, map, source, "startPositions.enemy", diagnostics);
                ValidateTestMapStart(starts.Neutral, registry, map, source, "startPositions.neutral", diagnostics);
            }

            HashSet<string> caseIds = new(StringComparer.Ordinal);

            foreach (TestMapCaseDto testCase in map.TestCases)
            {
                if (string.IsNullOrWhiteSpace(testCase.Id))
                {
                    Error(diagnostics, "CONTENT_TEST_MAP_CASE_ID", "У тестового утверждения отсутствует id.", source, map.Id, "testCases");
                }
                else if (!caseIds.Add(testCase.Id))
                {
                    Error(
                        diagnostics,
                        "CONTENT_TEST_MAP_CASE_DUPLICATE",
                        $"Утверждение «{testCase.Id}» объявлено более одного раза.",
                        source,
                        map.Id,
                        "testCases");
                }

                if (string.IsNullOrWhiteSpace(testCase.Type))
                {
                    Error(
                        diagnostics,
                        "CONTENT_TEST_MAP_CASE_TYPE",
                        $"У утверждения «{testCase.Id}» отсутствует type.",
                        source,
                        map.Id,
                        "testCases");
                }
            }
        }
    }

    private static void ValidateTestMapStart(
        TestMapStartDto? start,
        ContentRegistry registry,
        TestMapDto map,
        string source,
        string field,
        ContentDiagnosticCollection diagnostics)
    {
        if (start is null)
        {
            return;
        }

        if (!IsInside(start.Col, start.Row, map.Width, map.Height))
        {
            Error(
                diagnostics,
                "CONTENT_TEST_MAP_START_BOUNDS",
                $"Старт вне карты: ({start.Col}, {start.Row}).",
                source,
                map.Id,
                field);
        }

        RequireReference(
            registry.Factions,
            start.FactionId,
            "CONTENT_REF_FACTION_MISSING",
            "фракция",
            source,
            map.Id,
            $"{field}.factionId",
            diagnostics);

        if (!string.IsNullOrWhiteSpace(start.HeroId))
        {
            RequireReference(
                registry.Heroes,
                start.HeroId,
                "CONTENT_REF_HERO_MISSING",
                "герой",
                source,
                map.Id,
                $"{field}.heroId",
                diagnostics);
        }
    }

    private static bool IsInside(int column, int row, int width, int height)
    {
        return column >= 0 && row >= 0 && column < width && row < height;
    }

    private static void RequireKey(
        string? value,
        string field,
        string? entityId,
        string source,
        ContentDiagnosticCollection diagnostics,
        string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Error(
                diagnostics,
                code,
                $"Обязательное поле «{field}» не заполнено.",
                source,
                entityId,
                field);
        }
    }

    private static void ValidateLocalization(ContentRegistry registry, ContentDiagnosticCollection diagnostics)
    {
        if (registry.Localization is not { } localization)
        {
            Error(diagnostics, "LOC_MANIFEST_MISSING", "Отсутствует манифест локализации localization/languages.json.");
            return;
        }

        LocalizationManifest manifest = localization.Manifest;

        if (manifest.RequiredLanguages.Count == 0)
        {
            Error(diagnostics, "LOC_REQUIRED_LANGUAGES_EMPTY", "Список обязательных языков пуст.");
        }

        foreach (string required in new[] { "ru", "en" })
        {
            if (!manifest.RequiredLanguages.Contains(required, StringComparer.Ordinal))
            {
                Error(
                    diagnostics,
                    "LOC_REQUIRED_LANGUAGE_MISSING",
                    $"Обязательный язык «{required}» отсутствует в requiredLanguages.",
                    entityId: required,
                    field: "requiredLanguages");
            }
        }

        if (manifest.Find(manifest.DefaultLanguage) is null)
        {
            Error(
                diagnostics,
                "LOC_DEFAULT_LANGUAGE_UNKNOWN",
                $"Язык по умолчанию «{manifest.DefaultLanguage}» не зарегистрирован.",
                entityId: manifest.DefaultLanguage,
                field: "defaultLanguage");
        }

        if (manifest.Find(manifest.FallbackLanguage) is null)
        {
            Error(
                diagnostics,
                "LOC_FALLBACK_LANGUAGE_UNKNOWN",
                $"Глобальный язык фолбэка «{manifest.FallbackLanguage}» не зарегистрирован.",
                entityId: manifest.FallbackLanguage,
                field: "fallbackLanguage");
        }

        HashSet<string> registered = new(StringComparer.Ordinal);

        foreach (LanguageDefinition language in manifest.Languages)
        {
            if (string.IsNullOrWhiteSpace(language.Code))
            {
                continue;
            }

            if (!registered.Add(language.Code))
            {
                Error(
                    diagnostics,
                    "LOC_LANGUAGE_DUPLICATE",
                    $"Язык «{language.Code}» зарегистрирован более одного раза.",
                    entityId: language.Code,
                    field: "languages");
            }

            if (language.Fallback is { Length: > 0 } fallback && manifest.Find(fallback) is null)
            {
                Error(
                    diagnostics,
                    "LOC_LANGUAGE_FALLBACK_UNKNOWN",
                    $"Фолбэк «{fallback}» языка «{language.Code}» не зарегистрирован.",
                    entityId: language.Code,
                    field: "fallback");
            }

            if (ContentSchema.FontReviewLanguages.Contains(language.Code) && !language.RequiresFontReview)
            {
                Error(
                    diagnostics,
                    "LOC_FONT_REVIEW_REQUIRED",
                    $"Для языка «{language.Code}» обязателен requiresFontReview=true.",
                    entityId: language.Code,
                    field: "requiresFontReview");
            }

            if (language.VisibleInSettings && language.Status != LocalizationStatus.Complete)
            {
                Warning(
                    diagnostics,
                    "LOC_SETTINGS_INCOMPLETE_VISIBLE",
                    $"Язык «{language.Code}» виден в настройках, но имеет статус {LocalizationStatusParser.ToToken(language.Status)}.",
                    entityId: language.Code,
                    field: "visibleInSettings");
            }

            if (localization.Find(language.Code) is null)
            {
                Error(
                    diagnostics,
                    "LOC_TABLE_MISSING",
                    $"Для зарегистрированного языка «{language.Code}» отсутствует языковой файл.",
                    entityId: language.Code,
                    field: "language");
            }
        }

        foreach (string language in localization.Tables.Keys)
        {
            if (!registered.Contains(language))
            {
                Error(
                    diagnostics,
                    "LOC_TABLE_UNREGISTERED",
                    $"Языковой файл «{language}» не зарегистрирован в languages.json.",
                    entityId: language,
                    field: "language");
            }
        }

        LocalizationTable? english = localization.Find("en");
        LocalizationTable? russian = localization.Find("ru");

        if (english is null)
        {
            Error(diagnostics, "LOC_EN_MISSING", "Отсутствует обязательный языковой файл en.json.", entityId: "en");
        }

        if (russian is null)
        {
            Error(diagnostics, "LOC_RU_MISSING", "Отсутствует обязательный языковой файл ru.json.", entityId: "ru");
        }

        if (english is not null && russian is not null)
        {
            CompareRequiredLanguageKeys(english, russian, diagnostics);
        }

        HashSet<string> referencedKeys = CollectReferencedKeys(registry);

        foreach (string key in referencedKeys)
        {
            if (english is not null && !english.Entries.ContainsKey(key))
            {
                Error(
                    diagnostics,
                    "LOC_KEY_MISSING_EN",
                    $"Ключ «{key}», используемый контентом, отсутствует в en.",
                    english.SourceFile,
                    key,
                    "entries");
            }

            if (russian is not null && !russian.Entries.ContainsKey(key))
            {
                Error(
                    diagnostics,
                    "LOC_KEY_MISSING_RU",
                    $"Ключ «{key}», используемый контентом, отсутствует в ru.",
                    russian.SourceFile,
                    key,
                    "entries");
            }
        }

        if (english is not null)
        {
            foreach (LocalizationTable table in localization.Tables.Values.OrderBy(t => t.Language, StringComparer.Ordinal))
            {
                if (table.Language is "en" or "ru")
                {
                    continue;
                }

                CheckOptionalLanguage(table, english, manifest, diagnostics);
            }
        }
    }

    private static void CompareRequiredLanguageKeys(
        LocalizationTable english,
        LocalizationTable russian,
        ContentDiagnosticCollection diagnostics)
    {
        List<string> missingInRussian = english.Entries.Keys
            .Where(key => !russian.Entries.ContainsKey(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();
        List<string> missingInEnglish = russian.Entries.Keys
            .Where(key => !english.Entries.ContainsKey(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        foreach (string key in missingInRussian)
        {
            Error(
                diagnostics,
                "LOC_REQUIRED_KEY_MISMATCH",
                $"Ключ «{key}» есть в en, но отсутствует в ru.",
                russian.SourceFile,
                key,
                "entries");
        }

        foreach (string key in missingInEnglish)
        {
            Error(
                diagnostics,
                "LOC_REQUIRED_KEY_MISMATCH",
                $"Ключ «{key}» есть в ru, но отсутствует в en.",
                english.SourceFile,
                key,
                "entries");
        }
    }

    private static void CheckOptionalLanguage(
        LocalizationTable table,
        LocalizationTable english,
        LocalizationManifest manifest,
        ContentDiagnosticCollection diagnostics)
    {
        LanguageDefinition? definition = manifest.Find(table.Language);

        if (definition is not null && definition.Status != table.Status)
        {
            Warning(
                diagnostics,
                "LOC_STATUS_MISMATCH",
                $"Статус языка «{table.Language}» в languages.json — {LocalizationStatusParser.ToToken(definition.Status)}, в файле — {LocalizationStatusParser.ToToken(table.Status)}.",
                table.SourceFile,
                table.Language,
                "status");
        }

        foreach (string key in table.Entries.Keys)
        {
            if (!english.Entries.ContainsKey(key))
            {
                Error(
                    diagnostics,
                    "LOC_EXTRA_KEY",
                    $"Ключ «{key}» отсутствует в en, но есть в языке «{table.Language}».",
                    table.SourceFile,
                    table.Language,
                    "entries");
            }
        }

        List<string> missing = english.Entries.Keys
            .Where(key => !table.Entries.ContainsKey(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        string examples = string.Join(", ", missing.Take(8));
        string suffix = missing.Count > 8 ? $"; всего {missing.Count}" : string.Empty;

        Warning(
            diagnostics,
            definition?.Status == LocalizationStatus.Complete
                ? "LOC_COMPLETE_KEYS_MISSING"
                : "LOC_OPTIONAL_KEYS_MISSING",
            $"В языке «{table.Language}» отсутствует {missing.Count} ключей: {examples}{suffix}.",
            table.SourceFile,
            table.Language,
            "entries");
    }

    private static HashSet<string> CollectReferencedKeys(ContentRegistry registry)
    {
        HashSet<string> keys = new(StringComparer.Ordinal);

        foreach (ContentEntry<ResourceDefinitionDto> entry in registry.Resources.Values)
        {
            AddKey(keys, entry.Value.NameKey);
        }

        foreach (ContentEntry<FactionDto> entry in registry.Factions.Values)
        {
            AddKey(keys, entry.Value.NameKey);
            AddKey(keys, entry.Value.DescriptionKey);
        }

        foreach (ContentEntry<UnitDto> entry in registry.Units.Values)
        {
            AddKey(keys, entry.Value.NameKey);
            AddKey(keys, entry.Value.DescriptionKey);
        }

        foreach (ContentEntry<HeroClassDto> entry in registry.HeroClasses.Values)
        {
            AddKey(keys, entry.Value.NameKey);
            AddKey(keys, entry.Value.DescriptionKey);
        }

        foreach (ContentEntry<HeroDto> entry in registry.Heroes.Values)
        {
            AddKey(keys, entry.Value.NameKey);
            AddKey(keys, entry.Value.DescriptionKey);
        }

        foreach (ContentEntry<AbilityDto> entry in registry.Abilities.Values)
        {
            AddKey(keys, entry.Value.NameKey);
            AddKey(keys, entry.Value.DescriptionKey);
        }

        foreach (ContentEntry<SpellDto> entry in registry.Spells.Values)
        {
            AddKey(keys, entry.Value.NameKey);
            AddKey(keys, entry.Value.DescriptionKey);
        }

        foreach (ContentEntry<BuildingDto> entry in registry.Buildings.Values)
        {
            AddKey(keys, entry.Value.NameKey);
            AddKey(keys, entry.Value.DescriptionKey);
        }

        foreach (ContentEntry<SettlementTemplateDto> entry in registry.Settlements.Values)
        {
            AddKey(keys, entry.Value.NameKey);
            AddKey(keys, entry.Value.DescriptionKey);
        }

        foreach (ContentEntry<MapObjectDto> entry in registry.MapObjects.Values)
        {
            AddKey(keys, entry.Value.NameKey);
            AddKey(keys, entry.Value.DescriptionKey);

            foreach (MapObjectOptionDto option in entry.Value.Options)
            {
                AddKey(keys, option.NameKey);
                AddKey(keys, option.DescriptionKey);
            }
        }

        foreach (ContentEntry<BiomeDto> entry in registry.Biomes.Values)
        {
            AddKey(keys, entry.Value.NameKey);
        }

        foreach (ContentEntry<TestMapDto> entry in registry.TestMaps.Values)
        {
            AddKey(keys, entry.Value.NameKey);
        }

        if (registry.MoraleRules is { } morale)
        {
            foreach (MoraleLevelDto level in morale.Value.Levels)
            {
                AddKey(keys, level.NameKey);
            }
        }

        if (registry.VeterancyRules is { } veterancy)
        {
            foreach (string key in veterancy.Value.RankNameKeys)
            {
                AddKey(keys, key);
            }
        }

        if (registry.Localization is { } localization)
        {
            foreach (LanguageDefinition language in localization.Manifest.Languages)
            {
                AddKey(keys, language.NameKey);
            }
        }

        return keys;
    }

    private static void AddKey(HashSet<string> keys, string? key)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            keys.Add(key);
        }
    }

    private static void ValidateResistances(
        UnitDto unit,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        RequireRange(unit.Resistances.Physical, 0, 100, "resistances.physical", unit.Id, source, diagnostics, "CONTENT_UNIT_RESIST");
        RequireRange(unit.Resistances.Magical, 0, 100, "resistances.magical", unit.Id, source, diagnostics, "CONTENT_UNIT_RESIST");
        RequireRange(unit.Resistances.Poison, 0, 100, "resistances.poison", unit.Id, source, diagnostics, "CONTENT_UNIT_RESIST");
        RequireRange(unit.Resistances.Fire, 0, 100, "resistances.fire", unit.Id, source, diagnostics, "CONTENT_UNIT_RESIST");
        RequireRange(unit.Resistances.Firearm, 0, 100, "resistances.firearm", unit.Id, source, diagnostics, "CONTENT_UNIT_RESIST");
    }

    private static void ValidateAbilityIds(
        IReadOnlyList<string> abilityIds,
        ContentRegistry registry,
        string entityId,
        string field,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);

        for (int index = 0; index < abilityIds.Count; index++)
        {
            string abilityId = abilityIds[index];

            if (!seen.Add(abilityId))
            {
                Error(
                    diagnostics,
                    "CONTENT_ABILITY_ID_DUPLICATE",
                    $"Способность «{abilityId}» указана более одного раза.",
                    source,
                    entityId,
                    $"{field}[{index}]");
            }

            RequireReference(
                registry.Abilities,
                abilityId,
                "CONTENT_REF_ABILITY_MISSING",
                "способность",
                source,
                entityId,
                $"{field}[{index}]",
                diagnostics);
        }
    }

    private static void ValidateStats(
        StatsDto stats,
        string fieldPrefix,
        string entityId,
        string source,
        ContentDiagnosticCollection diagnostics,
        bool requireLeadership)
    {
        RequireAtLeast(stats.Attack, 0, $"{fieldPrefix}.attack", entityId, source, diagnostics, "CONTENT_STATS_ATTACK");
        RequireAtLeast(stats.Defense, 0, $"{fieldPrefix}.defense", entityId, source, diagnostics, "CONTENT_STATS_DEFENSE");
        RequireAtLeast(stats.Spellpower, 0, $"{fieldPrefix}.spellpower", entityId, source, diagnostics, "CONTENT_STATS_SPELLPOWER");
        RequireAtLeast(stats.Knowledge, 0, $"{fieldPrefix}.knowledge", entityId, source, diagnostics, "CONTENT_STATS_KNOWLEDGE");
        RequireAtLeast(stats.Logistics, 0, $"{fieldPrefix}.logistics", entityId, source, diagnostics, "CONTENT_STATS_LOGISTICS");

        if (requireLeadership)
        {
            RequireAtLeast(stats.Leadership, 1, $"{fieldPrefix}.leadership", entityId, source, diagnostics, "CONTENT_STATS_LEADERSHIP");
        }
        else
        {
            RequireAtLeast(stats.Leadership, 0, $"{fieldPrefix}.leadership", entityId, source, diagnostics, "CONTENT_STATS_LEADERSHIP");
        }
    }

    private static void ValidateStatWeights(
        StatsDto weights,
        string entityId,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        long sum = 0;

        sum += ValidateWeight(weights.Attack, "statWeights.attack", entityId, source, diagnostics);
        sum += ValidateWeight(weights.Defense, "statWeights.defense", entityId, source, diagnostics);
        sum += ValidateWeight(weights.Spellpower, "statWeights.spellpower", entityId, source, diagnostics);
        sum += ValidateWeight(weights.Knowledge, "statWeights.knowledge", entityId, source, diagnostics);
        sum += ValidateWeight(weights.Leadership, "statWeights.leadership", entityId, source, diagnostics);
        sum += ValidateWeight(weights.Logistics, "statWeights.logistics", entityId, source, diagnostics);

        if (sum <= 0)
        {
            Error(
                diagnostics,
                "CONTENT_HERO_CLASS_WEIGHTS_SUM",
                "Сумма весов характеристик должна быть положительной.",
                source,
                entityId,
                "statWeights");
        }
    }

    private static int ValidateWeight(
        int value,
        string field,
        string entityId,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        if (value < 0)
        {
            Error(
                diagnostics,
                "CONTENT_HERO_CLASS_WEIGHT_NEGATIVE",
                $"Вес не может быть отрицательным: {value}.",
                source,
                entityId,
                field);

            return 0;
        }

        return value;
    }

    private static void ValidateResourceMap(
        IReadOnlyDictionary<string, int> map,
        string fieldPrefix,
        string entityId,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        foreach (KeyValuePair<string, int> pair in map)
        {
            if (!ContentSchema.IsResourceId(pair.Key))
            {
                Error(
                    diagnostics,
                    "CONTENT_RESOURCE_UNKNOWN",
                    $"Неизвестный ресурс «{pair.Key}»; допустимы {Join(ContentSchema.ResourceIds)}.",
                    source,
                    entityId,
                    $"{fieldPrefix}.{pair.Key}");
            }

            if (pair.Value < 0)
            {
                Error(
                    diagnostics,
                    "CONTENT_RESOURCE_AMOUNT",
                    $"Количество ресурса «{pair.Key}» не может быть отрицательным: {pair.Value}.",
                    source,
                    entityId,
                    $"{fieldPrefix}.{pair.Key}");
            }
        }
    }

    private static void ValidateRewardMap(
        IReadOnlyDictionary<string, int> map,
        string fieldPrefix,
        string entityId,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        foreach (KeyValuePair<string, int> pair in map)
        {
            if (!ContentSchema.IsResourceId(pair.Key)
                && !string.Equals(pair.Key, "reputation", StringComparison.Ordinal))
            {
                Error(
                    diagnostics,
                    "CONTENT_REWARD_UNKNOWN",
                    $"Неизвестная награда «{pair.Key}»; допустимы ресурсы и reputation.",
                    source,
                    entityId,
                    $"{fieldPrefix}.{pair.Key}");
            }

            if (pair.Value < 0)
            {
                Error(
                    diagnostics,
                    "CONTENT_REWARD_AMOUNT",
                    $"Награда «{pair.Key}» не может быть отрицательной: {pair.Value}.",
                    source,
                    entityId,
                    $"{fieldPrefix}.{pair.Key}");
            }
        }
    }

    private static void ValidateTags(
        IReadOnlyList<string> tags,
        string field,
        string entityId,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);

        for (int index = 0; index < tags.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(tags[index]))
            {
                Error(
                    diagnostics,
                    "CONTENT_TAG_EMPTY",
                    "Тег не может быть пустым.",
                    source,
                    entityId,
                    $"{field}[{index}]");
            }
            else if (!seen.Add(tags[index]))
            {
                Error(
                    diagnostics,
                    "CONTENT_TAG_DUPLICATE",
                    $"Тег «{tags[index]}» указан более одного раза.",
                    source,
                    entityId,
                    $"{field}[{index}]");
            }
        }
    }

    private static void ValidateNonEmptyUnique(
        IReadOnlyList<string> values,
        string field,
        string entityId,
        string source,
        ContentDiagnosticCollection diagnostics,
        string code)
    {
        if (values.Count == 0)
        {
            Error(diagnostics, code, $"Список «{field}» не может быть пустым.", source, entityId, field);
            return;
        }

        HashSet<string> seen = new(StringComparer.Ordinal);

        for (int index = 0; index < values.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(values[index]))
            {
                Error(diagnostics, code, $"Элемент «{field}[{index}]» пуст.", source, entityId, $"{field}[{index}]");
            }
            else if (!seen.Add(values[index]))
            {
                Error(diagnostics, code, $"Элемент «{values[index]}» повторяется.", source, entityId, $"{field}[{index}]");
            }
        }
    }

    private static void ValidateColor(
        string value,
        string field,
        string entityId,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        if (!IsHexColor(value))
        {
            Error(
                diagnostics,
                "CONTENT_FACTION_COLOR",
                $"Цвет «{value}» должен иметь формат #RRGGBB.",
                source,
                entityId,
                field);
        }
    }

    private static bool IsHexColor(string value)
    {
        if (value.Length != 7 || value[0] != '#')
        {
            return false;
        }

        for (int index = 1; index < value.Length; index++)
        {
            char symbol = value[index];

            if (!Uri.IsHexDigit(symbol))
            {
                return false;
            }
        }

        return true;
    }

    private static void RequireEntityId(
        string id,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        if (!ContentSchema.IsValidId(id))
        {
            Error(
                diagnostics,
                "CONTENT_ID_FORMAT",
                $"ID «{id}» должен быть в точечной нотации нижнего регистра, например core.unit.brass.militia.",
                source,
                id,
                "id");
        }
    }

    private static bool RequireReference<T>(
        IReadOnlyDictionary<string, ContentEntry<T>> entries,
        string? id,
        string code,
        string what,
        string source,
        string? entityId,
        string field,
        ContentDiagnosticCollection diagnostics)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Error(
                diagnostics,
                "CONTENT_FIELD_REQUIRED",
                $"Не заполнена ссылка на {what}.",
                source,
                entityId,
                field);

            return false;
        }

        if (entries.ContainsKey(id))
        {
            return true;
        }

        Error(
            diagnostics,
            code,
            $"Не найден {what} «{id}».",
            source,
            entityId,
            field);

        return false;
    }

    private static void RequireRange(
        int value,
        int min,
        int max,
        string field,
        string entityId,
        string source,
        ContentDiagnosticCollection diagnostics,
        string code)
    {
        if (value < min || value > max)
        {
            Error(
                diagnostics,
                code,
                $"Поле «{field}» должно быть в диапазоне {min}…{max}, указано {value}.",
                source,
                entityId,
                field);
        }
    }

    private static void RequireAtLeast(
        int value,
        int min,
        string field,
        string entityId,
        string source,
        ContentDiagnosticCollection diagnostics,
        string code)
    {
        if (value < min)
        {
            Error(
                diagnostics,
                code,
                $"Поле «{field}» должно быть не меньше {min}, указано {value}.",
                source,
                entityId,
                field);
        }
    }

    private static void ValidateOptionalNonNegative(
        int? value,
        string field,
        string entityId,
        string source,
        ContentDiagnosticCollection diagnostics,
        string code)
    {
        if (value is int number && number < 0)
        {
            Error(
                diagnostics,
                code,
                $"Поле «{field}» не может быть отрицательным: {number}.",
                source,
                entityId,
                field);
        }
    }

    private static string Join(IEnumerable<string> values)
    {
        return string.Join(", ", values.OrderBy(value => value, StringComparer.Ordinal));
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
            _ => string.Empty
        };
    }

    private static void Error(
        ContentDiagnosticCollection diagnostics,
        string code,
        string message,
        string? sourceFile = null,
        string? entityId = null,
        string? field = null)
    {
        diagnostics.Add(ContentDiagnostic.Error(code, message, sourceFile, entityId, field));
    }

    private static void Warning(
        ContentDiagnosticCollection diagnostics,
        string code,
        string message,
        string? sourceFile = null,
        string? entityId = null,
        string? field = null)
    {
        diagnostics.Add(ContentDiagnostic.Warning(code, message, sourceFile, entityId, field));
    }
}
