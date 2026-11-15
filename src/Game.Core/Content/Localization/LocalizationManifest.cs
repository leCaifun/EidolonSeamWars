using System.Collections.ObjectModel;

namespace Game.Core.Content.Localization;

/// <summary>
/// Манифест языков проекта. Источник истины по списку языков, статусам
/// и фолбэку (ADR-026).
/// </summary>
public sealed class LocalizationManifest
{
    private readonly ReadOnlyCollection<LanguageDefinition> _languages;
    private readonly ReadOnlyCollection<string> _requiredLanguages;
    private readonly Dictionary<string, LanguageDefinition> _byCode;

    /// <summary>
    /// Создаёт манифест.
    /// </summary>
    /// <exception cref="ArgumentNullException">Списки не заданы.</exception>
    public LocalizationManifest(
        string defaultLanguage,
        string fallbackLanguage,
        IEnumerable<string> requiredLanguages,
        IEnumerable<LanguageDefinition> languages)
    {
        ArgumentNullException.ThrowIfNull(requiredLanguages);
        ArgumentNullException.ThrowIfNull(languages);

        DefaultLanguage = defaultLanguage ?? string.Empty;
        FallbackLanguage = fallbackLanguage ?? string.Empty;
        _requiredLanguages = Array.AsReadOnly(requiredLanguages.ToArray());
        _languages = Array.AsReadOnly(languages.ToArray());
        _byCode = new Dictionary<string, LanguageDefinition>(StringComparer.Ordinal);

        foreach (LanguageDefinition language in _languages)
        {
            _byCode[language.Code] = language;
        }
    }

    /// <summary>Язык по умолчанию.</summary>
    public string DefaultLanguage { get; }

    /// <summary>Глобальный язык фолбэка.</summary>
    public string FallbackLanguage { get; }

    /// <summary>Языки с обязательным полным покрытием.</summary>
    public IReadOnlyList<string> RequiredLanguages => _requiredLanguages;

    /// <summary>Все зарегистрированные языки.</summary>
    public IReadOnlyList<LanguageDefinition> Languages => _languages;

    /// <summary>
    /// Возвращает описание языка по коду или null.
    /// </summary>
    public LanguageDefinition? Find(string code)
    {
        return code is not null && _byCode.TryGetValue(code, out LanguageDefinition? language)
            ? language
            : null;
    }

    /// <summary>Языки, видимые в настройках: complete и visibleInSettings.</summary>
    public IReadOnlyList<LanguageDefinition> GetSettingsLanguages()
    {
        List<LanguageDefinition> result = new();

        foreach (LanguageDefinition language in _languages)
        {
            if (language.Status == LocalizationStatus.Complete && language.VisibleInSettings)
            {
                result.Add(language);
            }
        }

        return result.AsReadOnly();
    }
}
