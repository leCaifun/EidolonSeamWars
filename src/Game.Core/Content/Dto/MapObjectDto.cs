namespace Game.Core.Content.Dto;

/// <summary>Объект карты: шахта, жилище, эхо-схрон или патруль.</summary>
public sealed class MapObjectDto
{
    /// <summary>Стабильный ID объекта.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Тип объекта.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Ключ локализации названия.</summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>Ключ локализации описания.</summary>
    public string DescriptionKey { get; init; } = string.Empty;

    /// <summary>Охраняется ли объект.</summary>
    public bool Guarded { get; init; }

    /// <summary>Доход в день для шахт.</summary>
    public IReadOnlyDictionary<string, int> IncomePerDay { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>ID фракции для жилищ, патрулей и поселений.</summary>
    public string? FactionId { get; init; }

    /// <summary>ID шаблона поселения для объектов типа <c>settlement</c>.</summary>
    public string? TemplateId { get; init; }

    /// <summary>Начальный уровень поселения для объектов типа <c>settlement</c>.</summary>
    public int? InitialLevel { get; init; }

    /// <summary>Найм для жилищ.</summary>
    public RecruitmentDto? Recruitment { get; init; }

    /// <summary>Варианты взаимодействия для эхо-схронов.</summary>
    public IReadOnlyList<MapObjectOptionDto> Options { get; init; } =
        Array.Empty<MapObjectOptionDto>();

    /// <summary>ID героя-лидера патруля; null — отряд без героя.</summary>
    public string? HeroId { get; init; }

    /// <summary>Армия патруля.</summary>
    public IReadOnlyList<HeroArmySlotDto> Army { get; init; } = Array.Empty<HeroArmySlotDto>();
}
