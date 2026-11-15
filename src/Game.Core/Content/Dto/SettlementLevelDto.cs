namespace Game.Core.Content.Dto;

/// <summary>Уровень поселения: номер и число слотов под здания.</summary>
public sealed class SettlementLevelDto
{
    /// <summary>Номер уровня.</summary>
    public int Level { get; init; }

    /// <summary>Количество слотов под здания.</summary>
    public int BuildSlots { get; init; }
}
