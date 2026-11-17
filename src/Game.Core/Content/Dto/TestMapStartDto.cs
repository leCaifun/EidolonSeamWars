namespace Game.Core.Content.Dto;

/// <summary>Стартовая позиция одной стороны на рукотворной карте.</summary>
public sealed class TestMapStartDto
{
    /// <summary>Столбец offset-координат.</summary>
    public int Col { get; init; }

    /// <summary>Строка offset-координат.</summary>
    public int Row { get; init; }

    /// <summary>ID фракции.</summary>
    public string FactionId { get; init; } = string.Empty;

    /// <summary>ID героя; null — отряд без героя.</summary>
    public string? HeroId { get; init; }
}
