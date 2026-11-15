namespace Game.Core.Content.Dto;

/// <summary>Способность юнита, героя или класса.</summary>
public sealed class AbilityDto
{
    /// <summary>Стабильный ID способности.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Ключ локализации названия.</summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>Ключ локализации описания.</summary>
    public string DescriptionKey { get; init; } = string.Empty;

    /// <summary>Вид: passive, active или command.</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>Триггер срабатывания.</summary>
    public string Trigger { get; init; } = string.Empty;

    /// <summary>Цель способности.</summary>
    public string? Target { get; init; }

    /// <summary>Использований за бой.</summary>
    public int? UsesPerBattle { get; init; }

    /// <summary>Перезарядка в раундах.</summary>
    public int? CooldownRounds { get; init; }

    /// <summary>Длительность в раундах.</summary>
    public int? DurationRounds { get; init; }

    /// <summary>Радиус действия.</summary>
    public int? Radius { get; init; }

    /// <summary>Дальность действия.</summary>
    public int? Range { get; init; }

    /// <summary>Эффекты способности.</summary>
    public IReadOnlyList<AbilityEffectDto> Effects { get; init; } = Array.Empty<AbilityEffectDto>();
}
