namespace Game.Core.Battle;

/// <summary>
/// Определение заклинания героя из контента. Сила магии пока не масштабирует эффекты:
/// формулы GDD нет (ADR-043).
/// </summary>
/// <param name="Id">ID заклинания.</param>
/// <param name="School">Школа магии.</param>
/// <param name="ManaCost">Стоимость в мане.</param>
/// <param name="Target">Тип цели (<c>allyStack</c> или <c>enemyStack</c>).</param>
/// <param name="DurationRounds">Длительность эффектов по умолчанию, раундов.</param>
/// <param name="Effects">Эффекты в порядке применения.</param>
public sealed record SpellDef(
    string Id,
    string School,
    int ManaCost,
    string Target,
    int DurationRounds,
    IReadOnlyList<EffectDef> Effects);
