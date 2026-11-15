namespace Game.Core.Content.Dto;

/// <summary>Класс героя: стартовые характеристики, веса левелапа и способности.</summary>
public sealed class HeroClassDto
{
    /// <summary>Версия схемы записи.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>Стабильный ID класса.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>ID фракции-владельца.</summary>
    public string FactionId { get; init; } = string.Empty;

    /// <summary>Ключ локализации названия.</summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>Ключ локализации описания.</summary>
    public string DescriptionKey { get; init; } = string.Empty;

    /// <summary>Стартовые характеристики.</summary>
    public StatsDto StartingStats { get; init; } = new();

    /// <summary>Веса прироста характеристик при повышении уровня.</summary>
    public StatsDto StatWeights { get; init; } = new();

    /// <summary>ID пассивной классовой способности.</summary>
    public string PassiveAbilityId { get; init; } = string.Empty;

    /// <summary>ID командной способности 1/бой.</summary>
    public string CommandAbilityId { get; init; } = string.Empty;

    /// <summary>ID стартового заклинания.</summary>
    public string SpellId { get; init; } = string.Empty;
}
