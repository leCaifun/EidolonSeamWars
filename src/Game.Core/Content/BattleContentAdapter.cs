using Game.Core.Battle;
using Game.Core.Content.Dto;
using Game.Core.Domain;

namespace Game.Core.Content;

/// <summary>
/// Преобразует боевой контент (<c>rules/battle.json</c>, юниты) в доменные типы боя.
/// Единственное место, где токены контента превращаются в перечисления домена.
/// </summary>
public static class BattleContentAdapter
{
    private const int DefaultUnitSchemaVersion = 1;

    /// <summary>
    /// Создаёт боевые правила из контента.
    /// </summary>
    /// <param name="registry">Слитый реестр контента.</param>
    /// <exception cref="ArgumentNullException">Реестр не задан.</exception>
    /// <exception cref="InvalidOperationException">В контенте нет <c>rules/battle.json</c>.</exception>
    public static BattleRules CreateRules(ContentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        if (registry.BattleRules is null)
        {
            throw new InvalidOperationException("В контенте отсутствуют боевые правила (rules/battle.json).");
        }

        BattleRulesDto dto = registry.BattleRules.Value;

        return new BattleRules(
            dto.FieldWidth,
            dto.FieldHeight,
            dto.HeroOnBattlefield,
            dto.DefendDefenseBonusPercent,
            dto.DefendResistBonusPercent,
            dto.DefendInitiativePenalty,
            dto.RetaliationPerRound,
            dto.RetaliationIsAction,
            dto.WaitMovesToEndOfRound,
            dto.WaitAffectsNextRound,
            dto.AttackDefenseModifierPerPoint,
            dto.AttackDefenseModifierMin,
            dto.AttackDefenseModifierMax,
            dto.CritChanceCapPercent,
            dto.CritDamageCapPercent,
            dto.RangedLongDistanceFieldPercent,
            dto.RangedLongDistancePenaltyPercent,
            dto.RangedObstaclePenaltyPercent,
            dto.RangedMeleePenaltyPercent,
            dto.AttackMovementCost);
    }

    /// <summary>
    /// Создаёт правила морали из контента (<c>rules/morale.json</c>).
    /// </summary>
    /// <param name="registry">Слитый реестр контента.</param>
    /// <exception cref="ArgumentNullException">Реестр не задан.</exception>
    /// <exception cref="InvalidOperationException">В контенте нет <c>rules/morale.json</c>.</exception>
    public static MoraleRules CreateMoraleRules(ContentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        if (registry.MoraleRules is null)
        {
            throw new InvalidOperationException("В контенте отсутствуют правила морали (rules/morale.json).");
        }

        MoraleRulesDto dto = registry.MoraleRules.Value;

        return new MoraleRules(
            dto.MinLevel,
            dto.MaxLevel,
            dto.ExtraActionChanceCapPercent,
            dto.StatModifierCapPercent,
            dto.MaxExtraActionsPerUnitPerRound,
            dto.ImmuneTags,
            dto.Levels.Select(level => new MoraleLevelRule(
                level.Level,
                level.NameKey,
                level.ChancePercent,
                level.StatModifierPercent)),
            dto.Sources.SameFaction,
            dto.Sources.PerExtraFaction,
            dto.Sources.UndeadAmongLiving,
            dto.Sources.MarshalAura,
            dto.Sources.DefendingSettlement,
            dto.Sources.AllyStackDeath,
            dto.Sources.AllyStackDeathDurationRounds);
    }

    /// <summary>
    /// Создаёт справочник способностей и заклинаний боя из контента.
    /// </summary>
    /// <param name="registry">Слитый реестр контента.</param>
    /// <exception cref="ArgumentNullException">Реестр не задан.</exception>
    public static BattleCatalog CreateCatalog(ContentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        List<AbilityDef> abilities = registry.Abilities.Values
            .OrderBy(entry => entry.Value.Id, StringComparer.Ordinal)
            .Select(entry => CreateAbility(entry.Value))
            .ToList();

        List<SpellDef> spells = registry.Spells.Values
            .OrderBy(entry => entry.Value.Id, StringComparer.Ordinal)
            .Select(entry => CreateSpell(entry.Value))
            .ToList();

        return new BattleCatalog(abilities, spells);
    }

    /// <summary>
    /// Возвращает ID классовой пассивной способности класса героя.
    /// </summary>
    /// <param name="registry">Слитый реестр контента.</param>
    /// <param name="heroClassId">ID класса героя.</param>
    /// <returns>ID способности или <c>null</c>, если класса нет.</returns>
    public static string? GetClassPassiveAbilityId(ContentRegistry registry, string heroClassId)
    {
        ArgumentNullException.ThrowIfNull(registry);

        return registry.HeroClasses.TryGetValue(heroClassId, out ContentEntry<HeroClassDto>? entry)
            ? entry.Value.PassiveAbilityId
            : null;
    }

    /// <summary>Создаёт определение способности из DTO.</summary>
    /// <param name="dto">DTO способности.</param>
    /// <exception cref="ArgumentNullException">DTO не задан.</exception>
    public static AbilityDef CreateAbility(AbilityDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new AbilityDef(
            dto.Id,
            dto.Kind,
            dto.Trigger,
            dto.Target,
            dto.Range ?? 0,
            dto.Radius ?? 0,
            dto.CooldownRounds ?? 0,
            dto.DurationRounds ?? 0,
            dto.UsesPerBattle ?? 0,
            dto.Effects.Select(effect => CreateEffect(effect, dto.Id)).ToList());
    }

    /// <summary>Создаёт определение заклинания из DTO.</summary>
    /// <param name="dto">DTO заклинания.</param>
    /// <exception cref="ArgumentNullException">DTO не задан.</exception>
    public static SpellDef CreateSpell(SpellDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new SpellDef(
            dto.Id,
            dto.School,
            dto.ManaCost,
            dto.Target,
            dto.DurationRounds ?? 0,
            dto.Effects.Select(effect => CreateEffect(effect, dto.Id)).ToList());
    }

    private static EffectDef CreateEffect(AbilityEffectDto dto, string ownerId)
    {
        return new EffectDef(
            dto.Type,
            dto.Value ?? 0,
            string.IsNullOrWhiteSpace(dto.DamageType) ? null : ParseDamageType(dto.DamageType, ownerId),
            dto.Min ?? 0,
            dto.Max ?? 0,
            dto.MinHexes ?? 0,
            dto.MaxTier ?? 0,
            dto.UnitId,
            dto.Count ?? 0,
            dto.MaxCountPerStack ?? 0,
            dto.DurationRounds ?? 0);
    }

    /// <summary>
    /// Создаёт определения всех юнитов из контента, упорядоченные по ID.
    /// </summary>
    /// <param name="registry">Слитый реестр контента.</param>
    /// <exception cref="ArgumentNullException">Реестр не задан.</exception>
    public static IReadOnlyDictionary<string, UnitDef> CreateUnits(ContentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        SortedDictionary<string, UnitDef> units = new(StringComparer.Ordinal);

        foreach (ContentEntry<UnitDto> entry in registry.Units.Values)
        {
            UnitDef unit = CreateUnit(entry.Value);
            units.Add(unit.Id, unit);
        }

        return units;
    }

    /// <summary>
    /// Создаёт определение юнита из DTO.
    /// </summary>
    /// <param name="dto">DTO юнита.</param>
    /// <exception cref="ArgumentNullException">DTO не задан.</exception>
    /// <exception cref="ArgumentException">Неизвестный токен типа движения или урона.</exception>
    public static UnitDef CreateUnit(UnitDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        Resistances resistances = new(
            dto.Resistances.Physical,
            dto.Resistances.Magical,
            dto.Resistances.Poison,
            dto.Resistances.Fire,
            dto.Resistances.Firearm);

        return new UnitDef(
            dto.Id,
            dto.SchemaVersion > 0 ? dto.SchemaVersion : DefaultUnitSchemaVersion,
            dto.NameKey,
            dto.DescriptionKey,
            dto.FactionId,
            dto.Tier,
            dto.LeadershipCost,
            dto.Attack,
            dto.Defense,
            dto.Initiative,
            dto.Speed,
            dto.DamageMin,
            dto.DamageMax,
            dto.CritChancePercent,
            dto.CritDamagePercent,
            dto.HitPoints,
            ParseMovementType(dto.MovementType, dto.Id),
            resistances,
            dto.Abilities,
            dto.PromotionIds,
            dto.Ammo,
            dto.UnitClass,
            ParseDamageType(dto.DamageType, dto.Id));
    }

    /// <summary>
    /// Переводит токен типа урона контента в <see cref="DamageType"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Неизвестный токен.</exception>
    public static DamageType ParseDamageType(string token, string ownerId)
    {
        return token switch
        {
            "physical" => DamageType.Physical,
            "magical" => DamageType.Magic,
            "poison" => DamageType.Poison,
            "fire" => DamageType.Fire,
            "firearm" => DamageType.Gunpowder,
            _ => throw new ArgumentException(
                $"Неизвестный тип урона «{token}» у «{ownerId}».",
                nameof(token))
        };
    }

    /// <summary>
    /// Переводит токен типа движения контента в <see cref="MovementType"/>. Конница и звери
    /// ходят по земле и в домене относятся к пехоте; различия задаются данными способностей.
    /// </summary>
    /// <exception cref="ArgumentException">Неизвестный токен.</exception>
    public static MovementType ParseMovementType(string token, string ownerId)
    {
        return token switch
        {
            "foot" => MovementType.Infantry,
            "cavalry" => MovementType.Infantry,
            "beast" => MovementType.Infantry,
            "shooter" => MovementType.Ranged,
            "flyer" => MovementType.Flyer,
            "siege" => MovementType.Siege,
            _ => throw new ArgumentException(
                $"Неизвестный тип движения «{token}» у «{ownerId}».",
                nameof(token))
        };
    }
}
