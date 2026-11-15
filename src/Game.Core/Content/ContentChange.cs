namespace Game.Core.Content;

/// <summary>
/// Запись журнала merge: какой пак добавил или переопределил определение.
/// </summary>
public sealed record ContentChange
{
    /// <summary>Тип операции.</summary>
    public required ContentChangeKind Kind { get; init; }

    /// <summary>Категория определения, например <c>unit</c>.</summary>
    public required string Category { get; init; }

    /// <summary>ID определения.</summary>
    public required string EntityId { get; init; }

    /// <summary>Пак-источник новой записи.</summary>
    public required string SourcePack { get; init; }

    /// <summary>Пак, запись которого заменена; null для добавления.</summary>
    public string? ReplacedPack { get; init; }

    /// <inheritdoc />
    public override string ToString()
    {
        return Kind == ContentChangeKind.Replace
            ? $"{Category} {EntityId}: {ReplacedPack} → {SourcePack}"
            : $"{Category} {EntityId}: + {SourcePack}";
    }
}
