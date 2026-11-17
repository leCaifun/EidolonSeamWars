using Game.Core.Diagnostics;
using Game.Core.Domain;

namespace Game.Core.Map;

/// <summary>
/// Детерминированный поиск пути по гекс-карте (A*) и построение области
/// достижимых гексов (Дейкстра). Порядок соседей фиксирован типом
/// <see cref="Hex"/>; при равной оценке пути выбирается раньше добавленный,
/// поэтому результат воспроизводим при одном и том же состоянии карты.
/// Отсутствие пути логируется категорией <c>Map</c>.
/// </summary>
public sealed class Pathfinder
{
    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт поисковик путей.
    /// </summary>
    /// <param name="logger">Необязательный журнал доменных операций.</param>
    public Pathfinder(IGameLogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Ищет путь от старта до цели. Стоимость учитывает только входы на гексы;
    /// стартовый гекс стоит 0, целевой — включён.
    /// </summary>
    /// <param name="map">Карта.</param>
    /// <param name="start">Стартовый гекс.</param>
    /// <param name="goal">Целевой гекс.</param>
    /// <param name="blockOccupiedTiles">
    /// Если <c>true</c>, гексы с блокирующими объектами нельзя проходить
    /// транзитом; целевой гекс всё равно разрешён (столкновение обрабатывается
    /// командой движения отдельно).
    /// </param>
    /// <param name="blockedHexes">
    /// Необязательное множество гексов, занятых другими партиями: их нельзя
    /// проходить транзитом, но целевой гекс разрешён (встреча → бой). Стартовый
    /// гекс множеством не блокируется.
    /// </param>
    /// <exception cref="ArgumentNullException">Карта не задана.</exception>
    public PathResult FindPath(
        GameMap map,
        Hex start,
        Hex goal,
        bool blockOccupiedTiles = false,
        IReadOnlySet<Hex>? blockedHexes = null)
    {
        ArgumentNullException.ThrowIfNull(map);

        if (!map.Grid.Contains(start) || !map.Grid.Contains(goal))
        {
            return PathResult.Fail(PathFailure.OutOfBounds);
        }

        if (!map.IsPassable(start))
        {
            return PathResult.Fail(PathFailure.ImpassableStart);
        }

        if (!map.IsPassable(goal))
        {
            return PathResult.Fail(PathFailure.ImpassableGoal);
        }

        if (start == goal)
        {
            return PathResult.Found(0, new[] { start });
        }

        PriorityQueue<Hex, (int F, int G, int Seq)> open = new();
        Dictionary<Hex, int> bestCost = new() { [start] = 0 };
        Dictionary<Hex, Hex> cameFrom = new();
        HashSet<Hex> closed = new();
        long sequence = 0;

        open.Enqueue(start, (Heuristic(start, goal), 0, (int)sequence++));

        while (open.TryDequeue(out Hex current, out _))
        {
            if (!closed.Add(current))
            {
                continue;
            }

            if (current == goal)
            {
                return PathResult.Found(bestCost[goal], Reconstruct(cameFrom, start, goal));
            }

            Hex[] neighbors = current.GetNeighbors();

            for (int i = 0; i < neighbors.Length; i++)
            {
                Hex neighbor = neighbors[i];

                if (!map.Grid.Contains(neighbor) || !map.IsPassable(neighbor))
                {
                    continue;
                }

                if (blockOccupiedTiles && neighbor != goal && map.HasBlockingObject(neighbor))
                {
                    continue;
                }

                if (blockedHexes is not null && neighbor != goal && blockedHexes.Contains(neighbor))
                {
                    continue;
                }

                int stepCost = map.GetMovementCost(neighbor);
                int tentative = checked(bestCost[current] + stepCost);

                if (bestCost.TryGetValue(neighbor, out int known) && tentative >= known)
                {
                    continue;
                }

                bestCost[neighbor] = tentative;
                cameFrom[neighbor] = current;
                int f = checked(tentative + Heuristic(neighbor, goal));
                open.Enqueue(neighbor, (f, tentative, (int)sequence++));
            }
        }

        _logger?.Log(LogEvent.Create(
            LogLevel.Warning,
            "Map",
            $"Путь от {start} до {goal} не найден.",
            fields: new Dictionary<string, string>
            {
                ["from"] = start.ToString(),
                ["to"] = goal.ToString()
            }));

        return PathResult.Fail(PathFailure.NoPath);
    }

    /// <summary>
    /// Строит карту достижимых гексов и минимальных стоимостей от старта
    /// в пределах бюджета очков движения. Блокирующие объекты не учитываются:
    /// это подсветка достижимости, а не маршрут столкновения.
    /// </summary>
    /// <param name="map">Карта.</param>
    /// <param name="start">Стартовый гекс.</param>
    /// <param name="maxCost">Бюджет очков движения; отрицательный — только старт.</param>
    /// <param name="blockedHexes">
    /// Необязательное множество гексов, занятых другими партиями: в них можно войти
    /// (встреча), но через них нельзя пройти дальше.
    /// </param>
    /// <exception cref="ArgumentNullException">Карта не задана.</exception>
    public IReadOnlyDictionary<Hex, int> ComputeReachable(
        GameMap map,
        Hex start,
        int maxCost,
        IReadOnlySet<Hex>? blockedHexes = null)
    {
        ArgumentNullException.ThrowIfNull(map);

        Dictionary<Hex, int> costs = new();

        if (!map.Grid.Contains(start) || !map.IsPassable(start))
        {
            return costs;
        }

        PriorityQueue<Hex, (int Cost, int Seq)> open = new();
        long sequence = 0;
        open.Enqueue(start, (0, (int)sequence++));
        costs[start] = 0;

        while (open.TryDequeue(out Hex current, out (int Cost, int Seq) priority))
        {
            if (costs.TryGetValue(current, out int known) && priority.Cost > known)
            {
                continue;
            }

            Hex[] neighbors = current.GetNeighbors();

            for (int i = 0; i < neighbors.Length; i++)
            {
                Hex neighbor = neighbors[i];

                if (!map.Grid.Contains(neighbor) || !map.IsPassable(neighbor))
                {
                    continue;
                }

                int tentative = checked(priority.Cost + map.GetMovementCost(neighbor));

                if (tentative > maxCost)
                {
                    continue;
                }

                if (costs.TryGetValue(neighbor, out int existing) && tentative >= existing)
                {
                    continue;
                }

                costs[neighbor] = tentative;

                if (blockedHexes is not null && blockedHexes.Contains(neighbor))
                {
                    // Гекс с чужой партией достижим как цель, но не расширяет область.
                    continue;
                }

                open.Enqueue(neighbor, (tentative, (int)sequence++));
            }
        }

        return costs;
    }

    private static int Heuristic(Hex from, Hex goal)
    {
        return Hex.Distance(from, goal);
    }

    private static IReadOnlyList<Hex> Reconstruct(
        IReadOnlyDictionary<Hex, Hex> cameFrom,
        Hex start,
        Hex goal)
    {
        List<Hex> path = new() { goal };
        Hex current = goal;

        while (current != start)
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
