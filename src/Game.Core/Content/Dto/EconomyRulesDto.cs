namespace Game.Core.Content.Dto;

/// <summary>Правила экономики из <c>rules/economy.json</c>.</summary>
public sealed class EconomyRulesDto
{
    /// <summary>Версия схемы.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>ID набора правил.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Базовый доход поселения в день.</summary>
    public IReadOnlyDictionary<string, int> BaseSettlementIncomePerDay { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Число дней накопления прироста до капа.</summary>
    public int GrowthCapDays { get; init; }

    /// <summary>Суточный прирост в тысячных по тирам.</summary>
    public IReadOnlyDictionary<string, int> GrowthPerDayMilliByTier { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Удаляются ли здания при захвате.</summary>
    public bool CaptureDeletesBuildings { get; init; }

    /// <summary>Уровень после захвата.</summary>
    public int CaptureResetsLevel { get; init; }

    /// <summary>Переключается ли шаблон на захватчика.</summary>
    public bool CaptureSwitchesTemplateToCaptor { get; init; }

    /// <summary>Порядок дневного тика.</summary>
    public IReadOnlyList<string> DailyTickOrder { get; init; } = Array.Empty<string>();
}
