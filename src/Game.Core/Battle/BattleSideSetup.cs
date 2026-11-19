using Game.Core.Domain;

namespace Game.Core.Battle;

/// <summary>
/// Описание стороны перед боем: стаки в порядке слотов и входы морали армии.
/// </summary>
/// <param name="Stacks">Стаки по слотам; не пустой список.</param>
/// <param name="HeroFactionId">Фракция героя; <c>null</c> — армия без героя.</param>
/// <param name="MarshalAura">Действует аура Маршала.</param>
/// <param name="DefendingSettlement">Сторона обороняет поселение.</param>
/// <param name="Hero">Герой стороны (вне поля) или <c>null</c>.</param>
public sealed record BattleSideSetup(
    IReadOnlyList<Stack> Stacks,
    string? HeroFactionId = null,
    bool MarshalAura = false,
    bool DefendingSettlement = false,
    BattleHero? Hero = null);
