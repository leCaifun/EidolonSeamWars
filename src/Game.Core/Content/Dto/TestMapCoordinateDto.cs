namespace Game.Core.Content.Dto;

/// <summary>Координата в тестовом утверждении карты.</summary>
public sealed class TestMapCoordinateDto
{
    /// <summary>Столбец offset-координат.</summary>
    public int Col { get; init; }

    /// <summary>Строка offset-координат.</summary>
    public int Row { get; init; }
}
