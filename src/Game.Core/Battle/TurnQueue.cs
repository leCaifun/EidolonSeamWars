using Game.Core.Diagnostics;

namespace Game.Core.Battle;

/// <summary>
/// Очередь хода раунда по GDD §7.3 и ADR-038. Раунд состоит из двух групп: A — не ожидавшие в
/// прошлом раунде, B — ожидавшие; внутри группы порядок по убыванию инициативы, при равной
/// инициативе стороны чередуются (первым — атакующий), затем индекс слота. Ожидание переносит
/// ход в конец текущего раунда; повторное ожидание в том же раунде запрещено. Дополнительное
/// действие морали добавляется в конец очереди.
/// </summary>
public sealed class TurnQueue
{
    private readonly List<TurnEntry> _entries = new();
    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт пустую очередь; раунды начинаются <see cref="StartRound"/>.
    /// </summary>
    /// <param name="logger">Необязательный журнал; категория <c>Battle</c>.</param>
    public TurnQueue(IGameLogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>Номер текущего раунда; 0 до первого <see cref="StartRound"/>.</summary>
    public int Round { get; private set; }

    /// <summary>Текущая запись очереди; <c>null</c>, если раунд завершён.</summary>
    public TurnEntry? Current
    {
        get
        {
            PruneDeadFront();
            return _entries.Count == 0 ? null : _entries[0];
        }
    }

    /// <summary>Все ли ходы раунда сыграны.</summary>
    public bool IsRoundComplete
    {
        get
        {
            PruneDeadFront();
            return _entries.Count == 0;
        }
    }

    /// <summary>
    /// Начинает новый раунд: сбрасывает раундовые флаги живых стаков и строит группы A и B.
    /// </summary>
    /// <param name="units">Все стаки боя; погибшие пропускаются.</param>
    /// <exception cref="ArgumentNullException">Список не задан.</exception>
    /// <exception cref="ArgumentException">В списке повторяется ID стака.</exception>
    public void StartRound(IEnumerable<BattleUnit> units)
    {
        ArgumentNullException.ThrowIfNull(units);

        List<BattleUnit> alive = units.Where(unit => unit.IsAlive).ToList();
        HashSet<string> ids = new(StringComparer.Ordinal);

        foreach (BattleUnit unit in alive)
        {
            if (!ids.Add(unit.Id))
            {
                throw new ArgumentException($"ID стака «{unit.Id}» повторяется в очереди.", nameof(units));
            }
        }

        foreach (BattleUnit unit in alive)
        {
            unit.BeginRound();
        }

        Round = checked(Round + 1);
        _entries.Clear();

        AddGroup(alive.Where(unit => !unit.WaitedLastRound));
        AddGroup(alive.Where(unit => unit.WaitedLastRound));

        if (_logger is not null && _logger.IsEnabled(LogLevel.Debug))
        {
            _logger.Log(LogEvent.Create(
                LogLevel.Debug,
                "Battle",
                "Построена очередь раунда.",
                fields: new Dictionary<string, string>
                {
                    ["round"] = Round.ToString(),
                    ["order"] = string.Join(",", _entries.Select(entry => entry.Unit.Id))
                }));
        }
    }

    /// <summary>Записи, которые ещё предстоит сыграть в раунде (только живые стаки).</summary>
    public IReadOnlyList<TurnEntry> GetRemaining()
    {
        return _entries.Where(entry => entry.Unit.IsAlive).ToList();
    }

    /// <summary>
    /// Завершает текущий ход (действие, защита, пропуск по морали) и убирает первую запись очереди.
    /// Запись снимается даже если стак погиб во время хода (контратака); мёртвые записи в начале
    /// очереди отбрасывает только обращение к <see cref="Current"/>, поэтому между началом хода и
    /// <see cref="CompleteTurn"/> к <see cref="Current"/> обращаться не нужно.
    /// </summary>
    /// <exception cref="InvalidOperationException">Очередь пуста.</exception>
    public void CompleteTurn()
    {
        if (_entries.Count == 0)
        {
            throw new InvalidOperationException("Очередь хода пуста: раунд завершён.");
        }

        _entries.RemoveAt(0);
    }

    /// <summary>
    /// Можно ли ожидать: текущий ход не дополнительное действие и стак ещё не ожидал в этом раунде.
    /// </summary>
    public bool CanWait()
    {
        TurnEntry? current = Current;
        return current.HasValue
            && !current.Value.IsExtraAction
            && !current.Value.Unit.WaitedThisRound;
    }

    /// <summary>
    /// Ожидание: переносит ход текущего стака в конец раунда и помечает ожидание
    /// (в следующем раунде стак попадёт в группу B).
    /// </summary>
    /// <exception cref="InvalidOperationException">Ожидание сейчас запрещено.</exception>
    public void Wait()
    {
        if (!CanWait())
        {
            throw new InvalidOperationException(
                "Ожидание запрещено: очередь пуста, ход дополнительный или стак уже ожидал в этом раунде.");
        }

        TurnEntry entry = _entries[0];
        _entries.RemoveAt(0);
        entry.Unit.MarkWaited();
        _entries.Add(entry);

        _logger?.Log(LogEvent.Create(
            LogLevel.Debug,
            "Battle",
            "Стак ожидает: ход перенесён в конец раунда.",
            fields: new Dictionary<string, string>
            {
                ["unitId"] = entry.Unit.Id,
                ["round"] = Round.ToString()
            }));
    }

    /// <summary>
    /// Добавляет дополнительное действие морали в конец очереди и учитывает его у стака.
    /// </summary>
    /// <param name="unit">Стак, получивший дополнительное действие.</param>
    /// <exception cref="ArgumentNullException">Стак не задан.</exception>
    /// <exception cref="InvalidOperationException">Стак уничтожен.</exception>
    public void AppendExtraAction(BattleUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        if (!unit.IsAlive)
        {
            throw new InvalidOperationException($"Стак «{unit.Id}» уничтожен и не может получить действие.");
        }

        unit.RegisterExtraAction();
        _entries.Add(new TurnEntry(unit, true));

        _logger?.Log(LogEvent.Create(
            LogLevel.Debug,
            "Battle",
            "Дополнительное действие морали добавлено в конец очереди.",
            fields: new Dictionary<string, string>
            {
                ["unitId"] = unit.Id,
                ["round"] = Round.ToString()
            }));
    }

    private void AddGroup(IEnumerable<BattleUnit> group)
    {
        foreach (IGrouping<int, BattleUnit> bucket in group
                     .GroupBy(unit => unit.Initiative)
                     .OrderByDescending(grouping => grouping.Key))
        {
            Queue<BattleUnit> attackers = OrderedSide(bucket, BattleSide.Attacker);
            Queue<BattleUnit> defenders = OrderedSide(bucket, BattleSide.Defender);
            bool attackerTurn = true;

            while (attackers.Count > 0 || defenders.Count > 0)
            {
                Queue<BattleUnit> source = attackerTurn ? attackers : defenders;

                if (source.Count == 0)
                {
                    source = attackerTurn ? defenders : attackers;
                }

                _entries.Add(new TurnEntry(source.Dequeue(), false));
                attackerTurn = !attackerTurn;
            }
        }
    }

    private static Queue<BattleUnit> OrderedSide(IEnumerable<BattleUnit> bucket, BattleSide side)
    {
        return new Queue<BattleUnit>(bucket
            .Where(unit => unit.Side == side)
            .OrderBy(unit => unit.SlotIndex)
            .ThenBy(unit => unit.Id, StringComparer.Ordinal));
    }

    private void PruneDeadFront()
    {
        while (_entries.Count > 0 && !_entries[0].Unit.IsAlive)
        {
            _entries.RemoveAt(0);
        }
    }
}
