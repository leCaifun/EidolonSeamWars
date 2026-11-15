namespace Game.Core.Content.Dto;

/// <summary>Параметры найма в здании или внешнем жилище.</summary>
public sealed class RecruitmentDto
{
    /// <summary>ID нанимаемого юнита.</summary>
    public string UnitId { get; init; } = string.Empty;

    /// <summary>Суточный прирост в тысячных долях юнита.</summary>
    public int GrowthPerDayMilli { get; init; }

    /// <summary>Кап накопленного запаса.</summary>
    public int StockCap { get; init; }
}
