namespace Game.Core.Battle;

/// <summary>
/// Результат применения команды боя.
/// </summary>
public sealed class BattleCommandResult
{
    /// <summary>
    /// Создаёт результат.
    /// </summary>
    /// <param name="outcome">Исход.</param>
    /// <param name="unitId">ID стака, которому была отдана команда.</param>
    public BattleCommandResult(BattleCommandOutcome outcome, string unitId)
    {
        Outcome = outcome;
        UnitId = unitId;
    }

    /// <summary>Исход команды.</summary>
    public BattleCommandOutcome Outcome { get; }

    /// <summary>ID стака, которому была отдана команда.</summary>
    public string UnitId { get; }

    /// <summary>Применена ли команда.</summary>
    public bool IsApplied => Outcome == BattleCommandOutcome.Applied;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"BattleCommandResult({Outcome}, {UnitId})";
    }
}
