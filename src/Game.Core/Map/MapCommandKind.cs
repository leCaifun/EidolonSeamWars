namespace Game.Core.Map;

/// <summary>
/// Тип команды карты. Любое изменение состояния глобальной карты выполняется
/// командой (ADR-018); команды сериализуемы и образуют журнал реплея.
/// </summary>
public enum MapCommandKind
{
    /// <summary>Движение партии на целевой гекс.</summary>
    MoveParty = 0,

    /// <summary>Завершение дня: сброс движения и дневной доход.</summary>
    EndDay = 1,

    /// <summary>Вход в объект на текущем гексе партии.</summary>
    EnterObject = 2
}
