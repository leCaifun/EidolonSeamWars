namespace Game.Core.Map;

/// <summary>
/// Состояние гекса в тумане войны для конкретной фракции (GDD §4):
/// скрытый, исследованный или видимый сейчас.
/// </summary>
public enum FogState
{
    /// <summary>Гекс никогда не наблюдался.</summary>
    Hidden = 0,

    /// <summary>Гекс наблюдался ранее, но сейчас вне обзора.</summary>
    Explored = 1,

    /// <summary>Гекс виден в текущий момент.</summary>
    Visible = 2
}
