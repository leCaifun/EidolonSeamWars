using Game.Core.Diagnostics;

namespace Game.Core.Events;

/// <summary>
/// Простая синхронная шина доменных событий. Не является механизмом команд:
/// состояние меняется только через BattleCommand / MapCommand (AGENTS.md, инвариант 7).
/// Класс не потокобезопасен по семантике доставки: publish и subscribe предполагаются
/// в одном игровом потоке; внутренние структуры защищены от конкурентного изменения списка.
/// </summary>
public sealed class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _sync = new();
    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт шину событий.
    /// </summary>
    /// <param name="logger">Необязательный журнал для диагностики доставки и ошибок обработчиков.</param>
    public EventBus(IGameLogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Подписывает обработчик на события указанного типа.
    /// </summary>
    /// <typeparam name="TEvent">Тип доменного события.</typeparam>
    /// <param name="handler">Обработчик события.</param>
    /// <returns>Маркер подписки; вызов Dispose отменяет подписку.</returns>
    /// <exception cref="ArgumentNullException">Обработчик равен null.</exception>
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        Type eventType = typeof(TEvent);
        lock (_sync)
        {
            if (!_handlers.TryGetValue(eventType, out List<Delegate>? handlers))
            {
                handlers = new List<Delegate>();
                _handlers.Add(eventType, handlers);
            }

            handlers.Add(handler);
        }

        return new Subscription(this, eventType, handler);
    }

    /// <summary>
    /// Публикует событие всем подписчикам в порядке подписки.
    /// Исключение обработчика журналируется и пробрасывается вызывающему коду;
    /// последующие обработчики при этом не вызываются.
    /// </summary>
    /// <typeparam name="TEvent">Тип доменного события.</typeparam>
    /// <param name="domainEvent">Публикуемое событие.</param>
    /// <exception cref="ArgumentNullException">Событие равно null.</exception>
    public void Publish<TEvent>(TEvent domainEvent)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        Delegate[] snapshot;
        lock (_sync)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out List<Delegate>? handlers) || handlers.Count == 0)
            {
                LogTrace("Для события {0} нет подписчиков.", typeof(TEvent).Name);
                return;
            }

            snapshot = handlers.ToArray();
        }

        LogTrace("Публикация события {0}, подписчиков: {1}.", typeof(TEvent).Name, snapshot.Length);

        foreach (Delegate handler in snapshot)
        {
            try
            {
                ((Action<TEvent>)handler)(domainEvent);
            }
            catch (Exception exception)
            {
                LogError(exception, "Обработчик события {0} завершился исключением.", typeof(TEvent).Name);
                throw;
            }
        }
    }

    /// <summary>
    /// Проверяет наличие подписчиков на события указанного типа.
    /// </summary>
    /// <typeparam name="TEvent">Тип доменного события.</typeparam>
    public bool HasSubscribers<TEvent>()
        where TEvent : IDomainEvent
    {
        lock (_sync)
        {
            return _handlers.TryGetValue(typeof(TEvent), out List<Delegate>? handlers) && handlers.Count > 0;
        }
    }

    /// <summary>
    /// Удаляет все подписки. Используется при завершении партии и в тестах.
    /// </summary>
    public void Clear()
    {
        lock (_sync)
        {
            _handlers.Clear();
        }
    }

    private void Unsubscribe(Type eventType, Delegate handler)
    {
        lock (_sync)
        {
            if (!_handlers.TryGetValue(eventType, out List<Delegate>? handlers))
            {
                return;
            }

            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                _handlers.Remove(eventType);
            }
        }
    }

    private void LogTrace(string message, params object?[] args)
    {
        if (_logger is null || !_logger.IsEnabled(LogLevel.Trace))
        {
            return;
        }

        _logger.Log(LogEvent.Create(LogLevel.Trace, "Events", string.Format(message, args)));
    }

    private void LogError(Exception exception, string message, params object?[] args)
    {
        if (_logger is null || !_logger.IsEnabled(LogLevel.Error))
        {
            return;
        }

        _logger.Log(LogEvent.Create(
            LogLevel.Error,
            "Events",
            string.Format(message, args),
            fields: new Dictionary<string, string>
            {
                ["exceptionType"] = exception.GetType().Name,
                ["exceptionMessage"] = exception.Message
            }));
    }

    private sealed class Subscription : IDisposable
    {
        private readonly EventBus _owner;
        private readonly Type _eventType;
        private readonly Delegate _handler;
        private bool _disposed;

        public Subscription(EventBus owner, Type eventType, Delegate handler)
        {
            _owner = owner;
            _eventType = eventType;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _owner.Unsubscribe(_eventType, _handler);
        }
    }
}
