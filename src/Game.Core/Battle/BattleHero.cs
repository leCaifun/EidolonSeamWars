using Game.Core.Domain;

namespace Game.Core.Battle;

/// <summary>
/// Герой стороны в бою: только числа и ID; на поле героя нет, он не атакуем и не занимает слот (ADR-007).
/// </summary>
/// <param name="HeroId">ID героя.</param>
/// <param name="Attack">Атака героя; прибавляется к атаке каждого стака армии.</param>
/// <param name="Defense">Защита героя; прибавляется к защите каждого стака армии.</param>
/// <param name="Spellpower">Чародейство (пока не масштабирует эффекты, ADR-043).</param>
/// <param name="Mana">Мана на бой: <c>10 + Знание × 5</c>.</param>
/// <param name="SpellId">Заклинание класса или <c>null</c>.</param>
/// <param name="CommandAbilityId">Командная способность класса или <c>null</c>.</param>
/// <param name="PassiveAbilityId">Классовая пассивная способность или <c>null</c>.</param>
public sealed record BattleHero(
    string HeroId,
    int Attack,
    int Defense,
    int Spellpower,
    int Mana,
    string? SpellId,
    string? CommandAbilityId,
    string? PassiveAbilityId)
{
    /// <summary>
    /// Собирает боевого героя из героя кампании.
    /// </summary>
    /// <param name="hero">Герой.</param>
    /// <param name="passiveAbilityId">Классовая пассивная способность (из контента класса).</param>
    /// <exception cref="ArgumentNullException">Герой не задан.</exception>
    public static BattleHero FromHero(Hero hero, string? passiveAbilityId)
    {
        ArgumentNullException.ThrowIfNull(hero);

        return new BattleHero(
            hero.Id,
            hero.Stats.Attack,
            hero.Stats.Defense,
            hero.Stats.Spellpower,
            hero.Mana,
            hero.HeroClass.SpellId,
            hero.HeroClass.CommandAbilityId,
            passiveAbilityId);
    }
}
