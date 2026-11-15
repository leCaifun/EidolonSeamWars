namespace Game.Core.Content.Dto;

/// <summary>Описание языка в <c>languages.json</c> (ADR-026).</summary>
public sealed class LocalizationLanguageDto
{
    /// <summary>Код языка по ISO 639.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Ключ локализации названия языка.</summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>Письменность (Cyrl, Latn и т. п.).</summary>
    public string Script { get; init; } = string.Empty;

    /// <summary>Статус: complete, draft или experimental.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Показывать ли язык в настройках.</summary>
    public bool VisibleInSettings { get; init; }

    /// <summary>Язык фолбэка; null — использовать глобальный.</summary>
    public string? Fallback { get; init; }

    /// <summary>Требуется ли проверка шрифтового покрытия.</summary>
    public bool RequiresFontReview { get; init; }
}
