using System.Collections.ObjectModel;
using Game.Core.Domain;

namespace Game.Core.Map;

/// <summary>
/// Результат поиска пути: успех, суммарная стоимость входа и маршрут от старта
/// до цели включительно. Стоимость стартового гекса равна нулю.
/// </summary>
public sealed class PathResult
{
    private static readonly ReadOnlyCollection<Hex> EmptyPath =
        new(new List<Hex>());

    private PathResult(bool success, int cost, IReadOnlyList<Hex> path, PathFailure failure)
    {
        Success = success;
        Cost = cost;
        Path = path;
        Failure = failure;
    }

    /// <summary>Найден ли путь.</summary>
    public bool Success { get; }

    /// <summary>Суммарная стоимость входа по маршруту.</summary>
    public int Cost { get; }

    /// <summary>Маршрут от старта до цели включительно; пуст при неудаче.</summary>
    public IReadOnlyList<Hex> Path { get; }

    /// <summary>Причина неудачи; <see cref="PathFailure.None"/> при успехе.</summary>
    public PathFailure Failure { get; }

    /// <summary>Создаёт успешный результат.</summary>
    public static PathResult Found(int cost, IReadOnlyList<Hex> path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return new PathResult(true, cost, new ReadOnlyCollection<Hex>(path.ToArray()), PathFailure.None);
    }

    /// <summary>Создаёт результат неудачи.</summary>
    public static PathResult Fail(PathFailure failure)
    {
        if (failure == PathFailure.None)
        {
            throw new ArgumentOutOfRangeException(nameof(failure), failure, "Неудача не может иметь причину None.");
        }

        return new PathResult(false, 0, EmptyPath, failure);
    }
}
