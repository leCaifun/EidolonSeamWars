using Game.Core.Domain;

namespace Game.Core.Map;

/// <summary>
/// Результат применения команды карты. Содержит итог, потраченные очки движения,
/// конечную позицию и сведения о встрече. Бой не разрешается автоматически:
/// при <see cref="MapCommandOutcome.Collision"/> экран карты обязан открыть
/// честную заглушку боя или передать управление этапу 05.
/// </summary>
public sealed class MapCommandResult
{
    private MapCommandResult(
        MapCommandKind kind,
        MapCommandOutcome outcome,
        string? partyId,
        int cost,
        Hex? destination,
        string? blockingDefinitionId,
        string? blockingInstanceId,
        string? encounterPartyId)
    {
        Kind = kind;
        Outcome = outcome;
        PartyId = partyId;
        Cost = cost;
        Destination = destination;
        BlockingDefinitionId = blockingDefinitionId;
        BlockingInstanceId = blockingInstanceId;
        EncounterPartyId = encounterPartyId;
    }

    /// <summary>Тип применённой команды.</summary>
    public MapCommandKind Kind { get; }

    /// <summary>Итог применения.</summary>
    public MapCommandOutcome Outcome { get; }

    /// <summary>ID партии; null для команд без партии.</summary>
    public string? PartyId { get; }

    /// <summary>Потраченные очки движения.</summary>
    public int Cost { get; }

    /// <summary>Позиция партии после команды.</summary>
    public Hex? Destination { get; }

    /// <summary>Стабильный ID контента блокирующего объекта.</summary>
    public string? BlockingDefinitionId { get; }

    /// <summary>ID экземпляра блокирующего объекта на карте.</summary>
    public string? BlockingInstanceId { get; }

    /// <summary>ID партии, с которой произошла встреча.</summary>
    public string? EncounterPartyId { get; }

    /// <summary>
    /// Успешно ли применена команда. Столкновение считается успешным ходом:
    /// партия переместилась, а бой передаётся отдельному экрану.
    /// </summary>
    public bool IsSuccess => Outcome is
        MapCommandOutcome.Applied or
        MapCommandOutcome.DayEnded or
        MapCommandOutcome.ObjectEntered or
        MapCommandOutcome.Collision;

    /// <summary>Создаёт результат отказа без изменения состояния.</summary>
    public static MapCommandResult Rejected(
        MapCommandKind kind,
        string? partyId,
        MapCommandOutcome outcome)
    {
        if (outcome is MapCommandOutcome.Applied or
            MapCommandOutcome.DayEnded or
            MapCommandOutcome.ObjectEntered or
            MapCommandOutcome.Collision)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "Отказ не может иметь успешный итог.");
        }

        return new MapCommandResult(kind, outcome, partyId, 0, null, null, null, null);
    }

    /// <summary>Создаёт успешный результат движения.</summary>
    public static MapCommandResult Moved(
        MapCommandKind kind,
        string partyId,
        int cost,
        Hex destination,
        MapCommandOutcome outcome,
        string? blockingDefinitionId = null,
        string? blockingInstanceId = null,
        string? encounterPartyId = null)
    {
        return new MapCommandResult(
            kind,
            outcome,
            partyId,
            cost,
            destination,
            blockingDefinitionId,
            blockingInstanceId,
            encounterPartyId);
    }

    /// <summary>Создаёт результат завершения дня.</summary>
    public static MapCommandResult DayEnded()
    {
        return new MapCommandResult(
            MapCommandKind.EndDay,
            MapCommandOutcome.DayEnded,
            null,
            0,
            null,
            null,
            null,
            null);
    }

    /// <summary>Создаёт результат входа в неохраняемый объект.</summary>
    public static MapCommandResult ObjectEntered(
        string partyId,
        string definitionId,
        string instanceId)
    {
        return new MapCommandResult(
            MapCommandKind.EnterObject,
            MapCommandOutcome.ObjectEntered,
            partyId,
            0,
            null,
            definitionId,
            instanceId,
            null);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"MapCommandResult({Kind}, {Outcome}, party={PartyId}, cost={Cost})";
    }
}
