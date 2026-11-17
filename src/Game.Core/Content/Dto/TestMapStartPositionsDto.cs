namespace Game.Core.Content.Dto;

/// <summary>Стартовые позиции сторон на рукотворной карте.</summary>
public sealed class TestMapStartPositionsDto
{
    /// <summary>Старт игрока.</summary>
    public TestMapStartDto? Player { get; init; }

    /// <summary>Старт противника.</summary>
    public TestMapStartDto? Enemy { get; init; }

    /// <summary>Нейтральный старт.</summary>
    public TestMapStartDto? Neutral { get; init; }
}
