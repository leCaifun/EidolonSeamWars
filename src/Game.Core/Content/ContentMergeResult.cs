namespace Game.Core.Content;

/// <summary>
/// Результат merge паков: итоговый реестр, диагностика и журнал изменений.
/// </summary>
public sealed class ContentMergeResult
{
    /// <summary>
    /// Создаёт результат merge.
    /// </summary>
    public ContentMergeResult(
        ContentRegistry? registry,
        ContentDiagnosticCollection diagnostics,
        IReadOnlyList<ContentChange> changes)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(changes);

        Registry = registry;
        Diagnostics = diagnostics;
        Changes = changes;
    }

    /// <summary>Итоговый реестр или null при ошибках планирования/merge.</summary>
    public ContentRegistry? Registry { get; }

    /// <summary>Диагностика merge.</summary>
    public ContentDiagnosticCollection Diagnostics { get; }

    /// <summary>Журнал добавлений и переопределений.</summary>
    public IReadOnlyList<ContentChange> Changes { get; }

    /// <summary>Успешен ли merge: реестр создан и ошибок нет.</summary>
    public bool IsSuccess => Registry is not null && !Diagnostics.HasErrors;
}
