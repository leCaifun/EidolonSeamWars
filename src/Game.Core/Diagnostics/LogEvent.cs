namespace Game.Core.Diagnostics;

/// <summary>
/// Структурированная запись журнала. Иммутабельна и не зависит от движка.
/// Сериализация выполняется слоем инфраструктуры (см. docs/LOGGING.md).
/// </summary>
public sealed record LogEvent
{
    /// <summary>Момент события в UTC.</summary>
    public required DateTimeOffset TimestampUtc { get; init; }

    /// <summary>Уровень важности записи.</summary>
    public required LogLevel Level { get; init; }

    /// <summary>Категория записи (Content, Mods, Save, Battle, Economy, Map, AI, Hero, UI, Settings, Loc).</summary>
    public required string Category { get; init; }

    /// <summary>Человекочитаемое сообщение на русском языке.</summary>
    public required string Message { get; init; }

    /// <summary>Идентификатор игровой сессии для сквозной корреляции.</summary>
    public string? SessionId { get; init; }

    /// <summary>Идентификатор корреляции (например, battleId или armyId).</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Дополнительные структурированные поля записи.</summary>
    public IReadOnlyDictionary<string, string> Fields { get; init; } =
        EmptyFields;

    private static readonly IReadOnlyDictionary<string, string> EmptyFields =
        new Dictionary<string, string>(0);

    /// <summary>
    /// Создаёт запись журнала с текущей меткой времени UTC.
    /// </summary>
    /// <param name="level">Уровень важности.</param>
    /// <param name="category">Категория записи.</param>
    /// <param name="message">Текст сообщения.</param>
    /// <param name="sessionId">Необязательный идентификатор сессии.</param>
    /// <param name="correlationId">Необязательный идентификатор корреляции.</param>
    /// <param name="fields">Необязательные дополнительные поля.</param>
    /// <exception cref="ArgumentException">Категория или сообщение пусты.</exception>
    public static LogEvent Create(
        LogLevel level,
        string category,
        string message,
        string? sessionId = null,
        string? correlationId = null,
        IReadOnlyDictionary<string, string>? fields = null)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Категория записи журнала не может быть пустой.", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Сообщение журнала не может быть пустым.", nameof(message));
        }

        return new LogEvent
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Level = level,
            Category = category,
            Message = message,
            SessionId = sessionId,
            CorrelationId = correlationId,
            Fields = fields ?? EmptyFields
        };
    }
}
