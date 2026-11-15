namespace Game.Core.Content.Dto;

/// <summary>Здание поселения.</summary>
public sealed class BuildingDto
{
    /// <summary>Стабильный ID здания.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>ID фракции-владельца.</summary>
    public string FactionId { get; init; } = string.Empty;

    /// <summary>Ключ локализации названия.</summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>Ключ локализации описания.</summary>
    public string DescriptionKey { get; init; } = string.Empty;

    /// <summary>Требуемый уровень поселения.</summary>
    public int RequiredLevel { get; init; }

    /// <summary>Стоимость постройки в ресурсах.</summary>
    public IReadOnlyDictionary<string, int> Cost { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Ежедневный доход в ресурсах.</summary>
    public IReadOnlyDictionary<string, int> DailyIncome { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Найм, если здание рекрутинговое.</summary>
    public RecruitmentDto? Recruitment { get; init; }
}
