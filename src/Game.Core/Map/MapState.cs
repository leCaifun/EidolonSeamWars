using System.Collections.ObjectModel;
using Game.Core.Diagnostics;
using Game.Core.Domain;

namespace Game.Core.Map;

/// <summary>
/// Состояние глобальной карты: карта, партии, туман войны и день. Любое
/// изменение выполняется командой через <see cref="Apply"/> (ADR-018).
/// Движение списывает очки по фактической стоимости маршрута; встреча с
/// блокирующим объектом или чужой партией возвращает
/// <see cref="MapCommandOutcome.Collision"/> и не разрешает бой автоматически.
/// </summary>
public sealed class MapState
{
    private readonly Dictionary<string, Party> _parties;
    private readonly ReadOnlyDictionary<string, Party> _readOnlyParties;
    private readonly Pathfinder _pathfinder;
    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт состояние карты и открывает начальный обзор вокруг партий.
    /// </summary>
    /// <param name="map">Карта.</param>
    /// <param name="parties">Партии; ID уникальны.</param>
    /// <param name="fog">Туман войны.</param>
    /// <param name="settings">Параметры генерации и радиусов обзора.</param>
    /// <param name="pathfinder">Поисковик путей; по умолчанию создаётся новый.</param>
    /// <param name="logger">Необязательный журнал доменных операций.</param>
    /// <exception cref="ArgumentNullException">Обязательный аргумент не задан.</exception>
    /// <exception cref="ArgumentException">Повторяющийся ID партии.</exception>
    public MapState(
        GameMap map,
        IEnumerable<Party> parties,
        FogOfWar fog,
        MapGenerationSettings settings,
        Pathfinder? pathfinder = null,
        IGameLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(parties);
        ArgumentNullException.ThrowIfNull(fog);
        ArgumentNullException.ThrowIfNull(settings);

        Map = map;
        Fog = fog;
        Settings = settings;
        _logger = logger;
        _pathfinder = pathfinder ?? new Pathfinder(logger);
        _parties = new Dictionary<string, Party>(StringComparer.Ordinal);

        foreach (Party party in parties)
        {
            if (!_parties.TryAdd(party.Id, party))
            {
                throw new ArgumentException($"Повторный ID партии «{party.Id}».", nameof(parties));
            }
        }

        _readOnlyParties = new ReadOnlyDictionary<string, Party>(_parties);
        Day = 1;

        RefreshFog();
    }

    /// <summary>Карта.</summary>
    public GameMap Map { get; }

    /// <summary>Партии по ID.</summary>
    public IReadOnlyDictionary<string, Party> Parties => _readOnlyParties;

    /// <summary>Туман войны.</summary>
    public FogOfWar Fog { get; }

    /// <summary>Параметры радиусов обзора и движения.</summary>
    public MapGenerationSettings Settings { get; }

    /// <summary>Текущий день партии; начинается с 1.</summary>
    public int Day { get; private set; }

    /// <summary>
    /// Находит партию по ID или возвращает null.
    /// </summary>
    public Party? FindParty(string partyId)
    {
        return partyId is not null && _parties.TryGetValue(partyId, out Party? party)
            ? party
            : null;
    }

    /// <summary>
    /// Применяет команду карты.
    /// </summary>
    /// <param name="command">Команда.</param>
    /// <exception cref="ArgumentNullException">Команда не задана.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Неизвестный тип команды.</exception>
    public MapCommandResult Apply(MapCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command.Kind switch
        {
            MapCommandKind.MoveParty => ApplyMove(command),
            MapCommandKind.EndDay => ApplyEndDay(),
            MapCommandKind.EnterObject => ApplyEnterObject(command),
            _ => throw new ArgumentOutOfRangeException(
                nameof(command),
                command.Kind,
                "Неизвестный тип команды карты.")
        };
    }

    private MapCommandResult ApplyMove(MapCommand command)
    {
        if (command.PartyId is null || !_parties.TryGetValue(command.PartyId, out Party? party))
        {
            return MapCommandResult.Rejected(
                MapCommandKind.MoveParty,
                command.PartyId,
                MapCommandOutcome.InvalidParty);
        }

        if (command.Target is not Hex target)
        {
            return MapCommandResult.Rejected(
                MapCommandKind.MoveParty,
                party.Id,
                MapCommandOutcome.OutOfBounds);
        }

        if (!Map.Grid.Contains(target))
        {
            return MapCommandResult.Rejected(
                MapCommandKind.MoveParty,
                party.Id,
                MapCommandOutcome.OutOfBounds);
        }

        if (!Map.IsPassable(target))
        {
            return MapCommandResult.Rejected(
                MapCommandKind.MoveParty,
                party.Id,
                MapCommandOutcome.Impassable);
        }

        // Блокирующие объекты нельзя пройти насквозь: бой возможен только в целевом гексе.
        PathResult path = _pathfinder.FindPath(Map, party.Position, target, blockOccupiedTiles: true);

        if (!path.Success)
        {
            MapCommandOutcome outcome = path.Failure switch
            {
                PathFailure.OutOfBounds => MapCommandOutcome.OutOfBounds,
                PathFailure.ImpassableGoal => MapCommandOutcome.Impassable,
                _ => MapCommandOutcome.NoPath
            };

            Log(
                LogLevel.Warning,
                $"Партия «{party.Id}» не может пройти из {party.Position} в {target}: {path.Failure}.",
                ("party", party.Id),
                ("from", party.Position.ToString()),
                ("to", target.ToString()),
                ("failure", path.Failure.ToString()));

            return MapCommandResult.Rejected(MapCommandKind.MoveParty, party.Id, outcome);
        }

        if (!party.CanSpend(path.Cost))
        {
            Log(
                LogLevel.Warning,
                $"Партии «{party.Id}» не хватает очков движения: нужно {path.Cost}, остаток {party.MovementPoints}.",
                ("party", party.Id),
                ("cost", path.Cost.ToString()),
                ("remaining", party.MovementPoints.ToString()));

            return MapCommandResult.Rejected(
                MapCommandKind.MoveParty,
                party.Id,
                MapCommandOutcome.NotEnoughMovement);
        }

        party.MoveTo(target);
        party.Spend(path.Cost);
        RefreshFog();

        Party? other = SortedParties()
            .FirstOrDefault(candidate =>
                !string.Equals(candidate.Id, party.Id, StringComparison.Ordinal) &&
                candidate.Position == target);

        if (other is not null)
        {
            Log(
                LogLevel.Info,
                $"Партия «{party.Id}» встретила партию «{other.Id}» на {target}; бой передаётся экрану боя.",
                ("party", party.Id),
                ("encounter", other.Id),
                ("hex", target.ToString()));

            return MapCommandResult.Moved(
                MapCommandKind.MoveParty,
                party.Id,
                path.Cost,
                target,
                MapCommandOutcome.Collision,
                encounterPartyId: other.Id);
        }

        MapObjectInstance? blocking = Map.GetBlockingObjectAt(target);

        if (blocking is not null)
        {
            Log(
                LogLevel.Info,
                $"Партия «{party.Id}» встретила объект «{blocking.DefinitionId}» на {target}; бой передаётся экрану боя.",
                ("party", party.Id),
                ("object", blocking.DefinitionId),
                ("instance", blocking.InstanceId),
                ("hex", target.ToString()));

            return MapCommandResult.Moved(
                MapCommandKind.MoveParty,
                party.Id,
                path.Cost,
                target,
                MapCommandOutcome.Collision,
                blocking.DefinitionId,
                blocking.InstanceId);
        }

        MapObjectInstance? passive = Map.GetObjectsAt(target).FirstOrDefault();

        if (passive is not null)
        {
            return MapCommandResult.Moved(
                MapCommandKind.MoveParty,
                party.Id,
                path.Cost,
                target,
                MapCommandOutcome.ObjectEntered,
                passive.DefinitionId,
                passive.InstanceId);
        }

        Log(
            LogLevel.Info,
            $"Партия «{party.Id}» переместилась в {target}, стоимость {path.Cost}.",
            ("party", party.Id),
            ("to", target.ToString()),
            ("cost", path.Cost.ToString()));

        return MapCommandResult.Moved(
            MapCommandKind.MoveParty,
            party.Id,
            path.Cost,
            target,
            MapCommandOutcome.Applied);
    }

    private MapCommandResult ApplyEnterObject(MapCommand command)
    {
        if (command.PartyId is null || !_parties.TryGetValue(command.PartyId, out Party? party))
        {
            return MapCommandResult.Rejected(
                MapCommandKind.EnterObject,
                command.PartyId,
                MapCommandOutcome.InvalidParty);
        }

        if (command.ObjectInstanceId is null)
        {
            return MapCommandResult.Rejected(
                MapCommandKind.EnterObject,
                party.Id,
                MapCommandOutcome.InvalidObject);
        }

        MapObjectInstance? instance = Map.FindObject(command.ObjectInstanceId);

        if (instance is null || instance.Position != party.Position)
        {
            Log(
                LogLevel.Warning,
                $"Партия «{party.Id}» не может войти в объект «{command.ObjectInstanceId}»: объект не на её гексе.",
                ("party", party.Id),
                ("object", command.ObjectInstanceId));

            return MapCommandResult.Rejected(
                MapCommandKind.EnterObject,
                party.Id,
                MapCommandOutcome.InvalidObject);
        }

        if (instance.Definition.IsBlocking)
        {
            Log(
                LogLevel.Info,
                $"Вход в охраняемый объект «{instance.DefinitionId}» требует боя; автобой запрещён.",
                ("party", party.Id),
                ("object", instance.DefinitionId),
                ("instance", instance.InstanceId));

            return MapCommandResult.Moved(
                MapCommandKind.EnterObject,
                party.Id,
                0,
                party.Position,
                MapCommandOutcome.Collision,
                instance.DefinitionId,
                instance.InstanceId);
        }

        instance.MarkVisited();

        Log(
            LogLevel.Info,
            $"Партия «{party.Id}» вошла в объект «{instance.DefinitionId}».",
            ("party", party.Id),
            ("object", instance.DefinitionId),
            ("instance", instance.InstanceId));

        return MapCommandResult.ObjectEntered(party.Id, instance.DefinitionId, instance.InstanceId);
    }

    private MapCommandResult ApplyEndDay()
    {
        Day = checked(Day + 1);

        foreach (Party party in SortedParties())
        {
            party.ResetMovement();
        }

        RefreshFog();

        Log(
            LogLevel.Info,
            $"Наступил день {Day}; очки движения партий восстановлены.",
            ("day", Day.ToString()));

        return MapCommandResult.DayEnded();
    }

    /// <summary>
    /// Полностью пересчитывает туман по всем партиям: видимость фракции — объединение
    /// обзоров всех её партий; ранее открытое остаётся исследованным навсегда.
    /// </summary>
    private void RefreshFog()
    {
        List<Party> onMap = SortedParties()
            .Where(party => Map.Grid.Contains(party.Position))
            .ToList();

        foreach (string factionId in onMap
            .Select(party => party.FactionId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal))
        {
            Fog.ClearVisibility(factionId);

            foreach (Party party in onMap)
            {
                if (!string.Equals(party.FactionId, factionId, StringComparison.Ordinal))
                {
                    continue;
                }

                BiomeDefinition biome = Map.GetBiome(party.Position);
                int visibility = Math.Max(Settings.FogVisibilityRadius, biome.VisibilityRange);
                int explored = Math.Max(Settings.FogExploredRadius, visibility);

                Fog.AddVision(factionId, party.Position, visibility);
                Fog.MarkExplored(factionId, party.Position, explored);
            }
        }
    }

    private IEnumerable<Party> SortedParties()
    {
        return _parties.Values.OrderBy(party => party.Id, StringComparer.Ordinal);
    }

    private void Log(
        LogLevel level,
        string message,
        params (string Key, string Value)[] fields)
    {
        if (_logger is null)
        {
            return;
        }

        Dictionary<string, string> values = new(fields.Length);

        foreach ((string key, string value) in fields)
        {
            values[key] = value;
        }

        _logger.Log(LogEvent.Create(level, "Map", message, fields: values));
    }
}
