using Game.Core.Domain;

namespace Game.Core.Map;

/// <summary>
/// Команда изменения состояния глобальной карты (ADR-018). Команды иммутабельны,
/// сериализуемы и вместе с мастер-сидом образуют журнал реплея.
/// Прямое изменение позиции партии в обход команды запрещено.
/// </summary>
public sealed class MapCommand
{
    private MapCommand(
        MapCommandKind kind,
        string? partyId,
        Hex? target,
        string? objectInstanceId)
    {
        Kind = kind;
        PartyId = partyId;
        Target = target;
        ObjectInstanceId = objectInstanceId;
    }

    /// <summary>Тип команды.</summary>
    public MapCommandKind Kind { get; }

    /// <summary>ID партии; null для команд, не привязанных к партии.</summary>
    public string? PartyId { get; }

    /// <summary>Целевой гекс для команды движения.</summary>
    public Hex? Target { get; }

    /// <summary>ID экземпляра объекта для команды входа в объект.</summary>
    public string? ObjectInstanceId { get; }

    /// <summary>
    /// Создаёт команду движения партии на целевой гекс.
    /// </summary>
    /// <param name="partyId">ID партии.</param>
    /// <param name="target">Целевой гекс.</param>
    /// <exception cref="ArgumentException">ID партии пуст.</exception>
    public static MapCommand MoveParty(string partyId, Hex target)
    {
        return new MapCommand(
            MapCommandKind.MoveParty,
            RequirePartyId(partyId),
            target,
            null);
    }

    /// <summary>
    /// Создаёт команду завершения дня.
    /// </summary>
    public static MapCommand EndDay()
    {
        return new MapCommand(MapCommandKind.EndDay, null, null, null);
    }

    /// <summary>
    /// Создаёт команду входа в объект на текущем гексе партии.
    /// </summary>
    /// <param name="partyId">ID партии.</param>
    /// <param name="objectInstanceId">ID экземпляра объекта.</param>
    /// <exception cref="ArgumentException">ID партии или объекта пуст.</exception>
    public static MapCommand EnterObject(string partyId, string objectInstanceId)
    {
        if (string.IsNullOrWhiteSpace(objectInstanceId))
        {
            throw new ArgumentException("ID экземпляра объекта не может быть пустым.", nameof(objectInstanceId));
        }

        return new MapCommand(
            MapCommandKind.EnterObject,
            RequirePartyId(partyId),
            null,
            objectInstanceId);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Kind switch
        {
            MapCommandKind.MoveParty => $"MoveParty({PartyId} → {Target})",
            MapCommandKind.EndDay => "EndDay()",
            MapCommandKind.EnterObject => $"EnterObject({PartyId}, {ObjectInstanceId})",
            _ => $"MapCommand({Kind})"
        };
    }

    private static string RequirePartyId(string partyId)
    {
        if (string.IsNullOrWhiteSpace(partyId))
        {
            throw new ArgumentException("ID партии не может быть пустым.", nameof(partyId));
        }

        return partyId;
    }
}
