namespace Game.Core.Content.Dto;

/// <summary>
/// Данные одного эффекта способности или заклинания. Набор полей намеренно
/// широкий: эффекты различаются полем <see cref="Type"/>, а движок боя
/// интерпретирует их на этапе 05. Свободные скрипты запрещены.
/// </summary>
public sealed class AbilityEffectDto
{
    /// <summary>Тип эффекта, например <c>damage</c> или <c>moraleBonus</c>.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Числовое значение эффекта.</summary>
    public int? Value { get; init; }

    /// <summary>ID юнита для эффектов призыва.</summary>
    public string? UnitId { get; init; }

    /// <summary>Количество юнитов для эффектов призыва.</summary>
    public int? Count { get; init; }

    /// <summary>Тип урона для эффектов урона и иммунитета.</summary>
    public string? DamageType { get; init; }

    /// <summary>Минимальный урон.</summary>
    public int? Min { get; init; }

    /// <summary>Максимальный урон.</summary>
    public int? Max { get; init; }

    /// <summary>Максимальный тир цели для эффекта.</summary>
    public int? MaxTier { get; init; }

    /// <summary>Тип местности для эффекта игнорирования штрафа.</summary>
    public string? Terrain { get; init; }

    /// <summary>Максимум юнитов за побеждённый отряд.</summary>
    public int? MaxCountPerStack { get; init; }

    /// <summary>Длительность эффекта в раундах.</summary>
    public int? DurationRounds { get; init; }

    /// <summary>Минимальная дистанция эффекта в гексах.</summary>
    public int? MinHexes { get; init; }
}
