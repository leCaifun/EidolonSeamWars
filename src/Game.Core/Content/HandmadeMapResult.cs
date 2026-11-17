using Game.Core.Content.Dto;
using Game.Core.Map;

namespace Game.Core.Content;

/// <summary>
/// Результат загрузки рукотворной карты: исходное определение, доменное
/// представление, готовая карта и проверяемые утверждения из контента.
/// </summary>
public sealed class HandmadeMapResult
{
    /// <summary>
    /// Создаёт результат загрузки.
    /// </summary>
    /// <exception cref="ArgumentNullException">Обязательный аргумент не задан.</exception>
    public HandmadeMapResult(
        TestMapDto definition,
        MapLayoutDefinition layout,
        GameMap map)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(map);

        Definition = definition;
        Layout = layout;
        Map = map;
    }

    /// <summary>Исходное определение из контента.</summary>
    public TestMapDto Definition { get; }

    /// <summary>Доменное представление карты.</summary>
    public MapLayoutDefinition Layout { get; }

    /// <summary>Готовая карта.</summary>
    public GameMap Map { get; }

    /// <summary>Проверяемые утверждения карты.</summary>
    public IReadOnlyList<TestMapCaseDto> TestCases => Definition.TestCases;
}
