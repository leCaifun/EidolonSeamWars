namespace Game.Core.Content.Dto;

/// <summary>Объект, размещённый на рукотворной карте.</summary>
public sealed class TestMapObjectDto
{
    /// <summary>Столбец offset-координат.</summary>
    public int Col { get; init; }

    /// <summary>Строка offset-координат.</summary>
    public int Row { get; init; }

    /// <summary>ID объекта карты.</summary>
    public string ObjectId { get; init; } = string.Empty;
}
