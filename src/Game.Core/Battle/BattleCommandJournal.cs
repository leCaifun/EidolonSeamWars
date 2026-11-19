using System.Collections.ObjectModel;

namespace Game.Core.Battle;

/// <summary>
/// Журнал команд боя: применяет команды к <see cref="BattleState"/> и запоминает их вместе с
/// результатами. Сид боя и журнал полностью воспроизводят бой (инвариант 4, ADR-040).
/// </summary>
public sealed class BattleCommandJournal
{
    private readonly List<BattleCommand> _commands = new();
    private readonly List<BattleCommandResult> _results = new();
    private readonly ReadOnlyCollection<BattleCommand> _readOnlyCommands;
    private readonly ReadOnlyCollection<BattleCommandResult> _readOnlyResults;

    /// <summary>
    /// Создаёт пустой журнал.
    /// </summary>
    /// <param name="battleSeed">Сид стрима <c>battle</c>, с которым начат бой.</param>
    public BattleCommandJournal(ulong battleSeed)
    {
        BattleSeed = battleSeed;
        _readOnlyCommands = _commands.AsReadOnly();
        _readOnlyResults = _results.AsReadOnly();
    }

    /// <summary>Сид стрима <c>battle</c>.</summary>
    public ulong BattleSeed { get; }

    /// <summary>Применённые и отклонённые команды в порядке отправки.</summary>
    public IReadOnlyList<BattleCommand> Commands => _readOnlyCommands;

    /// <summary>Результаты команд в том же порядке.</summary>
    public IReadOnlyList<BattleCommandResult> Results => _readOnlyResults;

    /// <summary>
    /// Применяет команду и записывает её в журнал (отклонённые команды тоже записываются).
    /// </summary>
    /// <param name="state">Состояние боя.</param>
    /// <param name="command">Команда.</param>
    /// <exception cref="ArgumentNullException">Состояние или команда не заданы.</exception>
    public BattleCommandResult Apply(BattleState state, BattleCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        BattleCommandResult result = state.Apply(command);
        _commands.Add(command);
        _results.Add(result);
        return result;
    }

    /// <summary>
    /// Воспроизводит команды на новом состоянии боя (после <see cref="BattleState.Start"/>).
    /// </summary>
    /// <param name="state">Свежее состояние с тем же сидом и составом сторон.</param>
    /// <param name="commands">Команды журнала.</param>
    /// <returns>Результаты команд.</returns>
    /// <exception cref="ArgumentNullException">Аргумент не задан.</exception>
    public static IReadOnlyList<BattleCommandResult> Replay(BattleState state, IEnumerable<BattleCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(commands);

        List<BattleCommandResult> results = new();

        foreach (BattleCommand command in commands)
        {
            results.Add(state.Apply(command));
        }

        return results;
    }
}
