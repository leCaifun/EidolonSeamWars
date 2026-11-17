using System.Collections.ObjectModel;

namespace Game.Core.Map;

/// <summary>
/// Запрос на процедурную генерацию карты: мастер-сид, параметры и стартовые
/// позиции сторон. Один и тот же запрос обязан давать одну и ту же карту
/// независимо от платформы (ADR-027).
/// </summary>
public sealed class MapStartRequest
{
    private readonly ReadOnlyCollection<MapStartDefinition> _starts;

    /// <summary>
    /// Создаёт запрос генерации.
    /// </summary>
    /// <param name="masterSeed">Мастер-сид партии.</param>
    /// <param name="settings">Параметры генерации.</param>
    /// <param name="starts">Стартовые позиции сторон; минимум одна.</param>
    /// <exception cref="ArgumentNullException">Параметры или старты не заданы.</exception>
    /// <exception cref="ArgumentException">Список стартов пуст.</exception>
    public MapStartRequest(
        ulong masterSeed,
        MapGenerationSettings settings,
        IEnumerable<MapStartDefinition> starts)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(starts);

        List<MapStartDefinition> list = new(starts);

        if (list.Count == 0)
        {
            throw new ArgumentException("Запрос генерации должен содержать хотя бы один старт.", nameof(starts));
        }

        MasterSeed = masterSeed;
        Settings = settings;
        _starts = new ReadOnlyCollection<MapStartDefinition>(list);
    }

    /// <summary>Мастер-сид партии.</summary>
    public ulong MasterSeed { get; }

    /// <summary>Параметры генерации.</summary>
    public MapGenerationSettings Settings { get; }

    /// <summary>Стартовые позиции сторон.</summary>
    public IReadOnlyList<MapStartDefinition> Starts => _starts;
}
