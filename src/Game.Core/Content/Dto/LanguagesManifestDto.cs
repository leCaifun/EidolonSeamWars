namespace Game.Core.Content.Dto;

/// <summary>Список языков проекта — источник истины по локализации (ADR-026).</summary>
public sealed class LanguagesManifestDto
{
    /// <summary>Версия схемы.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>Стабильный ID манифеста.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Язык по умолчанию.</summary>
    public string DefaultLanguage { get; init; } = string.Empty;

    /// <summary>Глобальный язык фолбэка.</summary>
    public string FallbackLanguage { get; init; } = string.Empty;

    /// <summary>Языки, для которых обязательно полное покрытие.</summary>
    public IReadOnlyList<string> RequiredLanguages { get; init; } = Array.Empty<string>();

    /// <summary>Все зарегистрированные языки.</summary>
    public IReadOnlyList<LocalizationLanguageDto> Languages { get; init; } =
        Array.Empty<LocalizationLanguageDto>();
}
