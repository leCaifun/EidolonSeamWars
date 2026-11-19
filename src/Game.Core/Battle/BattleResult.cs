namespace Game.Core.Battle;

/// <summary>
/// Итог завершённого боя: победитель, число раундов и состояние каждого стака.
/// Не содержит последствий для героев (побег, плен) — их считает <see cref="BattleAftermathResolver"/>.
/// </summary>
public sealed class BattleResult
{
    private readonly List<BattleStackResult> _stacks;
    private readonly List<BattleRaisedUnit> _raised;

    /// <summary>
    /// Создаёт итог боя.
    /// </summary>
    /// <param name="status">Статус завершённого боя (не <see cref="BattleStatus.Ongoing"/>).</param>
    /// <param name="rounds">Число сыгранных раундов.</param>
    /// <param name="stacks">Итоги стаков в порядке боя.</param>
    /// <param name="raisedUnits">Юниты, поднятые классовыми способностями после боя.</param>
    /// <exception cref="ArgumentException">Бой не завершён.</exception>
    /// <exception cref="ArgumentNullException">Список стаков не задан.</exception>
    public BattleResult(
        BattleStatus status,
        int rounds,
        IEnumerable<BattleStackResult> stacks,
        IEnumerable<BattleRaisedUnit>? raisedUnits = null)
    {
        ArgumentNullException.ThrowIfNull(stacks);

        if (status == BattleStatus.Ongoing)
        {
            throw new ArgumentException("Итог можно построить только для завершённого боя.", nameof(status));
        }

        Status = status;
        Rounds = rounds;
        _stacks = stacks.ToList();
        _raised = raisedUnits?.ToList() ?? new List<BattleRaisedUnit>();
    }

    /// <summary>Статус боя.</summary>
    public BattleStatus Status { get; }

    /// <summary>Число сыгранных раундов.</summary>
    public int Rounds { get; }

    /// <summary>Итоги всех стаков.</summary>
    public IReadOnlyList<BattleStackResult> Stacks => _stacks;

    /// <summary>Юниты, поднятые после боя классовыми способностями победителя.</summary>
    public IReadOnlyList<BattleRaisedUnit> RaisedUnits => _raised;

    /// <summary>Итоги стаков стороны.</summary>
    public IEnumerable<BattleStackResult> GetStacks(BattleSide side)
    {
        return _stacks.Where(stack => stack.Side == side);
    }

    /// <summary>Победила ли сторона (при ничьей — никто).</summary>
    public bool IsVictory(BattleSide side)
    {
        return side == BattleSide.Attacker
            ? Status == BattleStatus.AttackerWon
            : Status == BattleStatus.DefenderWon;
    }

    /// <summary>Сколько юнитов потеряла сторона.</summary>
    public int UnitsLost(BattleSide side)
    {
        return GetStacks(side).Sum(stack => stack.Lost);
    }

    /// <summary>Сколько стаков стороны выжило.</summary>
    public int SurvivingStacks(BattleSide side)
    {
        return GetStacks(side).Count(stack => stack.Survived);
    }
}
