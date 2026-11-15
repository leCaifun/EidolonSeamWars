namespace Game.Core.Content.Dto;

/// <summary>Заклинание героя.</summary>
public sealed class SpellDto
{
    /// <summary>Стабильный ID заклинания.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Ключ локализации названия.</summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>Ключ локализации описания.</summary>
    public string DescriptionKey { get; init; } = string.Empty;

    /// <summary>Школа магии (в MVP — <c>magical</c>).</summary>
    public string School { get; init; } = string.Empty;

    /// <summary>Стоимость маны.</summary>
    public int ManaCost { get; init; }

    /// <summary>Цель заклинания.</summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>Длительность в раундах.</summary>
    public int? DurationRounds { get; init; }

    /// <summary>Эффекты заклинания.</summary>
    public IReadOnlyList<AbilityEffectDto> Effects { get; init; } = Array.Empty<AbilityEffectDto>();
}
