using Game.Core.Diagnostics;

namespace Game.SimRunner;

/// <summary>
/// Консольный журнал: пишет структурированные события в stdout/stderr.
/// Ошибки и предупреждения видны всегда, отладочные записи скрыты.
/// </summary>
internal sealed class ConsoleGameLogger : IGameLogger
{
    private readonly LogLevel _minimumLevel;

    /// <summary>
    /// Создаёт консольный журнал.
    /// </summary>
    /// <param name="minimumLevel">Минимальный уровень записей; по умолчанию <see cref="LogLevel.Info"/>.</param>
    public ConsoleGameLogger(LogLevel minimumLevel = LogLevel.Info)
    {
        _minimumLevel = minimumLevel;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel level)
    {
        return level >= _minimumLevel;
    }

    /// <inheritdoc />
    public void Log(LogEvent logEvent)
    {
        if (logEvent is null || !IsEnabled(logEvent.Level))
        {
            return;
        }

        string line =
            $"[{logEvent.Level}] {logEvent.Category}: {logEvent.Message}";

        if (logEvent.Level >= LogLevel.Warning)
        {
            Console.Error.WriteLine(line);
        }
        else
        {
            Console.WriteLine(line);
        }
    }
}
