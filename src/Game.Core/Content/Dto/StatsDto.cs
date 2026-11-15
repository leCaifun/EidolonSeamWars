namespace Game.Core.Content.Dto;

/// <summary>Шесть характеристик героя в JSON-контенте (GDD §9.2).</summary>
public sealed class StatsDto
{
    /// <summary>Атака.</summary>
    public int Attack { get; init; }

    /// <summary>Защита.</summary>
    public int Defense { get; init; }

    /// <summary>Чародейство.</summary>
    public int Spellpower { get; init; }

    /// <summary>Знание.</summary>
    public int Knowledge { get; init; }

    /// <summary>Лидерство.</summary>
    public int Leadership { get; init; }

    /// <summary>Логистика.</summary>
    public int Logistics { get; init; }
}
