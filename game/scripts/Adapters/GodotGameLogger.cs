using Godot;
using Game.Core.Diagnostics;

/// <summary>
/// Журнал ядра поверх Godot: Info и Debug идут в <c>GD.Print</c>,
/// Warning — в <c>GD.PushWarning</c>, Error и Critical — в <c>GD.PushError</c>.
/// </summary>
public sealed class GodotGameLogger : IGameLogger
{
    private readonly LogLevel _minimumLevel;

    /// <summary>
    /// Создаёт журнал с порогом <see cref="LogLevel.Info"/>.
    /// </summary>
    /// <param name="minimumLevel">Минимальный уровень записей.</param>
    public GodotGameLogger(LogLevel minimumLevel = LogLevel.Info)
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

        string line = $"[{logEvent.Level}] {logEvent.Category}: {logEvent.Message}";

        switch (logEvent.Level)
        {
            case LogLevel.Warning:
                GD.PushWarning(line);
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                GD.PushError(line);
                break;
            default:
                GD.Print(line);
                break;
        }
    }
}
