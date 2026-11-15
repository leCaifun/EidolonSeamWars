namespace Game.Core.Heroes;

/// <summary>
/// Исход попытки побега героя (GDD §7.7, ADR-012).
/// </summary>
public enum EscapeResult
{
    /// <summary>Побег успешен: герой уходит без армии и теряет часть артефактов.</summary>
    Escaped = 0,

    /// <summary>Побег провален: герой попал в плен на срок <see cref="EscapeRules.CaptivityDays"/>.</summary>
    Captured = 1,

    /// <summary>Побег запрещён: защитник осады не может бежать.</summary>
    Forbidden = 2
}
