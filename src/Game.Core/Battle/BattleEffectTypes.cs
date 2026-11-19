namespace Game.Core.Battle;

/// <summary>
/// Типы эффектов и видов способностей, которые понимает боевой движок (этап 05g, ADR-043).
/// Тип, которого нет в списке, принимается из контента, но при использовании отклоняется
/// исходом <see cref="BattleCommandOutcome.UnsupportedEffect"/>; новые эффекты добавляются здесь
/// и в <c>BattleState</c>.
/// </summary>
public static class BattleEffectTypes
{
    /// <summary>Вид способности: пассивная.</summary>
    public const string KindPassive = "passive";

    /// <summary>Вид способности: активная юнита (кулдаун).</summary>
    public const string KindActive = "active";

    /// <summary>Вид способности: командная способность героя (1 за бой).</summary>
    public const string KindCommand = "command";

    /// <summary>Модификатор атаки на срок (бафф или дебафф).</summary>
    public const string AttackModifier = "attackModifier";

    /// <summary>Модификатор защиты на срок.</summary>
    public const string DefenseModifier = "defenseModifier";

    /// <summary>Модификатор инициативы на срок.</summary>
    public const string InitiativeModifier = "initiativeModifier";

    /// <summary>Бонус морали на срок (аура знамени).</summary>
    public const string MoraleBonus = "moraleBonus";

    /// <summary>Регенерация: лечение в начале каждого раунда.</summary>
    public const string Regeneration = "regeneration";

    /// <summary>Укоренение: стак не может перемещаться.</summary>
    public const string Root = "root";

    /// <summary>Прямой урон от способности или заклинания.</summary>
    public const string Damage = "damage";

    /// <summary>Мгновенное лечение.</summary>
    public const string Heal = "heal";

    /// <summary>Иммунитет к морали (обеспечивается классом юнита).</summary>
    public const string ImmuneMorale = "immuneMorale";

    /// <summary>Иммунитет к типу урона.</summary>
    public const string ImmuneDamageType = "immuneDamageType";

    /// <summary>Сопротивление типу урона, %.</summary>
    public const string DamageTypeResistance = "damageTypeResistance";

    /// <summary>Лечение долей нанесённого урона, %.</summary>
    public const string HealPercentOfDamage = "healPercentOfDamage";

    /// <summary>Бонус защиты при союзнике на соседнем гексе.</summary>
    public const string DefenseBonusIfAdjacentAlly = "defenseBonusIfAdjacentAlly";

    /// <summary>Игнорирование доли защиты цели, %.</summary>
    public const string IgnoreDefensePercent = "ignoreDefensePercent";

    /// <summary>Бонус урона, %, если стак прошёл не меньше гексов за ход.</summary>
    public const string DamageBonusPercentIfMovedHexes = "damageBonusPercentIfMovedHexes";

    /// <summary>Бонус шанса крита по раненой цели.</summary>
    public const string CritChanceBonusIfTargetWounded = "critChanceBonusIfTargetWounded";

    /// <summary>Бонус атаки за каждого соседнего союзника того же юнита.</summary>
    public const string AttackBonusPerAdjacentSameUnit = "attackBonusPerAdjacentSameUnit";

    /// <summary>Иммунитет к отталкиванию (в бою MVP отталкивания нет).</summary>
    public const string ImmunePush = "immunePush";

    /// <summary>Бонус инициативы юнитам не выше указанного тира.</summary>
    public const string InitiativeBonusIfTierAtMost = "initiativeBonusIfTierAtMost";

    /// <summary>Игнорирование штрафа местности (карта; в бою без эффекта).</summary>
    public const string IgnoreTerrainPenalty = "ignoreTerrainPenalty";

    /// <summary>Поднятие нежити после боя за побеждённые стаки врага.</summary>
    public const string RaiseUndeadPerDefeatedEnemyStack = "raiseUndeadPerDefeatedEnemyStack";

    private static readonly HashSet<string> ActiveSupported = new(StringComparer.Ordinal)
    {
        AttackModifier, DefenseModifier, InitiativeModifier, MoraleBonus, Regeneration, Root, Damage, Heal
    };

    private static readonly HashSet<string> PassiveSupported = new(StringComparer.Ordinal)
    {
        ImmuneMorale, ImmuneDamageType, DamageTypeResistance, HealPercentOfDamage, DefenseBonusIfAdjacentAlly,
        IgnoreDefensePercent, DamageBonusPercentIfMovedHexes, CritChanceBonusIfTargetWounded,
        AttackBonusPerAdjacentSameUnit, ImmunePush, InitiativeBonusIfTierAtMost, IgnoreTerrainPenalty,
        RaiseUndeadPerDefeatedEnemyStack, MoraleBonus
    };

    /// <summary>Может ли эффект применяться активной способностью или заклинанием.</summary>
    public static bool IsActiveSupported(string type)
    {
        return ActiveSupported.Contains(type);
    }

    /// <summary>Может ли эффект работать как пассивный.</summary>
    public static bool IsPassiveSupported(string type)
    {
        return PassiveSupported.Contains(type);
    }

    /// <summary>Длительный ли эффект (живёт раунды, а не срабатывает мгновенно).</summary>
    public static bool IsLasting(string type)
    {
        return type is AttackModifier or DefenseModifier or InitiativeModifier or MoraleBonus or Regeneration or Root;
    }
}
