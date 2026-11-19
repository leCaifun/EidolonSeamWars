namespace Game.Core.Battle;

/// <summary>
/// Справочник способностей и заклинаний боя. Пустой справочник допустим: бой без способностей.
/// </summary>
public sealed class BattleCatalog
{
    private readonly Dictionary<string, AbilityDef> _abilities;
    private readonly Dictionary<string, SpellDef> _spells;

    /// <summary>
    /// Создаёт справочник.
    /// </summary>
    /// <param name="abilities">Способности.</param>
    /// <param name="spells">Заклинания.</param>
    /// <exception cref="ArgumentException">Повторяющийся ID.</exception>
    public BattleCatalog(IEnumerable<AbilityDef> abilities, IEnumerable<SpellDef> spells)
    {
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(spells);

        _abilities = new Dictionary<string, AbilityDef>(StringComparer.Ordinal);
        _spells = new Dictionary<string, SpellDef>(StringComparer.Ordinal);

        foreach (AbilityDef ability in abilities)
        {
            if (!_abilities.TryAdd(ability.Id, ability))
            {
                throw new ArgumentException($"Способность «{ability.Id}» объявлена дважды.", nameof(abilities));
            }
        }

        foreach (SpellDef spell in spells)
        {
            if (!_spells.TryAdd(spell.Id, spell))
            {
                throw new ArgumentException($"Заклинание «{spell.Id}» объявлено дважды.", nameof(spells));
            }
        }
    }

    /// <summary>Пустой справочник.</summary>
    public static BattleCatalog Empty { get; } = new(Array.Empty<AbilityDef>(), Array.Empty<SpellDef>());

    /// <summary>Все способности.</summary>
    public IReadOnlyDictionary<string, AbilityDef> Abilities => _abilities;

    /// <summary>Все заклинания.</summary>
    public IReadOnlyDictionary<string, SpellDef> Spells => _spells;

    /// <summary>Способность по ID или <c>null</c>.</summary>
    public AbilityDef? FindAbility(string? id)
    {
        return id is not null && _abilities.TryGetValue(id, out AbilityDef? ability) ? ability : null;
    }

    /// <summary>Заклинание по ID или <c>null</c>.</summary>
    public SpellDef? FindSpell(string? id)
    {
        return id is not null && _spells.TryGetValue(id, out SpellDef? spell) ? spell : null;
    }
}
