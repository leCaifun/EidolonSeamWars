using System.Collections.ObjectModel;

namespace Game.Core.Content.Localization;

/// <summary>
/// Набор всех загруженных языковых таблиц вместе с манифестом языков.
/// </summary>
public sealed class LocalizationSet
{
    private readonly ReadOnlyDictionary<string, LocalizationTable> _tables;

    /// <summary>
    /// Создаёт набор таблиц.
    /// </summary>
    /// <exception cref="ArgumentNullException">Манифест или таблицы не заданы.</exception>
    public LocalizationSet(
        LocalizationManifest manifest,
        IEnumerable<LocalizationTable> tables)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(tables);

        Manifest = manifest;
        Dictionary<string, LocalizationTable> byLanguage = new(StringComparer.Ordinal);

        foreach (LocalizationTable table in tables)
        {
            byLanguage[table.Language] = table;
        }

        _tables = new ReadOnlyDictionary<string, LocalizationTable>(byLanguage);
    }

    /// <summary>Манифест языков.</summary>
    public LocalizationManifest Manifest { get; }

    /// <summary>Таблицы по коду языка.</summary>
    public IReadOnlyDictionary<string, LocalizationTable> Tables => _tables;

    /// <summary>Количество загруженных языков.</summary>
    public int Count => _tables.Count;

    /// <summary>
    /// Возвращает таблицу языка или null.
    /// </summary>
    public LocalizationTable? Find(string language)
    {
        return language is not null && _tables.TryGetValue(language, out LocalizationTable? table)
            ? table
            : null;
    }

    /// <summary>
    /// Возвращает таблицу языка; отсутствие — ошибка конфигурации.
    /// </summary>
    /// <exception cref="InvalidOperationException">Язык не загружен.</exception>
    public LocalizationTable Get(string language)
    {
        if (Find(language) is { } table)
        {
            return table;
        }

        throw new InvalidOperationException($"Язык «{language}» не загружен.");
    }
}
