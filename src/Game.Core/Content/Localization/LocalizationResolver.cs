using Game.Core.Diagnostics;

namespace Game.Core.Content.Localization;

/// <summary>
/// Разрешение ключей локализации по цепочке
/// «выбранный язык → fallback языка → глобальный fallback → ключ».
/// Отсутствующий ключ логируется как Warning и возвращается как строка.
/// </summary>
public sealed class LocalizationResolver
{
    private readonly LocalizationSet _set;
    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт резолвер.
    /// </summary>
    /// <exception cref="ArgumentNullException">Набор таблиц не задан.</exception>
    public LocalizationResolver(LocalizationSet set, IGameLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(set);
        _set = set;
        _logger = logger;
    }

    /// <summary>
    /// Возвращает перевод ключа или сам ключ, если перевода нет.
    /// </summary>
    /// <param name="language">Выбранный язык.</param>
    /// <param name="key">Ключ локализации.</param>
    public string Resolve(string language, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        HashSet<string> visited = new(StringComparer.Ordinal);
        string? current = language;

        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            LocalizationTable? table = _set.Find(current);

            if (table is not null && table.TryGet(key, out string value))
            {
                return value;
            }

            current = ResolveNextLanguage(current, table);
        }

        _logger?.Log(LogEvent.Create(
            LogLevel.Warning,
            "Loc",
            $"Отсутствует ключ локализации «{key}» для языка «{language}».",
            fields: new Dictionary<string, string>
            {
                ["key"] = key,
                ["language"] = language
            }));

        return key;
    }

    private string? ResolveNextLanguage(string language, LocalizationTable? table)
    {
        if (table?.Fallback is { Length: > 0 } localFallback)
        {
            return localFallback;
        }

        LanguageDefinition? definition = _set.Manifest.Find(language);

        if (definition?.Fallback is { Length: > 0 } definitionFallback)
        {
            return definitionFallback;
        }

        return _set.Manifest.FallbackLanguage;
    }
}
