namespace Game.Core.Content.Dto;

/// <summary>Один языковой файл локализации.</summary>
public sealed class LocalizationTableDto
{
    /// <summary>Версия схемы.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>Стабильный ID файла.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Код языка.</summary>
    public string Language { get; init; } = string.Empty;

    /// <summary>Статус: complete, draft или experimental.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Язык фолбэка; null — использовать глобальный.</summary>
    public string? Fallback { get; init; }

    /// <summary>Ключи и значения перевода.</summary>
    public IReadOnlyDictionary<string, string> Entries { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
