namespace Game.Core.Content.Dto;

/// <summary>Пять резистов юнита в целых процентах (ADR-010).</summary>
public sealed class ResistancesDto
{
    /// <summary>Физический резист, %.</summary>
    public int Physical { get; init; }

    /// <summary>Магический резист, %.</summary>
    public int Magical { get; init; }

    /// <summary>Ядовитый резист, %.</summary>
    public int Poison { get; init; }

    /// <summary>Огненный резист, %.</summary>
    public int Fire { get; init; }

    /// <summary>Огнестрельный резист, %.</summary>
    public int Firearm { get; init; }
}
