using Game.Core.Diagnostics;
using Game.Core.Domain;
using Game.Core.Numerics;
using Game.Core.Rng;

namespace Game.Core.Battle;

/// <summary>
/// Состояние пошагового боя 13×9 (GDD §7): стаки двух сторон, очередь хода, мораль в начале хода
/// и команды активного стака (перемещение, ожидание, защита, атака) с автоматической контратакой.
/// Герой, способности, перенос результата в армии и реплей добавляются на этапе 05f.
/// Все случайности идут через стрим <c>battle</c>.
/// </summary>
/// <remarks>
/// Расстановка (ADR-039): слот N занимает строку N, атакующий — колонку 0, защитник — последнюю;
/// слоты сверх высоты поля уходят во вторую колонку от края. Ход (ADR-040): скорость — запас очков
/// движения на ход; перемещение тратит очки и ход не завершает, атака завершает ход.
/// </remarks>
public sealed class BattleState
{
    private const int MaxAdvanceIterations = 100_000;

    private readonly MoraleSystem _morale;
    private readonly SeededRandom _random;
    private readonly IGameLogger? _logger;
    private readonly List<BattleUnit> _units = new();
    private readonly Dictionary<BattleSide, BattleSideSetup> _setups = new();
    private readonly Dictionary<BattleSide, List<int>> _allyDeathRounds = new();
    private bool _started;
    private bool _committed;
    private readonly Dictionary<BattleSide, int> _mana = new();
    private readonly Dictionary<BattleSide, int> _spellCastRound = new();
    private readonly HashSet<BattleSide> _commandUsed = new();
    private BattleCatalog _catalog = BattleCatalog.Empty;

    private BattleState(
        BattleField field,
        BattleRules rules,
        MoraleSystem morale,
        DamageCalculator damage,
        SeededRandom random,
        IGameLogger? logger)
    {
        Field = field;
        Rules = rules;
        _morale = morale;
        Damage = damage;
        _random = random;
        _logger = logger;
        Queue = new TurnQueue(logger);
        _allyDeathRounds[BattleSide.Attacker] = new List<int>();
        _allyDeathRounds[BattleSide.Defender] = new List<int>();
    }

    /// <summary>Поле боя.</summary>
    public BattleField Field { get; }

    /// <summary>Боевые правила.</summary>
    public BattleRules Rules { get; }

    /// <summary>Калькулятор урона (используется командами атаки).</summary>
    public DamageCalculator Damage { get; }

    /// <summary>Очередь хода.</summary>
    public TurnQueue Queue { get; }

    /// <summary>Все стаки боя (включая погибших) в порядке создания.</summary>
    public IReadOnlyList<BattleUnit> Units => _units;

    /// <summary>Состояние боя.</summary>
    public BattleStatus Status { get; private set; } = BattleStatus.Ongoing;

    /// <summary>Закончен ли бой.</summary>
    public bool IsOver => Status != BattleStatus.Ongoing;

    /// <summary>Номер текущего раунда.</summary>
    public int Round => Queue.Round;

    /// <summary>Стак, чей ход идёт сейчас; <c>null</c>, если бой не начат или закончен.</summary>
    public BattleUnit? ActiveUnit => IsOver ? null : Queue.Current?.Unit;

    /// <summary>
    /// Создаёт бой и расставляет стаки по слотам.
    /// </summary>
    /// <param name="field">Поле боя.</param>
    /// <param name="rules">Боевые правила.</param>
    /// <param name="morale">Система морали.</param>
    /// <param name="damage">Калькулятор урона.</param>
    /// <param name="random">Стрим <c>battle</c>.</param>
    /// <param name="attacker">Атакующая сторона.</param>
    /// <param name="defender">Сторона защитника.</param>
    /// <param name="logger">Необязательный журнал.</param>
    /// <param name="catalog">Справочник способностей и заклинаний; без него способности недоступны.</param>
    /// <exception cref="ArgumentNullException">Обязательный аргумент не задан.</exception>
    /// <exception cref="ArgumentException">Сторона пуста или не помещается на поле.</exception>
    /// <exception cref="InvalidOperationException">Стартовый гекс занят препятствием.</exception>
    public static BattleState Create(
        BattleField field,
        BattleRules rules,
        MoraleSystem morale,
        DamageCalculator damage,
        SeededRandom random,
        BattleSideSetup attacker,
        BattleSideSetup defender,
        IGameLogger? logger = null,
        BattleCatalog? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(morale);
        ArgumentNullException.ThrowIfNull(damage);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(defender);

        BattleState state = new(field, rules, morale, damage, random, logger);
        state._catalog = catalog ?? BattleCatalog.Empty;
        // Копия списка: армия может менять слоты после боя, индексы слотов должны остаться стабильными.
        state._setups[BattleSide.Attacker] = attacker with { Stacks = attacker.Stacks.ToList() };
        state._setups[BattleSide.Defender] = defender with { Stacks = defender.Stacks.ToList() };
        state.Deploy(BattleSide.Attacker, attacker);
        state.Deploy(BattleSide.Defender, defender);
        state.ApplyHeroBonuses(BattleSide.Attacker);
        state.ApplyHeroBonuses(BattleSide.Defender);
        return state;
    }

    /// <summary>
    /// Начинает бой: строит очередь первого раунда и начинает ход первого стака.
    /// </summary>
    /// <exception cref="InvalidOperationException">Бой уже начат.</exception>
    public void Start()
    {
        if (_started)
        {
            throw new InvalidOperationException("Бой уже начат.");
        }

        _started = true;

        _logger?.Log(LogEvent.Create(
            LogLevel.Info,
            "Battle",
            "Бой начат.",
            fields: new Dictionary<string, string>
            {
                ["attackerStacks"] = _units.Count(u => u.Side == BattleSide.Attacker).ToString(),
                ["defenderStacks"] = _units.Count(u => u.Side == BattleSide.Defender).ToString(),
                ["field"] = $"{Field.Width}x{Field.Height}"
            }));

        AdvanceTurn();
    }

    /// <summary>
    /// Каноническая строка состояния боя (раунд, статус, активный стак, все стаки) для сверки реплеев
    /// и тестов детерминизма. Формат стабилен, числа в инвариантной культуре.
    /// </summary>
    public string CreateSnapshot()
    {
        List<string> parts = new()
        {
            FormattableString.Invariant($"round={Round}"),
            FormattableString.Invariant($"status={Status}"),
            FormattableString.Invariant($"active={ActiveUnit?.Id ?? "-"}")
        };

        foreach (BattleUnit unit in _units)
        {
            parts.Add(FormattableString.Invariant(
                $"{unit.Id}@{unit.Position.Q},{unit.Position.R}:n={unit.Count}:top={unit.TopUnitHealth}:kills={unit.KilledStacks}:mp={unit.MovementPointsRemaining}:def={(unit.IsDefending ? 1 : 0)}:waited={(unit.WaitedThisRound ? 1 : 0)}"));
        }

        return string.Join("|", parts);
    }

    /// <summary>
    /// Строит итог завершённого боя.
    /// </summary>
    /// <exception cref="InvalidOperationException">Бой ещё идёт.</exception>
    public BattleResult CreateResult()
    {
        if (!IsOver)
        {
            throw new InvalidOperationException("Итог боя недоступен: бой ещё идёт.");
        }

        return new BattleResult(
            Status,
            Round,
            _units.Select(unit => new BattleStackResult(
                unit.Id,
                unit.Side,
                unit.SlotIndex,
                unit.Unit.Id,
                unit.InitialCount,
                unit.Count,
                unit.KilledStacks - unit.InitialKilledStacks)),
            CalculateRaisedUnits());
    }

    /// <summary>
    /// Нежить, поднятая классовой способностью победившего героя: за каждый уничтоженный стак врага
    /// не больше <c>maxCountPerStack</c> юнитов и не больше начального размера стака.
    /// </summary>
    private IEnumerable<BattleRaisedUnit> CalculateRaisedUnits()
    {
        foreach (BattleSide side in new[] { BattleSide.Attacker, BattleSide.Defender })
        {
            bool won = side == BattleSide.Attacker
                ? Status == BattleStatus.AttackerWon
                : Status == BattleStatus.DefenderWon;
            AbilityDef? passive = _catalog.FindAbility(_setups[side].Hero?.PassiveAbilityId);

            if (!won || passive is null || passive.Kind != BattleEffectTypes.KindPassive)
            {
                continue;
            }

            foreach (EffectDef effect in passive.Effects)
            {
                if (effect.Type != BattleEffectTypes.RaiseUndeadPerDefeatedEnemyStack || effect.UnitId is null)
                {
                    continue;
                }

                int raised = _units
                    .Where(unit => unit.Side != side && !unit.IsAlive)
                    .Sum(unit => Math.Min(effect.MaxCountPerStack, unit.InitialCount));

                if (raised > 0)
                {
                    yield return new BattleRaisedUnit(side, effect.UnitId, raised);
                }
            }
        }
    }

    /// <summary>
    /// Переносит итог боя в исходные стаки: убирает потери, добавляет победы (ветеранство) и удаляет
    /// пустые стаки из переданных армий. Ранения верхнего юнита не сохраняются. Вызывается один раз.
    /// </summary>
    /// <param name="attackerArmy">Армия атакующего, из слотов которой собран состав; можно не передавать.</param>
    /// <param name="defenderArmy">Армия защитника; можно не передавать.</param>
    /// <returns>Итог боя.</returns>
    /// <exception cref="InvalidOperationException">Бой не закончен, итог уже перенесён или исходный стак менялся во время боя.</exception>
    public BattleResult CommitResult(Army? attackerArmy = null, Army? defenderArmy = null)
    {
        BattleResult result = CreateResult();

        if (_committed)
        {
            throw new InvalidOperationException("Итог боя уже перенесён в армии.");
        }

        foreach (BattleUnit unit in _units)
        {
            Stack source = _setups[unit.Side].Stacks[unit.SlotIndex];

            if (source.Count != unit.InitialCount)
            {
                throw new InvalidOperationException(
                    $"Исходный стак «{unit.Id}» изменился во время боя: было {unit.InitialCount}, стало {source.Count}.");
            }
        }

        foreach (BattleUnit unit in _units)
        {
            Stack source = _setups[unit.Side].Stacks[unit.SlotIndex];
            source.TakeCasualties(unit.InitialCount - unit.Count);
            source.RegisterKilledStacks(unit.KilledStacks - unit.InitialKilledStacks);
        }

        RemoveEmptyStacks(attackerArmy);
        RemoveEmptyStacks(defenderArmy);
        _committed = true;

        _logger?.Log(LogEvent.Create(
            LogLevel.Info,
            "Battle",
            "Итог боя перенесён в армии.",
            fields: new Dictionary<string, string>
            {
                ["status"] = result.Status.ToString(),
                ["attackerLost"] = result.UnitsLost(BattleSide.Attacker).ToString(),
                ["defenderLost"] = result.UnitsLost(BattleSide.Defender).ToString(),
                ["rounds"] = result.Rounds.ToString()
            }));

        return result;
    }

    private static void RemoveEmptyStacks(Army? army)
    {
        if (army is null)
        {
            return;
        }

        for (int index = army.Slots.Count - 1; index >= 0; index--)
        {
            if (army.Slots[index].IsEmpty)
            {
                army.RemoveAt(index);
            }
        }
    }

    /// <summary>Находит стак по ID.</summary>
    /// <returns>Стак или <c>null</c>.</returns>
    public BattleUnit? FindUnit(string unitId)
    {
        return _units.FirstOrDefault(unit => string.Equals(unit.Id, unitId, StringComparison.Ordinal));
    }

    /// <summary>Живой стак на гексе или <c>null</c>.</summary>
    public BattleUnit? GetUnitAt(Hex hex)
    {
        return _units.FirstOrDefault(unit => unit.IsAlive && unit.Position == hex);
    }

    /// <summary>
    /// Текущий уровень морали стороны: источники по живому составу армии и действующим штрафам
    /// за смерть союзных стаков.
    /// </summary>
    /// <param name="side">Сторона.</param>
    public int GetMoraleLevel(BattleSide side)
    {
        BattleSideSetup setup = _setups[side];
        IEnumerable<UnitDef> defs = _units
            .Where(unit => unit.IsAlive && unit.Side == side)
            .Select(unit => unit.Unit);

        MoraleContext context = _morale.BuildContext(
            defs,
            setup.HeroFactionId,
            setup.MarshalAura || ClassPassiveHasEffect(side, BattleEffectTypes.MoraleBonus),
            setup.DefendingSettlement,
            CountActiveAllyDeaths(side));

        return _morale.ComputeLevel(context);
    }

    /// <summary>
    /// Уровень морали стака: мораль армии плюс бонусы эффектов (аура знамени), 0 для иммунных.
    /// </summary>
    private int GetUnitMoraleLevel(BattleUnit unit)
    {
        return _morale.GetEffectiveLevel(
            unit.Unit,
            GetMoraleLevel(unit.Side) + unit.GetEffectSum(BattleEffectTypes.MoraleBonus));
    }

    /// <summary>Остаток маны героя стороны; 0, если героя нет.</summary>
    /// <param name="side">Сторона.</param>
    public int GetMana(BattleSide side)
    {
        return _mana.TryGetValue(side, out int mana) ? mana : 0;
    }

    /// <summary>Использована ли командная способность героя стороны в этом бою.</summary>
    /// <param name="side">Сторона.</param>
    public bool IsCommandAbilityUsed(BattleSide side)
    {
        return _commandUsed.Contains(side);
    }

    /// <summary>
    /// Фиксирует гибель стака стороны: штраф морали действует
    /// <see cref="MoraleSystem.AllyStackDeathDurationRounds"/> раундов, считая текущий.
    /// </summary>
    /// <param name="side">Сторона, потерявшая стак.</param>
    public void NotifyStackDestroyed(BattleSide side)
    {
        _allyDeathRounds[side].Add(Queue.Round);
    }

    /// <summary>
    /// Гексы, куда стак может прийти в пределах остатка очков движения
    /// (<see cref="BattleUnit.MovementPointsRemaining"/>).
    /// </summary>
    /// <param name="unit">Стак.</param>
    /// <returns>Гекс → число шагов; стартовый гекс не входит.</returns>
    /// <exception cref="ArgumentNullException">Стак не задан.</exception>
    public IReadOnlyDictionary<Hex, int> ComputeReachable(BattleUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return ComputeReachable(unit, unit.MovementPointsRemaining);
    }

    /// <summary>
    /// Гексы, куда стак может прийти не более чем за <paramref name="maxSteps"/> шагов: препятствия и
    /// чужие стаки обходятся, летающие проходят над ними, но останавливаться на занятом гексе нельзя.
    /// </summary>
    /// <param name="unit">Стак.</param>
    /// <param name="maxSteps">Бюджет шагов; отрицательный — только стартовый гекс.</param>
    /// <returns>Гекс → число шагов; стартовый гекс не входит.</returns>
    /// <exception cref="ArgumentNullException">Стак не задан.</exception>
    public IReadOnlyDictionary<Hex, int> ComputeReachable(BattleUnit unit, int maxSteps)
    {
        ArgumentNullException.ThrowIfNull(unit);

        if (unit.HasEffect(BattleEffectTypes.Root))
        {
            return new Dictionary<Hex, int>();
        }

        bool flyer = unit.Unit.MovementType == MovementType.Flyer;
        HashSet<Hex> occupied = _units
            .Where(other => other.IsAlive && !ReferenceEquals(other, unit))
            .Select(other => other.Position)
            .ToHashSet();

        Dictionary<Hex, int> steps = new() { [unit.Position] = 0 };
        System.Collections.Generic.Queue<Hex> frontier = new();
        frontier.Enqueue(unit.Position);

        while (frontier.Count > 0)
        {
            Hex current = frontier.Dequeue();

            if (steps[current] >= maxSteps)
            {
                continue;
            }

            foreach (Hex neighbor in current.GetNeighbors())
            {
                if (!Field.Contains(neighbor) || steps.ContainsKey(neighbor))
                {
                    continue;
                }

                bool blocked = Field.IsObstacle(neighbor) || occupied.Contains(neighbor);

                if (blocked && !flyer)
                {
                    continue;
                }

                steps[neighbor] = steps[current] + 1;
                frontier.Enqueue(neighbor);
            }
        }

        Dictionary<Hex, int> reachable = new();

        foreach (KeyValuePair<Hex, int> pair in steps)
        {
            if (pair.Key != unit.Position && !Field.IsObstacle(pair.Key) && !occupied.Contains(pair.Key))
            {
                reachable[pair.Key] = pair.Value;
            }
        }

        return reachable;
    }

    /// <summary>
    /// Применяет команду к активному стаку. Отказ не меняет состояние боя.
    /// </summary>
    /// <param name="command">Команда.</param>
    /// <exception cref="ArgumentNullException">Команда не задана.</exception>
    /// <exception cref="InvalidOperationException">Бой не начат.</exception>
    public BattleCommandResult Apply(BattleCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_started)
        {
            throw new InvalidOperationException("Бой не начат: вызовите Start().");
        }

        if (IsOver)
        {
            return Reject(BattleCommandOutcome.BattleOver, command);
        }

        if (command.Kind == BattleCommandKind.CastSpell)
        {
            return ApplyCastSpell(command);
        }

        if (command.Kind == BattleCommandKind.UseCommandAbility)
        {
            return ApplyCommandAbility(command);
        }

        BattleUnit? active = ActiveUnit;

        if (active is null || !string.Equals(active.Id, command.UnitId, StringComparison.Ordinal))
        {
            return Reject(BattleCommandOutcome.NotActiveUnit, command);
        }

        return command.Kind switch
        {
            BattleCommandKind.Move => ApplyMove(active, command),
            BattleCommandKind.Wait => ApplyWait(active, command),
            BattleCommandKind.Defend => ApplyDefend(active, command),
            BattleCommandKind.Attack => ApplyAttack(active, command),
            BattleCommandKind.UseAbility => ApplyUseAbility(active, command),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command.Kind, "Неизвестный вид команды боя.")
        };
    }

    private BattleCommandResult ApplyMove(BattleUnit unit, BattleCommand command)
    {
        Hex target = command.TargetHex
            ?? throw new ArgumentException("У команды перемещения нет целевого гекса.", nameof(command));

        if (!Field.Contains(target))
        {
            return Reject(BattleCommandOutcome.TargetOutOfField, command);
        }

        if (Field.IsObstacle(target) || GetUnitAt(target) is not null)
        {
            return Reject(BattleCommandOutcome.TargetBlocked, command);
        }

        if (!ComputeReachable(unit).TryGetValue(target, out int steps))
        {
            return Reject(BattleCommandOutcome.NoPath, command);
        }

        Hex from = unit.Position;
        MoveUnit(unit, target, steps);

        _logger?.Log(LogEvent.Create(
            LogLevel.Debug,
            "Battle",
            "Стак переместился.",
            fields: new Dictionary<string, string>
            {
                ["unitId"] = unit.Id,
                ["from"] = from.ToString(),
                ["to"] = target.ToString(),
                ["steps"] = steps.ToString(),
                ["movementLeft"] = unit.MovementPointsRemaining.ToString()
            }));

        return new BattleCommandResult(BattleCommandOutcome.Applied, command.UnitId);
    }

    private BattleCommandResult ApplyWait(BattleUnit unit, BattleCommand command)
    {
        // Ожидание после перемещения дало бы второй пул очков в тот же раунд.
        if (!Queue.CanWait() || unit.MovementPointsRemaining < unit.Unit.Speed)
        {
            return Reject(BattleCommandOutcome.WaitNotAllowed, command);
        }

        Queue.Wait();
        AdvanceTurn();
        return new BattleCommandResult(BattleCommandOutcome.Applied, command.UnitId);
    }

    private BattleCommandResult ApplyDefend(BattleUnit unit, BattleCommand command)
    {
        unit.SetDefending();

        _logger?.Log(LogEvent.Create(
            LogLevel.Debug,
            "Battle",
            "Стак встал в защиту.",
            fields: new Dictionary<string, string>
            {
                ["unitId"] = unit.Id,
                ["defenseBonusPercent"] = Rules.DefendDefenseBonusPercent.ToString()
            }));

        Queue.CompleteTurn();
        AdvanceTurn();
        return new BattleCommandResult(BattleCommandOutcome.Applied, command.UnitId);
    }

    private BattleCommandResult ApplyAttack(BattleUnit attacker, BattleCommand command)
    {
        BattleUnit? target = command.TargetUnitId is null ? null : FindUnit(command.TargetUnitId);

        if (target is null || !target.IsAlive || target.Side == attacker.Side)
        {
            return Reject(BattleCommandOutcome.InvalidTarget, command);
        }

        // Любая атака требует очков движения (по умолчанию 1) и завершает ход (ADR-040).
        if (attacker.MovementPointsRemaining < Rules.AttackMovementCost)
        {
            return Reject(BattleCommandOutcome.NotEnoughMovement, command);
        }

        int distance = Field.Distance(attacker.Position, target.Position);
        bool melee = !(attacker.Unit.MovementType == MovementType.Ranged && distance >= 2);

        if (melee && distance > 1)
        {
            // ПКМ по врагу: идём к свободному соседнему гексу цели; на удар резервируется
            // attackMovementCost очков, поэтому путь не длиннее остатка минус стоимость атаки (ADR-040).
            (Hex Hex, int Steps)? approach = FindApproach(attacker, target);

            if (approach is null)
            {
                return Reject(BattleCommandOutcome.OutOfReach, command);
            }

            Hex from = attacker.Position;
            MoveUnit(attacker, approach.Value.Hex, approach.Value.Steps);

            _logger?.Log(LogEvent.Create(
                LogLevel.Debug,
                "Battle",
                "Стак подошёл к цели для ближней атаки.",
                fields: new Dictionary<string, string>
                {
                    ["unitId"] = attacker.Id,
                    ["targetId"] = target.Id,
                    ["from"] = from.ToString(),
                    ["to"] = attacker.Position.ToString(),
                    ["steps"] = approach.Value.Steps.ToString()
                }));
        }

        bool destroyed = Strike(attacker, target, melee, retaliation: false);

        if (melee && !destroyed && target.RetaliationsUsedThisRound < Rules.RetaliationPerRound)
        {
            target.SpendRetaliation();
            Strike(target, attacker, melee: true, retaliation: true);
        }

        // Атака завершает ход: остаток очков движения сгорает.
        Queue.CompleteTurn();
        AdvanceTurn();
        return new BattleCommandResult(BattleCommandOutcome.Applied, command.UnitId);
    }

    /// <summary>
    /// Ищет свободный гекс рядом с целью с кратчайшим путём не длиннее остатка очков движения
    /// за вычетом стоимости атаки.
    /// При равенстве выбирается первый в порядке соседей цели.
    /// </summary>
    private (Hex Hex, int Steps)? FindApproach(BattleUnit attacker, BattleUnit target)
    {
        int budget = attacker.MovementPointsRemaining - Rules.AttackMovementCost;

        if (budget < 1)
        {
            return null;
        }

        IReadOnlyDictionary<Hex, int> reachable = ComputeReachable(attacker, budget);
        (Hex Hex, int Steps)? best = null;

        foreach (Hex neighbor in target.Position.GetNeighbors())
        {
            if (reachable.TryGetValue(neighbor, out int steps) && (best is null || steps < best.Value.Steps))
            {
                best = (neighbor, steps);
            }
        }

        return best;
    }

    private void MoveUnit(BattleUnit unit, Hex target, int steps)
    {
        unit.SpendMovement(steps);
        unit.RegisterMovedSteps(steps);
        unit.MoveTo(target);
    }

    /// <summary>
    /// Один удар: считает урон с учётом морали, «Защиты», пассивных способностей, ветеранства и резистов,
    /// снимает здоровье, фиксирует гибель стака и ветеранство.
    /// </summary>
    /// <returns><c>true</c>, если цель уничтожена.</returns>
    private bool Strike(BattleUnit attacker, BattleUnit target, bool melee, bool retaliation)
    {
        int attackPercent = _morale.GetStatModifierPercent(GetUnitMoraleLevel(attacker));
        int defensePercent = _morale.GetStatModifierPercent(GetUnitMoraleLevel(target))
            + (target.IsDefending ? Rules.DefendDefenseBonusPercent : 0);

        List<EffectDef> attackerPassives = GetPassiveEffects(attacker).ToList();
        List<EffectDef> targetPassives = GetPassiveEffects(target).ToList();

        int attack = attacker.Attack;
        int defense = target.Defense;
        int critChance = attacker.CritChancePercent;
        int ignoreDefensePercent = 0;
        int damageBonusPercent = 0;
        int adjacentSame = CountAdjacent(
            attacker,
            other => other.Side == attacker.Side && string.Equals(other.Unit.Id, attacker.Unit.Id, StringComparison.Ordinal));
        int targetAdjacentAllies = CountAdjacent(target, other => other.Side == target.Side);

        foreach (EffectDef effect in attackerPassives)
        {
            switch (effect.Type)
            {
                case BattleEffectTypes.AttackBonusPerAdjacentSameUnit:
                    attack += effect.Value * adjacentSame;
                    break;
                case BattleEffectTypes.IgnoreDefensePercent:
                    ignoreDefensePercent += effect.Value;
                    break;
                case BattleEffectTypes.DamageBonusPercentIfMovedHexes:
                    if (!retaliation && attacker.StepsMovedThisTurn >= effect.MinHexes)
                    {
                        damageBonusPercent += effect.Value;
                    }

                    break;
                case BattleEffectTypes.CritChanceBonusIfTargetWounded:
                    if (target.TotalHealth < target.MaxTotalHealth)
                    {
                        critChance += effect.Value;
                    }

                    break;
            }
        }

        if (targetAdjacentAllies > 0)
        {
            foreach (EffectDef effect in targetPassives)
            {
                if (effect.Type == BattleEffectTypes.DefenseBonusIfAdjacentAlly)
                {
                    defense += effect.Value;
                }
            }
        }

        if (ignoreDefensePercent > 0)
        {
            defense = (int)IntegerMath.MulDiv(defense, 100 - Math.Min(100, ignoreDefensePercent), 100);
        }

        int distance = Field.Distance(attacker.Position, target.Position);
        int modifier;

        if (melee)
        {
            modifier = attacker.Unit.MovementType == MovementType.Ranged
                ? Damage.GetShooterMeleeModifierPerMille()
                : 1000;
        }
        else
        {
            HashSet<Hex> others = _units
                .Where(unit => unit.IsAlive && !ReferenceEquals(unit, attacker) && !ReferenceEquals(unit, target))
                .Select(unit => unit.Position)
                .ToHashSet();

            modifier = Damage.GetRangedModifierPerMille(
                distance,
                Field.HasBlockerBetween(attacker.Position, target.Position, others));
        }

        if (damageBonusPercent > 0)
        {
            modifier = (int)IntegerMath.MulDiv(modifier, 1000 + damageBonusPercent * 10, 1000);
        }

        DamageRequest request = new(
            UnitCount: attacker.Count,
            DamageMin: attacker.Unit.DamageMin,
            DamageMax: attacker.Unit.DamageMax,
            Attack: attack,
            Defense: defense,
            DamageType: attacker.Unit.DamageType,
            TargetResistances: AdjustResistances(target.Unit.Resistances, targetPassives),
            CritChancePercent: Math.Min(100, critChance),
            CritDamagePercent: attacker.CritDamagePercent,
            AttackPercent: attackPercent,
            DefensePercent: defensePercent,
            ModifierPerMille: modifier);

        DamageResult damage = Damage.Calculate(request, _random);
        CasualtyResult casualty = target.ApplyDamage(damage.FinalDamage);

        _logger?.Log(LogEvent.Create(
            LogLevel.Info,
            "Battle",
            retaliation ? "Контратака." : "Удар.",
            fields: new Dictionary<string, string>
            {
                ["attackerId"] = attacker.Id,
                ["targetId"] = target.Id,
                ["melee"] = melee ? "true" : "false",
                ["damage"] = damage.FinalDamage.ToString(),
                ["critical"] = damage.IsCritical ? "true" : "false",
                ["unitsKilled"] = casualty.UnitsKilled.ToString(),
                ["targetCount"] = casualty.RemainingCount.ToString(),
                ["round"] = Queue.Round.ToString()
            }));

        foreach (EffectDef effect in attackerPassives)
        {
            if (effect.Type == BattleEffectTypes.HealPercentOfDamage && attacker.IsAlive)
            {
                int healed = attacker.Heal((int)IntegerMath.MulDiv(casualty.DamageDealt, effect.Value, 100));

                if (healed > 0)
                {
                    LogEffect("Стак восстановил здоровье от нанесённого урона.", attacker, healed);
                }
            }
        }

        if (casualty.StackDestroyed)
        {
            NotifyStackDestroyed(target.Side);
            attacker.RegisterKills(1, _logger);
        }

        return casualty.StackDestroyed;
    }

    private IEnumerable<EffectDef> GetPassiveEffects(BattleUnit unit)
    {
        foreach (string abilityId in unit.Unit.AbilityIds)
        {
            AbilityDef? ability = _catalog.FindAbility(abilityId);

            if (ability is not null && ability.Kind == BattleEffectTypes.KindPassive)
            {
                foreach (EffectDef effect in ability.Effects)
                {
                    yield return effect;
                }
            }
        }

        AbilityDef? classPassive = _catalog.FindAbility(_setups[unit.Side].Hero?.PassiveAbilityId);

        if (classPassive is not null && classPassive.Kind == BattleEffectTypes.KindPassive)
        {
            foreach (EffectDef effect in classPassive.Effects)
            {
                yield return effect;
            }
        }
    }

    private bool ClassPassiveHasEffect(BattleSide side, string effectType)
    {
        AbilityDef? passive = _catalog.FindAbility(_setups[side].Hero?.PassiveAbilityId);

        return passive is not null
            && passive.Kind == BattleEffectTypes.KindPassive
            && passive.Effects.Any(effect => effect.Type == effectType);
    }

    private static Resistances AdjustResistances(Resistances baseValues, IEnumerable<EffectDef> passives)
    {
        List<EffectDef> list = passives.ToList();

        int Value(DamageType type)
        {
            int value = baseValues.Get(type);

            foreach (EffectDef effect in list)
            {
                if (effect.DamageType != type)
                {
                    continue;
                }

                if (effect.Type == BattleEffectTypes.DamageTypeResistance)
                {
                    value += effect.Value;
                }
                else if (effect.Type == BattleEffectTypes.ImmuneDamageType)
                {
                    value = 100;
                }
            }

            return Math.Clamp(value, 0, 100);
        }

        return new Resistances(
            Value(DamageType.Physical),
            Value(DamageType.Magic),
            Value(DamageType.Poison),
            Value(DamageType.Fire),
            Value(DamageType.Gunpowder));
    }

    private int CountAdjacent(BattleUnit unit, Func<BattleUnit, bool> predicate)
    {
        int count = 0;

        foreach (Hex neighbor in unit.Position.GetNeighbors())
        {
            BattleUnit? other = GetUnitAt(neighbor);

            if (other is not null && !ReferenceEquals(other, unit) && predicate(other))
            {
                count++;
            }
        }

        return count;
    }

    private void ApplyHeroBonuses(BattleSide side)
    {
        BattleHero? hero = _setups[side].Hero;

        if (hero is null)
        {
            return;
        }

        _mana[side] = hero.Mana;
        AbilityDef? passive = _catalog.FindAbility(hero.PassiveAbilityId);

        foreach (BattleUnit unit in _units.Where(u => u.Side == side))
        {
            int initiative = 0;

            if (passive is not null && passive.Kind == BattleEffectTypes.KindPassive)
            {
                foreach (EffectDef effect in passive.Effects)
                {
                    if (effect.Type == BattleEffectTypes.InitiativeBonusIfTierAtMost && unit.Unit.Tier <= effect.MaxTier)
                    {
                        initiative += effect.Value;
                    }
                }
            }

            unit.SetHeroBonuses(hero.Attack, hero.Defense, initiative);
        }
    }

    /// <summary>
    /// Начало нового раунда: регенерация, затем уменьшение длительности эффектов и перезарядок.
    /// </summary>
    private void BeginNextRoundEffects()
    {
        foreach (BattleUnit unit in _units.Where(u => u.IsAlive))
        {
            int regeneration = unit.GetEffectSum(BattleEffectTypes.Regeneration);

            if (regeneration > 0)
            {
                int healed = unit.Heal(regeneration);

                if (healed > 0)
                {
                    LogEffect("Регенерация восстановила здоровье стака.", unit, healed);
                }
            }

            unit.TickEffects();
            unit.TickCooldowns();
        }
    }

    private void LogEffect(string message, BattleUnit unit, int amount)
    {
        _logger?.Log(LogEvent.Create(
            LogLevel.Debug,
            "Battle",
            message,
            fields: new Dictionary<string, string>
            {
                ["unitId"] = unit.Id,
                ["amount"] = amount.ToString(),
                ["round"] = Queue.Round.ToString()
            }));
    }

    private BattleCommandResult ApplyUseAbility(BattleUnit unit, BattleCommand command)
    {
        AbilityDef? ability = _catalog.FindAbility(command.AbilityId);

        if (ability is null
            || ability.Kind != BattleEffectTypes.KindActive
            || !unit.Unit.AbilityIds.Contains(ability.Id, StringComparer.Ordinal))
        {
            return Reject(BattleCommandOutcome.UnknownAbility, command);
        }

        if (unit.GetCooldown(ability.Id) > 0)
        {
            return Reject(BattleCommandOutcome.OnCooldown, command);
        }

        if (ability.Effects.Any(effect => !BattleEffectTypes.IsActiveSupported(effect.Type)))
        {
            return Reject(BattleCommandOutcome.UnsupportedEffect, command);
        }

        BattleCommandOutcome targeting = ResolveAbilityTargets(unit, ability, command.TargetUnitId, out List<BattleUnit> targets);

        if (targeting != BattleCommandOutcome.Applied)
        {
            return Reject(targeting, command);
        }

        ApplyEffects(ability.Effects, targets, ability.DurationRounds, unit.Id, unit);
        unit.StartCooldown(ability.Id, ability.CooldownRounds);

        _logger?.Log(LogEvent.Create(
            LogLevel.Info,
            "Battle",
            "Стак применил способность.",
            fields: new Dictionary<string, string>
            {
                ["unitId"] = unit.Id,
                ["abilityId"] = ability.Id,
                ["targets"] = string.Join(",", targets.Select(t => t.Id)),
                ["cooldown"] = ability.CooldownRounds.ToString(),
                ["round"] = Queue.Round.ToString()
            }));

        Queue.CompleteTurn();
        AdvanceTurn();
        return new BattleCommandResult(BattleCommandOutcome.Applied, command.UnitId);
    }

    private BattleCommandOutcome ResolveAbilityTargets(
        BattleUnit caster,
        AbilityDef ability,
        string? targetUnitId,
        out List<BattleUnit> targets)
    {
        targets = new List<BattleUnit>();

        switch (ability.Target)
        {
            case "aura":
                targets = _units
                    .Where(u => u.IsAlive && u.Side == caster.Side
                        && Field.Distance(caster.Position, u.Position) <= ability.Radius)
                    .ToList();
                return BattleCommandOutcome.Applied;

            case "enemyStack":
            case "allyStack":
            case "enemyArea":
                bool wantEnemy = ability.Target != "allyStack";
                BattleUnit? target = targetUnitId is null ? null : FindUnit(targetUnitId);

                if (target is null || !target.IsAlive || (target.Side != caster.Side) != wantEnemy)
                {
                    return BattleCommandOutcome.InvalidTarget;
                }

                if (ability.Range > 0 && Field.Distance(caster.Position, target.Position) > ability.Range)
                {
                    return BattleCommandOutcome.OutOfRange;
                }

                if (ability.Target == "enemyArea")
                {
                    targets = _units
                        .Where(u => u.IsAlive && u.Side == target.Side
                            && Field.Distance(target.Position, u.Position) <= ability.Radius)
                        .ToList();
                }
                else
                {
                    targets.Add(target);
                }

                return BattleCommandOutcome.Applied;

            default:
                return BattleCommandOutcome.UnsupportedEffect;
        }
    }

    private BattleCommandResult ApplyCastSpell(BattleCommand command)
    {
        BattleSide side = command.Side ?? BattleSide.Attacker;
        BattleUnit? active = ActiveUnit;

        if (active is null || active.Side != side)
        {
            return Reject(BattleCommandOutcome.NotActiveUnit, command);
        }

        BattleHero? hero = _setups[side].Hero;
        SpellDef? spell = hero is null ? null : _catalog.FindSpell(hero.SpellId);

        if (hero is null || hero.SpellId is null)
        {
            return Reject(BattleCommandOutcome.NoHero, command);
        }

        if (spell is null)
        {
            return Reject(BattleCommandOutcome.UnknownAbility, command);
        }

        if (_spellCastRound.TryGetValue(side, out int round) && round == Queue.Round)
        {
            return Reject(BattleCommandOutcome.SpellAlreadyCast, command);
        }

        if (GetMana(side) < spell.ManaCost)
        {
            return Reject(BattleCommandOutcome.NotEnoughMana, command);
        }

        if (spell.Effects.Any(effect => !BattleEffectTypes.IsActiveSupported(effect.Type))
            || (spell.Target != "allyStack" && spell.Target != "enemyStack"))
        {
            return Reject(BattleCommandOutcome.UnsupportedEffect, command);
        }

        BattleUnit? target = command.TargetUnitId is null ? null : FindUnit(command.TargetUnitId);
        bool wantEnemy = spell.Target == "enemyStack";

        if (target is null || !target.IsAlive || (target.Side != side) != wantEnemy)
        {
            return Reject(BattleCommandOutcome.InvalidTarget, command);
        }

        _mana[side] = GetMana(side) - spell.ManaCost;
        _spellCastRound[side] = Queue.Round;

        ApplyEffects(spell.Effects, new List<BattleUnit> { target }, spell.DurationRounds, hero.HeroId, null);

        _logger?.Log(LogEvent.Create(
            LogLevel.Info,
            "Battle",
            "Герой применил заклинание.",
            fields: new Dictionary<string, string>
            {
                ["side"] = side.ToString(),
                ["spellId"] = spell.Id,
                ["targetId"] = target.Id,
                ["manaLeft"] = GetMana(side).ToString(),
                ["round"] = Queue.Round.ToString()
            }));

        UpdateStatus();
        return new BattleCommandResult(BattleCommandOutcome.Applied, command.UnitId);
    }

    private BattleCommandResult ApplyCommandAbility(BattleCommand command)
    {
        BattleSide side = command.Side ?? BattleSide.Attacker;
        BattleUnit? active = ActiveUnit;

        if (active is null || active.Side != side)
        {
            return Reject(BattleCommandOutcome.NotActiveUnit, command);
        }

        BattleHero? hero = _setups[side].Hero;

        if (hero is null || hero.CommandAbilityId is null)
        {
            return Reject(BattleCommandOutcome.NoHero, command);
        }

        if (_commandUsed.Contains(side))
        {
            return Reject(BattleCommandOutcome.CommandAbilityAlreadyUsed, command);
        }

        // Заглушка до этапа героев: использование фиксируется, эффект не применяется.
        _commandUsed.Add(side);

        _logger?.Log(LogEvent.Create(
            LogLevel.Info,
            "Battle",
            "Герой использовал командную способность (заглушка, эффект не применён).",
            fields: new Dictionary<string, string>
            {
                ["side"] = side.ToString(),
                ["abilityId"] = hero.CommandAbilityId,
                ["round"] = Queue.Round.ToString()
            }));

        return new BattleCommandResult(BattleCommandOutcome.Applied, command.UnitId);
    }

    /// <summary>
    /// Применяет эффекты способности или заклинания к целям. Источник-стак получает ветеранство за убийства.
    /// </summary>
    private void ApplyEffects(
        IReadOnlyList<EffectDef> effects,
        IReadOnlyList<BattleUnit> targets,
        int defaultDuration,
        string sourceId,
        BattleUnit? creditUnit)
    {
        foreach (EffectDef effect in effects)
        {
            foreach (BattleUnit target in targets)
            {
                if (!target.IsAlive)
                {
                    continue;
                }

                switch (effect.Type)
                {
                    case BattleEffectTypes.Damage:
                        DealEffectDamage(effect, target, creditUnit);
                        break;
                    case BattleEffectTypes.Heal:
                        target.Heal(effect.Value);
                        break;
                    default:
                        int duration = effect.DurationRounds > 0
                            ? effect.DurationRounds
                            : Math.Max(1, defaultDuration);
                        target.AddEffect(effect.Type, effect.Value, duration, sourceId);
                        break;
                }
            }
        }
    }

    private void DealEffectDamage(EffectDef effect, BattleUnit target, BattleUnit? creditUnit)
    {
        DamageType type = effect.DamageType ?? DamageType.Physical;
        int roll = _random.Next(effect.Min, effect.Max + 1);
        int resist = AdjustResistances(target.Unit.Resistances, GetPassiveEffects(target)).Get(type);
        int amount = (int)IntegerMath.MulDiv(roll, 100 - resist, 100);

        if (resist < 100 && amount < 1)
        {
            amount = 1;
        }

        CasualtyResult casualty = target.ApplyDamage(amount);

        _logger?.Log(LogEvent.Create(
            LogLevel.Info,
            "Battle",
            "Урон способности или заклинания.",
            fields: new Dictionary<string, string>
            {
                ["targetId"] = target.Id,
                ["damageType"] = type.ToString(),
                ["roll"] = roll.ToString(),
                ["resistPercent"] = resist.ToString(),
                ["damage"] = amount.ToString(),
                ["unitsKilled"] = casualty.UnitsKilled.ToString()
            }));

        if (casualty.StackDestroyed)
        {
            NotifyStackDestroyed(target.Side);
            creditUnit?.RegisterKills(1, _logger);
        }
    }

    private BattleCommandResult Reject(BattleCommandOutcome outcome, BattleCommand command)
    {
        _logger?.Log(LogEvent.Create(
            LogLevel.Debug,
            "Battle",
            "Команда боя отклонена.",
            fields: new Dictionary<string, string>
            {
                ["command"] = command.Kind.ToString(),
                ["unitId"] = command.UnitId,
                ["outcome"] = outcome.ToString()
            }));

        return new BattleCommandResult(outcome, command.UnitId);
    }

    /// <summary>
    /// Доводит очередь до следующего хода, который должен сыграть игрок: начинает раунды,
    /// снимает «Защиту», бросает мораль (пропуск хода и дополнительное действие).
    /// </summary>
    private void AdvanceTurn()
    {
        for (int iteration = 0; iteration < MaxAdvanceIterations; iteration++)
        {
            if (UpdateStatus())
            {
                return;
            }

            TurnEntry? current = Queue.Current;

            if (current is null)
            {
                if (Queue.Round > 0)
                {
                    BeginNextRoundEffects();
                }

                Queue.StartRound(_units);
                continue;
            }

            BattleUnit unit = current.Value.Unit;
            unit.BeginTurn();

            if (unit.WaitedThisRound)
            {
                return;
            }

            MoraleRoll roll = _morale.RollAction(
                unit.Unit,
                GetUnitMoraleLevel(unit),
                unit.ExtraActionsUsedThisRound,
                current.Value.IsExtraAction,
                _random);

            if (roll == MoraleRoll.Skip)
            {
                LogMorale(unit, "Стак пропускает ход из-за морали.");
                Queue.CompleteTurn();
                continue;
            }

            if (roll == MoraleRoll.ExtraAction)
            {
                Queue.AppendExtraAction(unit);
                LogMorale(unit, "Стак получил дополнительное действие от морали.");
            }

            return;
        }

        throw new InvalidOperationException(
            "Очередь боя не продвигается: превышен лимит пропусков хода (вероятная ошибка правил).");
    }

    private bool UpdateStatus()
    {
        bool attackerAlive = _units.Any(unit => unit.Side == BattleSide.Attacker && unit.IsAlive);
        bool defenderAlive = _units.Any(unit => unit.Side == BattleSide.Defender && unit.IsAlive);

        BattleStatus status = (attackerAlive, defenderAlive) switch
        {
            (true, true) => BattleStatus.Ongoing,
            (true, false) => BattleStatus.AttackerWon,
            (false, true) => BattleStatus.DefenderWon,
            _ => BattleStatus.Draw
        };

        if (status != Status)
        {
            Status = status;

            _logger?.Log(LogEvent.Create(
                LogLevel.Info,
                "Battle",
                "Бой завершён.",
                fields: new Dictionary<string, string>
                {
                    ["status"] = status.ToString(),
                    ["round"] = Queue.Round.ToString()
                }));
        }

        return status != BattleStatus.Ongoing;
    }

    private int CountActiveAllyDeaths(BattleSide side)
    {
        int duration = _morale.AllyStackDeathDurationRounds;
        return _allyDeathRounds[side].Count(round => Queue.Round - round < duration);
    }

    private void LogMorale(BattleUnit unit, string message)
    {
        _logger?.Log(LogEvent.Create(
            LogLevel.Debug,
            "Battle",
            message,
            fields: new Dictionary<string, string>
            {
                ["unitId"] = unit.Id,
                ["round"] = Queue.Round.ToString(),
                ["moraleLevel"] = GetMoraleLevel(unit.Side).ToString()
            }));
    }

    private void Deploy(BattleSide side, BattleSideSetup setup)
    {
        if (setup.Stacks is null || setup.Stacks.Count == 0)
        {
            throw new ArgumentException($"У стороны {side} нет стаков.", nameof(setup));
        }

        int capacity = Field.Height * 2;

        if (setup.Stacks.Count > capacity)
        {
            throw new ArgumentException(
                $"Сторона {side} имеет {setup.Stacks.Count} стаков, на поле помещается {capacity}.",
                nameof(setup));
        }

        string prefix = side == BattleSide.Attacker ? "A" : "D";

        for (int slot = 0; slot < setup.Stacks.Count; slot++)
        {
            Stack stack = setup.Stacks[slot];
            int row = slot % Field.Height;
            int depth = slot / Field.Height;
            int column = side == BattleSide.Attacker ? depth : Field.Width - 1 - depth;
            Hex position = Field.Grid.HexAt(column, row);

            if (Field.IsObstacle(position))
            {
                throw new InvalidOperationException(
                    $"Стартовый гекс {position} стака {prefix}{slot} занят препятствием.");
            }

            _units.Add(new BattleUnit(
                $"{prefix}{slot}",
                side,
                slot,
                stack.Unit,
                stack.Count,
                position,
                stack.KilledStacks));
        }
    }
}
