namespace Game.Core.Battle;

/// <summary>
/// Вычисленные входы для уровня морали армии (GDD §7.5). Строится
/// <see cref="MoraleSystem.BuildContext"/> по составу армии.
/// </summary>
/// <param name="HeroFactionOnly">В армии есть только юниты фракции героя — бонус «своя фракция».</param>
/// <param name="ForeignFactionCount">Число лишних фракций (кроме фракции героя либо кроме первой без героя).</param>
/// <param name="UndeadAmongLiving">Нежить сражается рядом с живыми юнитами.</param>
/// <param name="MarshalAura">Действует классовая аура Маршала.</param>
/// <param name="DefendingSettlement">Армия обороняет поселение.</param>
/// <param name="ActiveAllyStackDeaths">Число смертей союзных стаков, штраф от которых ещё действует.</param>
public readonly record struct MoraleContext(
    bool HeroFactionOnly,
    int ForeignFactionCount,
    bool UndeadAmongLiving,
    bool MarshalAura,
    bool DefendingSettlement,
    int ActiveAllyStackDeaths);
