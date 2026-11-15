namespace Game.Core.Content;

/// <summary>
/// Результат валидации контента: диагностика и агрегированные счётчики.
/// </summary>
public sealed class ContentValidationResult
{
    /// <summary>
    /// Создаёт результат валидации.
    /// </summary>
    public ContentValidationResult(ContentDiagnosticCollection diagnostics, int entityCount)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        Diagnostics = diagnostics;
        EntityCount = entityCount;
    }

    /// <summary>Сообщения валидатора.</summary>
    public ContentDiagnosticCollection Diagnostics { get; }

    /// <summary>Количество проверенных сущностей.</summary>
    public int EntityCount { get; }

    /// <summary>Валиден ли контент: ошибок нет.</summary>
    public bool IsValid => !Diagnostics.HasErrors;
}
