namespace Game.Core.Diagnostics;

/// <summary>
/// Абстракция журналирования ядра. Реализации (файл, консоль, движок) находятся вне Game.Core.
/// Обязательные точки логирования перечислены в docs/LOGGING.md.
/// </summary>
public interface IGameLogger
{
    /// <summary>
    /// Проверяет, будет ли запись указанного уровня принята реализацией.
    /// Позволяет не строить дорогие поля для отфильтрованных записей.
    /// </summary>
    /// <param name="level">Проверяемый уровень.</param>
    bool IsEnabled(LogLevel level);

    /// <summary>
    /// Записывает событие журнала. Реализация обязана быть безопасной для вызова
    /// из любого потока и не бросать исключений при ошибках ввода-вывода.
    /// </summary>
    /// <param name="logEvent">Событие журнала.</param>
    void Log(LogEvent logEvent);
}
