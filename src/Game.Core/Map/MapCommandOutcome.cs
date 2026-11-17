namespace Game.Core.Map;

/// <summary>
/// Итог применения команды карты. <see cref="Collision"/> означает, что партия
/// переместилась, но на целевом гексе обнаружен блокирующий объект или партия:
/// бой не разрешается автоматически и передаётся экрану боя (этап 05).
/// </summary>
public enum MapCommandOutcome
{
    /// <summary>Команда применена без особых последствий.</summary>
    Applied = 0,

    /// <summary>День завершён.</summary>
    DayEnded = 1,

    /// <summary>Партия вошла в неохраняемый объект.</summary>
    ObjectEntered = 2,

    /// <summary>Партия переместилась и встретила блокирующий объект.</summary>
    Collision = 3,

    /// <summary>Партия не найдена.</summary>
    InvalidParty = 4,

    /// <summary>Путь не существует.</summary>
    NoPath = 5,

    /// <summary>Недостаточно очков движения.</summary>
    NotEnoughMovement = 6,

    /// <summary>Целевой гекс непроходим.</summary>
    Impassable = 7,

    /// <summary>Целевой гекс вне карты.</summary>
    OutOfBounds = 8,

    /// <summary>Объект не найден на гексе партии.</summary>
    InvalidObject = 9
}
