namespace Game.Core.Content.Dto;

/// <summary>Стартовый герой фракции.</summary>
public sealed class HeroDto
{
    /// <summary>Версия схемы записи.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>Стабильный ID героя.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>ID фракции героя.</summary>
    public string FactionId { get; init; } = string.Empty;

    /// <summary>ID класса героя.</summary>
    public string ClassId { get; init; } = string.Empty;

    /// <summary>Ключ локализации имени.</summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>Ключ локализации описания.</summary>
    public string DescriptionKey { get; init; } = string.Empty;

    /// <summary>Стартовый уровень.</summary>
    public int Level { get; init; }

    /// <summary>Стартовый опыт.</summary>
    public int Xp { get; init; }

    /// <summary>Накопленные очки навыков.</summary>
    public int SkillPoints { get; init; }

    /// <summary>Стартовые характеристики.</summary>
    public StatsDto Stats { get; init; } = new();

    /// <summary>Стартовая армия.</summary>
    public IReadOnlyList<HeroArmySlotDto> Army { get; init; } = Array.Empty<HeroArmySlotDto>();

    /// <summary>Слоты артефактов.</summary>
    public HeroEquipmentDto Equipment { get; init; } = new();

    /// <summary>ID стартовых артефактов.</summary>
    public IReadOnlyList<string> Artifacts { get; init; } = Array.Empty<string>();
}
