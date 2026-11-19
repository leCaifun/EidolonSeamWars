using Game.Core.Domain;

namespace Game.Core.Battle;

/// <summary>
/// Определение эффекта способности или заклинания из контента. Поля, не относящиеся к типу, равны 0 или <c>null</c>.
/// </summary>
/// <param name="Type">Тип эффекта (<see cref="BattleEffectTypes"/>).</param>
/// <param name="Value">Основное числовое значение.</param>
/// <param name="DamageType">Тип урона (для урона, иммунитета и сопротивления).</param>
/// <param name="Min">Минимум урона.</param>
/// <param name="Max">Максимум урона.</param>
/// <param name="MinHexes">Минимум пройденных гексов.</param>
/// <param name="MaxTier">Максимальный тир юнита.</param>
/// <param name="UnitId">ID юнита (призыв, поднятие нежити).</param>
/// <param name="Count">Количество.</param>
/// <param name="MaxCountPerStack">Максимум за побеждённый стак.</param>
/// <param name="DurationRounds">Длительность эффекта, раундов; 0 — берётся из способности.</param>
public sealed record EffectDef(
    string Type,
    int Value = 0,
    DamageType? DamageType = null,
    int Min = 0,
    int Max = 0,
    int MinHexes = 0,
    int MaxTier = 0,
    string? UnitId = null,
    int Count = 0,
    int MaxCountPerStack = 0,
    int DurationRounds = 0);
