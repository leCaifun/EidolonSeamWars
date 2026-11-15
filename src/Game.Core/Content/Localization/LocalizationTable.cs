using System.Collections.ObjectModel;

namespace Game.Core.Content.Localization;

/// <summary>
/// Таблица переводов одного языка. Значения — только строки, ключи стабильны.
/// </summary>
public sealed class LocalizationTable
{
    private readonly ReadOnlyDictionary<string, string> _entries;

    /// <summary>
    /// Создаёт таблицу.
    /// </summary>
    /// <exception cref="ArgumentNullException">Словарь не задан.</exception>
    public LocalizationTable(
        string language,
        LocalizationStatus status,
        string? fallback,
        IReadOnlyDictionary<string, string> entries,
        string sourceFile)
    {
        ArgumentNullException.ThrowIfNull(entries);

        Language = language ?? string.Empty;
        Status = status;
        Fallback = fallback;
        SourceFile = sourceFile ?? string.Empty;
        _entries = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(entries, StringComparer.Ordinal));
    }

    /// <summary>Код языка.</summary>
    public string Language { get; }

    /// <summary>Статус из языкового файла.</summary>
    public LocalizationStatus Status { get; }

    /// <summary>Локальный фолбэк языка.</summary>
    public string? Fallback { get; }

    /// <summary>Файл-источник.</summary>
    public string SourceFile { get; }

    /// <summary>Все переводы.</summary>
    public IReadOnlyDictionary<string, string> Entries => _entries;

    /// <summary>Количество ключей.</summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Пытается получить перевод по ключу.
    /// </summary>
    public bool TryGet(string key, out string value)
    {
        if (key is not null && _entries.TryGetValue(key, out string? found))
        {
            value = found;
            return true;
        }

        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Возвращает новую таблицу с объединёнными ключами: значения
    /// <paramref name="overrides"/> заменяют текущие.
    /// </summary>
    public LocalizationTable WithOverrides(
        LocalizationStatus status,
        string? fallback,
        IReadOnlyDictionary<string, string> overrides,
        string sourceFile)
    {
        ArgumentNullException.ThrowIfNull(overrides);

        Dictionary<string, string> merged = new(_entries, StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> pair in overrides)
        {
            merged[pair.Key] = pair.Value;
        }

        return new LocalizationTable(Language, status, fallback, merged, sourceFile);
    }
}
