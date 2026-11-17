using System.Collections.ObjectModel;

namespace Game.Core.Map;

/// <summary>
/// Журнал команд карты: мастер-сид и последовательность команд с результатами.
/// Журнал вместе с мастер-сидом полностью определяет ход карты (ADR-018)
/// и пригоден для воспроизведения багов и будущих реплеев.
/// </summary>
public sealed class MapCommandJournal
{
    private readonly List<MapCommand> _commands = new();
    private readonly List<MapCommandResult> _results = new();
    private readonly ReadOnlyCollection<MapCommand> _readOnlyCommands;
    private readonly ReadOnlyCollection<MapCommandResult> _readOnlyResults;

    /// <summary>
    /// Создаёт пустой журнал для партии.
    /// </summary>
    /// <param name="masterSeed">Мастер-сид партии.</param>
    public MapCommandJournal(ulong masterSeed)
    {
        MasterSeed = masterSeed;
        _readOnlyCommands = _commands.AsReadOnly();
        _readOnlyResults = _results.AsReadOnly();
    }

    /// <summary>Мастер-сид партии.</summary>
    public ulong MasterSeed { get; }

    /// <summary>Применённые команды в порядке применения.</summary>
    public IReadOnlyList<MapCommand> Commands => _readOnlyCommands;

    /// <summary>Результаты команд в порядке применения.</summary>
    public IReadOnlyList<MapCommandResult> Results => _readOnlyResults;

    /// <summary>
    /// Применяет команду к состоянию и записывает её в журнал.
    /// </summary>
    /// <param name="state">Состояние карты.</param>
    /// <param name="command">Команда.</param>
    /// <exception cref="ArgumentNullException">Состояние или команда не заданы.</exception>
    public MapCommandResult Apply(MapState state, MapCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        MapCommandResult result = state.Apply(command);
        _commands.Add(command);
        _results.Add(result);
        return result;
    }
}
