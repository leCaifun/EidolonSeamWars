namespace Game.Core.Content;

/// <summary>
/// Диагностическое сообщение с указанием файла, поля и записи. Пригодно
/// для исправления человеком без чтения кода валидатора.
/// </summary>
public sealed record ContentDiagnostic
{
    /// <summary>Важность сообщения.</summary>
    public required ContentDiagnosticSeverity Severity { get; init; }

    /// <summary>Стабильный машинный код сообщения, например <c>CONTENT_ID_DUPLICATE</c>.</summary>
    public required string Code { get; init; }

    /// <summary>Файл-источник; для предупреждений без файла — пустая строка.</summary>
    public string SourceFile { get; init; } = string.Empty;

    /// <summary>ID записи, если сообщение относится к конкретной записи.</summary>
    public string? EntityId { get; init; }

    /// <summary>Поле или путь внутри JSON, если применимо.</summary>
    public string? Field { get; init; }

    /// <summary>Человекочитаемое описание на русском языке.</summary>
    public required string Message { get; init; }

    /// <summary>
    /// Создаёт сообщение об ошибке.
    /// </summary>
    public static ContentDiagnostic Error(
        string code,
        string message,
        string? sourceFile = null,
        string? entityId = null,
        string? field = null)
    {
        return new ContentDiagnostic
        {
            Severity = ContentDiagnosticSeverity.Error,
            Code = code,
            SourceFile = sourceFile ?? string.Empty,
            EntityId = entityId,
            Field = field,
            Message = message
        };
    }

    /// <summary>
    /// Создаёт предупреждение.
    /// </summary>
    public static ContentDiagnostic Warning(
        string code,
        string message,
        string? sourceFile = null,
        string? entityId = null,
        string? field = null)
    {
        return new ContentDiagnostic
        {
            Severity = ContentDiagnosticSeverity.Warning,
            Code = code,
            SourceFile = sourceFile ?? string.Empty,
            EntityId = entityId,
            Field = field,
            Message = message
        };
    }

    /// <summary>Форматирует запись для консольного вывода.</summary>
    public override string ToString()
    {
        string prefix = Severity == ContentDiagnosticSeverity.Error ? "ОШИБКА" : "ПРЕДУПРЕЖДЕНИЕ";
        string location = SourceFile.Length == 0 ? "<общий>" : SourceFile;

        if (!string.IsNullOrEmpty(EntityId))
        {
            location += $" [{EntityId}]";
        }

        if (!string.IsNullOrEmpty(Field))
        {
            location += $" поле {Field}";
        }

        return $"{prefix} {Code}: {location}: {Message}";
    }
}
