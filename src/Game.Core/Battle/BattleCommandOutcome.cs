namespace Game.Core.Battle;

/// <summary>
/// Исход применения команды боя. Отказы не меняют состояние боя.
/// </summary>
public enum BattleCommandOutcome
{
    /// <summary>Команда применена.</summary>
    Applied = 0,

    /// <summary>Бой уже закончен.</summary>
    BattleOver = 1,

    /// <summary>Команда отдана не активному стаку.</summary>
    NotActiveUnit = 2,

    /// <summary>Целевой гекс вне поля.</summary>
    TargetOutOfField = 3,

    /// <summary>Целевой гекс занят препятствием или стаком.</summary>
    TargetBlocked = 4,

    /// <summary>До целевого гекса нет пути в пределах остатка очков движения.</summary>
    NoPath = 5,

    /// <summary>Ожидание сейчас запрещено.</summary>
    WaitNotAllowed = 6,

    /// <summary>Цель атаки не существует, уничтожена или своя.</summary>
    InvalidTarget = 7,

    /// <summary>Цель вне досягаемости: нет свободного соседнего гекса в пределах очков движения.</summary>
    OutOfReach = 8,

    /// <summary>Не хватает очков движения на атаку (стоимость атаки — <c>attackMovementCost</c>).</summary>
    NotEnoughMovement = 9,

    /// <summary>Способность не найдена, не принадлежит стаку или не является активной.</summary>
    UnknownAbility = 10,

    /// <summary>Способность на перезарядке.</summary>
    OnCooldown = 11,

    /// <summary>Эффект или цель способности пока не поддерживаются боевым движком.</summary>
    UnsupportedEffect = 12,

    /// <summary>Цель дальше дальности способности.</summary>
    OutOfRange = 13,

    /// <summary>У стороны нет героя или нужной способности героя.</summary>
    NoHero = 14,

    /// <summary>Заклинание в этом раунде уже брошено.</summary>
    SpellAlreadyCast = 15,

    /// <summary>Не хватает маны.</summary>
    NotEnoughMana = 16,

    /// <summary>Командная способность уже использована в этом бою.</summary>
    CommandAbilityAlreadyUsed = 17
}
