namespace Game.Core.Content.Dto;

/// <summary>
/// Рукотворная тестовая или сценарная карта из <c>test_maps/*.json</c>.
/// Layout задаётся строками, один символ — один биом из <see cref="Legend"/>.
/// </summary>
public sealed class TestMapDto
{
    /// <summary>Версия схемы.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>Стабильный ID карты.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Ключ локализации названия карты.</summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>Ширина карты в гексах.</summary>
    public int Width { get; init; }

    /// <summary>Высота карты в гексах.</summary>
    public int Height { get; init; }

    /// <summary>Схема offset-координат: <c>odd-r</c> или <c>even-r</c>.</summary>
    public string OffsetType { get; init; } = string.Empty;

    /// <summary>Соответствие символа layout и ID биома.</summary>
    public IReadOnlyDictionary<string, string> Legend { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Строки layout сверху вниз.</summary>
    public IReadOnlyList<string> Layout { get; init; } = Array.Empty<string>();

    /// <summary>Объекты, размещённые вручную.</summary>
    public IReadOnlyList<TestMapObjectDto> Objects { get; init; } =
        Array.Empty<TestMapObjectDto>();

    /// <summary>Стартовые позиции сторон.</summary>
    public TestMapStartPositionsDto? StartPositions { get; init; }

    /// <summary>Проверяемые утверждения карты.</summary>
    public IReadOnlyList<TestMapCaseDto> TestCases { get; init; } =
        Array.Empty<TestMapCaseDto>();
}
