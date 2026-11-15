namespace Game.Core.Content.Dto;

/// <summary>Палитра фракции. Цвета — строки <c>#RRGGBB</c>, проверяются валидатором.</summary>
public sealed class FactionColorsDto
{
    /// <summary>Основной цвет.</summary>
    public string Primary { get; init; } = string.Empty;

    /// <summary>Второстепенный цвет.</summary>
    public string Secondary { get; init; } = string.Empty;

    /// <summary>Акцентный цвет.</summary>
    public string Accent { get; init; } = string.Empty;

    /// <summary>Цвет фона карточки.</summary>
    public string Background { get; init; } = string.Empty;

    /// <summary>Цвет рамки.</summary>
    public string Frame { get; init; } = string.Empty;
}
