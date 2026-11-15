using Game.Core.Diagnostics;

namespace Game.Core.Tests;

/// <summary>
/// Тестовый журнал: сохраняет все записи в памяти для проверки обязательного
/// логирования в уязвимых местах домена.
/// </summary>
internal sealed class TestLogger : IGameLogger
{
    public List<LogEvent> Events { get; } = new();

    public bool IsEnabled(LogLevel level) => true;

    public void Log(LogEvent logEvent) => Events.Add(logEvent);
}
