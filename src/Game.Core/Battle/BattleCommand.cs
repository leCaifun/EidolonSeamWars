using Game.Core.Domain;

namespace Game.Core.Battle;

/// <summary>
/// Команда боя для активного стака. Создаётся фабричными методами и не содержит логики.
/// </summary>
public sealed class BattleCommand
{
    private BattleCommand(
        BattleCommandKind kind,
        string unitId,
        Hex? targetHex,
        string? targetUnitId = null,
        string? abilityId = null,
        BattleSide? side = null)
    {
        Kind = kind;
        UnitId = unitId;
        TargetHex = targetHex;
        TargetUnitId = targetUnitId;
        AbilityId = abilityId;
        Side = side;
    }

    /// <summary>Вид команды.</summary>
    public BattleCommandKind Kind { get; }

    /// <summary>ID стака, которому отдана команда.</summary>
    public string UnitId { get; }

    /// <summary>Целевой гекс (для перемещения), иначе <c>null</c>.</summary>
    public Hex? TargetHex { get; }

    /// <summary>ID способности (для способности юнита), иначе <c>null</c>.</summary>
    public string? AbilityId { get; }

    /// <summary>Сторона героя (для команд героя), иначе <c>null</c>.</summary>
    public BattleSide? Side { get; }

    /// <summary>
    /// Активная способность стака. Цель нужна для способностей с целью (<c>enemyStack</c>, <c>enemyArea</c>,
    /// <c>allyStack</c>), для ауры не нужна.
    /// </summary>
    /// <param name="unitId">ID стака.</param>
    /// <param name="abilityId">ID способности.</param>
    /// <param name="targetUnitId">ID стака-цели или <c>null</c>.</param>
    public static BattleCommand UseAbility(string unitId, string abilityId, string? targetUnitId = null)
    {
        return new BattleCommand(
            BattleCommandKind.UseAbility,
            RequireId(unitId),
            null,
            targetUnitId,
            RequireId(abilityId));
    }

    /// <summary>
    /// Заклинание героя стороны по стаку-цели (в свой ход, не чаще раза за раунд).
    /// </summary>
    /// <param name="side">Сторона героя.</param>
    /// <param name="targetUnitId">ID стака-цели.</param>
    public static BattleCommand CastSpell(BattleSide side, string targetUnitId)
    {
        return new BattleCommand(
            BattleCommandKind.CastSpell,
            HeroCommandActor(side),
            null,
            RequireId(targetUnitId),
            null,
            side);
    }

    /// <summary>Командная способность героя стороны (1 за бой).</summary>
    /// <param name="side">Сторона героя.</param>
    public static BattleCommand UseCommandAbility(BattleSide side)
    {
        return new BattleCommand(BattleCommandKind.UseCommandAbility, HeroCommandActor(side), null, null, null, side);
    }

    /// <summary>Служебный ID «исполнителя» команд героя: <c>hero:Attacker</c> / <c>hero:Defender</c>.</summary>
    public static string HeroCommandActor(BattleSide side)
    {
        return $"hero:{side}";
    }

    /// <summary>ID стака-цели (для атаки), иначе <c>null</c>.</summary>
    public string? TargetUnitId { get; }

    /// <summary>
    /// Атака вражеского стака. Если цель не рядом и стак не стрелок на дистанции, стак идёт по
    /// кратчайшему маршруту к свободному соседнему гексу и бьёт (ПКМ по врагу).
    /// </summary>
    /// <param name="unitId">ID атакующего стака.</param>
    /// <param name="targetUnitId">ID стака-цели.</param>
    public static BattleCommand Attack(string unitId, string targetUnitId)
    {
        return new BattleCommand(BattleCommandKind.Attack, RequireId(unitId), null, RequireId(targetUnitId));
    }

    /// <summary>Перемещение стака на гекс.</summary>
    /// <param name="unitId">ID стака.</param>
    /// <param name="target">Целевой гекс.</param>
    public static BattleCommand Move(string unitId, Hex target)
    {
        return new BattleCommand(BattleCommandKind.Move, RequireId(unitId), target);
    }

    /// <summary>Ожидание.</summary>
    /// <param name="unitId">ID стака.</param>
    public static BattleCommand Wait(string unitId)
    {
        return new BattleCommand(BattleCommandKind.Wait, RequireId(unitId), null);
    }

    /// <summary>Защита.</summary>
    /// <param name="unitId">ID стака.</param>
    public static BattleCommand Defend(string unitId)
    {
        return new BattleCommand(BattleCommandKind.Defend, RequireId(unitId), null);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (AbilityId is not null)
        {
            return $"BattleCommand({Kind}, {UnitId}, {AbilityId} -> {TargetUnitId ?? "-"})";
        }

        if (TargetUnitId is not null)
        {
            return $"BattleCommand({Kind}, {UnitId} -> {TargetUnitId})";
        }

        return TargetHex is null
            ? $"BattleCommand({Kind}, {UnitId})"
            : $"BattleCommand({Kind}, {UnitId}, {TargetHex})";
    }

    private static string RequireId(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            throw new ArgumentException("ID стака в команде не может быть пустым.", nameof(unitId));
        }

        return unitId;
    }
}
