namespace Game.Core.Map;

/// <summary>
/// Причина неудачи поиска пути. <see cref="None"/> соответствует успеху.
/// </summary>
public enum PathFailure
{
    /// <summary>Путь найден.</summary>
    None = 0,

    /// <summary>Путь не существует.</summary>
    NoPath = 1,

    /// <summary>Целевой гекс непроходим.</summary>
    ImpassableGoal = 2,

    /// <summary>Стартовый или целевой гекс вне карты.</summary>
    OutOfBounds = 3,

    /// <summary>Стартовый гекс непроходим.</summary>
    ImpassableStart = 4
}
