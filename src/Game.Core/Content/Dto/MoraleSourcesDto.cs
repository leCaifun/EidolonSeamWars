namespace Game.Core.Content.Dto;

/// <summary>Источники изменения морали (GDD §7.5).</summary>
public sealed class MoraleSourcesDto
{
    /// <summary>Бонус за отряды своей фракции.</summary>
    public int SameFaction { get; init; }

    /// <summary>Штраф за каждую лишнюю фракцию.</summary>
    public int PerExtraFaction { get; init; }

    /// <summary>Штраф за нежить среди живых.</summary>
    public int UndeadAmongLiving { get; init; }

    /// <summary>Аура маршала.</summary>
    public int MarshalAura { get; init; }

    /// <summary>Бонус защиты поселения.</summary>
    public int DefendingSettlement { get; init; }

    /// <summary>Штраф за гибель союзного отряда.</summary>
    public int AllyStackDeath { get; init; }

    /// <summary>Длительность штрафа за гибель отряда, раунды.</summary>
    public int AllyStackDeathDurationRounds { get; init; }
}
