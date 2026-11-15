namespace Game.Core.Content.Dto;

/// <summary>Фракция: палитра, максимальный уровень поселения и класс героя.</summary>
public sealed class FactionDto
{
    /// <summary>Версия схемы записи.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>Стабильный ID фракции.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Ключ локализации названия.</summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>Ключ локализации описания.</summary>
    public string DescriptionKey { get; init; } = string.Empty;

    /// <summary>Палитра фракции.</summary>
    public FactionColorsDto Colors { get; init; } = new();

    /// <summary>Максимальный уровень поселения, 2…4.</summary>
    public int MaxSettlementLevel { get; init; }

    /// <summary>ID класса героя фракции.</summary>
    public string HeroClassId { get; init; } = string.Empty;

    /// <summary>Теги фракции (например, <c>undead</c>, <c>living</c>).</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
