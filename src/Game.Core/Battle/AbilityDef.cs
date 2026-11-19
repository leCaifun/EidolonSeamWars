namespace Game.Core.Battle;

/// <summary>
/// Определение способности (юнита, класса героя или командной) из контента.
/// </summary>
/// <param name="Id">ID способности.</param>
/// <param name="Kind">Вид: <c>passive</c>, <c>active</c> или <c>command</c>.</param>
/// <param name="Trigger">Триггер из контента (информационно).</param>
/// <param name="Target">Тип цели (<c>enemyStack</c>, <c>enemyArea</c>, <c>allyStack</c>, <c>aura</c>, …) или <c>null</c>.</param>
/// <param name="Range">Дальность в гексах; 0 — без ограничения.</param>
/// <param name="Radius">Радиус области в гексах.</param>
/// <param name="CooldownRounds">Перезарядка в раундах.</param>
/// <param name="DurationRounds">Длительность эффектов по умолчанию, раундов.</param>
/// <param name="UsesPerBattle">Использований за бой (командные способности).</param>
/// <param name="Effects">Эффекты в порядке применения.</param>
public sealed record AbilityDef(
    string Id,
    string Kind,
    string Trigger,
    string? Target,
    int Range,
    int Radius,
    int CooldownRounds,
    int DurationRounds,
    int UsesPerBattle,
    IReadOnlyList<EffectDef> Effects);
