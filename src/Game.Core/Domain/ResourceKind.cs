namespace Game.Core.Domain;

/// <summary>
/// Семь ресурсов MVP (ADR-005, GDD §5.1). Числовые значения стабильны
/// и используются в сохранениях и JSON-контенте.
/// </summary>
public enum ResourceKind
{
    /// <summary>Золото — единая валюта найма, построек и выкупа.</summary>
    Gold = 0,

    /// <summary>Древесина — постройки и будущие осадные машины.</summary>
    Wood = 1,

    /// <summary>Руда — постройки и улучшения брони.</summary>
    Ore = 2,

    /// <summary>Кристаллы — заклинания, топ-тиры и улучшение поселения.</summary>
    Crystal = 3,

    /// <summary>Сера — огнестрельные юниты и огненные заклинания.</summary>
    Sulfur = 4,

    /// <summary>Ртуть — промоушены, алхимия и зачарование.</summary>
    Mercury = 5,

    /// <summary>Эссенция — исследования, навыки героев и ритуалы.</summary>
    Essence = 6
}
