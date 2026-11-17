namespace Game.Core.Content.Dto;

/// <summary>Утверждение, проверяемое на рукотворной карте.</summary>
public sealed class TestMapCaseDto
{
    /// <summary>Локальный ID утверждения, например <c>MS-01</c>.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Тип утверждения: movementCost, unreachable, collision, fogRadius, fullConnectivity, objectCount, minStartDistance.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Начальная координата для проверок пути.</summary>
    public TestMapCoordinateDto? From { get; init; }

    /// <summary>Целевая координата для проверок пути.</summary>
    public TestMapCoordinateDto? To { get; init; }

    /// <summary>Координата для проверки тумана.</summary>
    public TestMapCoordinateDto? At { get; init; }

    /// <summary>Ожидаемая стоимость пути.</summary>
    public int? ExpectedCost { get; init; }

    /// <summary>ID объекта, блокирующего цель.</summary>
    public string? BlockingObjectId { get; init; }

    /// <summary>Ожидаемый радиус видимости.</summary>
    public int? Radius { get; init; }

    /// <summary>Ожидаемое число достижимых гексов.</summary>
    public int? ExpectedReachableCount { get; init; }

    /// <summary>Ожидаемое общее число объектов.</summary>
    public int? ExpectedTotal { get; init; }

    /// <summary>Ожидаемое минимальное расстояние между стартами.</summary>
    public int? ExpectedMinHexDistance { get; init; }
}
