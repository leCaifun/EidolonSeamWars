namespace Game.Core.Content.Localization;

/// <summary>Зарегистрированный язык из <c>languages.json</c>.</summary>
public sealed class LanguageDefinition
{
    /// <summary>
    /// Создаёт описание языка.
    /// </summary>
    public LanguageDefinition(
        string code,
        string nameKey,
        string script,
        LocalizationStatus status,
        bool visibleInSettings,
        string? fallback,
        bool requiresFontReview)
    {
        Code = code;
        NameKey = nameKey;
        Script = script;
        Status = status;
        VisibleInSettings = visibleInSettings;
        Fallback = fallback;
        RequiresFontReview = requiresFontReview;
    }

    /// <summary>Код языка.</summary>
    public string Code { get; }

    /// <summary>Ключ локализации названия языка.</summary>
    public string NameKey { get; }

    /// <summary>Письменность.</summary>
    public string Script { get; }

    /// <summary>Статус готовности.</summary>
    public LocalizationStatus Status { get; }

    /// <summary>Показывать ли язык в настройках.</summary>
    public bool VisibleInSettings { get; }

    /// <summary>Язык фолбэка; null — использовать глобальный.</summary>
    public string? Fallback { get; }

    /// <summary>Требуется ли проверка шрифтового покрытия.</summary>
    public bool RequiresFontReview { get; }
}
