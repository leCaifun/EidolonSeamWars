namespace Game.Core.Content.Dto;

/// <summary>Двенадцать слотов артефактов героя (GDD §9.4). Пустой слот — null.</summary>
public sealed class HeroEquipmentDto
{
    /// <summary>Голова.</summary>
    public string? Head { get; init; }

    /// <summary>Шея.</summary>
    public string? Neck { get; init; }

    /// <summary>Левая рука.</summary>
    public string? LeftHand { get; init; }

    /// <summary>Правая рука.</summary>
    public string? RightHand { get; init; }

    /// <summary>Плечи.</summary>
    public string? Shoulders { get; init; }

    /// <summary>Торс.</summary>
    public string? Torso { get; init; }

    /// <summary>Спина.</summary>
    public string? Back { get; init; }

    /// <summary>Пояс.</summary>
    public string? Belt { get; init; }

    /// <summary>Ноги.</summary>
    public string? Legs { get; init; }

    /// <summary>Стопы.</summary>
    public string? Feet { get; init; }

    /// <summary>Кольцо 1.</summary>
    public string? Ring1 { get; init; }

    /// <summary>Кольцо 2.</summary>
    public string? Ring2 { get; init; }
}
