namespace Game.Core.Content.Dto;

/// <summary>Описательный уровень морали.</summary>
public sealed class MoraleLevelDto
{
    /// <summary>Уровень от −4 до +4.</summary>
    public int Level { get; init; }

    /// <summary>Ключ локализации названия уровня.</summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>Шанс дополнительного действия или пропуска, %.</summary>
    public int ChancePercent { get; init; }

    /// <summary>Модификатор атаки и защиты, %.</summary>
    public int StatModifierPercent { get; init; }
}
