namespace Game.Core.Content;

/// <summary>
/// Результат загрузки, merge или валидации: полный список ошибок и предупреждений.
/// </summary>
public sealed class ContentDiagnosticCollection
{
    private readonly List<ContentDiagnostic> _items = new();

    /// <summary>Все сообщения в порядке добавления.</summary>
    public IReadOnlyList<ContentDiagnostic> Items => _items;

    /// <summary>Есть ли хотя бы одна ошибка.</summary>
    public bool HasErrors
    {
        get
        {
            foreach (ContentDiagnostic item in _items)
            {
                if (item.Severity == ContentDiagnosticSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Количество ошибок.</summary>
    public int ErrorCount
    {
        get
        {
            int count = 0;
            foreach (ContentDiagnostic item in _items)
            {
                if (item.Severity == ContentDiagnosticSeverity.Error)
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>Количество предупреждений.</summary>
    public int WarningCount => _items.Count - ErrorCount;

    /// <summary>Добавляет сообщение.</summary>
    /// <exception cref="ArgumentNullException">Сообщение не задано.</exception>
    public void Add(ContentDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        _items.Add(diagnostic);
    }

    /// <summary>Добавляет все сообщения другого набора.</summary>
    public void AddRange(IEnumerable<ContentDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        foreach (ContentDiagnostic diagnostic in diagnostics)
        {
            _items.Add(diagnostic);
        }
    }
}
