using Game.Core.Domain;

namespace Game.Core.Map;

/// <summary>
/// Экземпляр объекта на конкретном гексе карты. Ссылается на иммутабельное
/// описание <see cref="Definition"/> и хранит состояние экземпляра: владельца
/// и признак посещения. Изменяется только через команды карты.
/// </summary>
public sealed class MapObjectInstance
{
    /// <summary>
    /// Создаёт экземпляр объекта.
    /// </summary>
    /// <param name="instanceId">Уникальный ID экземпляра на карте.</param>
    /// <param name="definition">Описание объекта из контента.</param>
    /// <param name="position">Гекс размещения.</param>
    /// <param name="ownerFactionId">ID фракции-владельца; null — нейтральный.</param>
    /// <exception cref="ArgumentNullException">Описание не задано.</exception>
    /// <exception cref="ArgumentException">Пустой ID экземпляра.</exception>
    public MapObjectInstance(
        string instanceId,
        MapObjectDefinition definition,
        Hex position,
        string? ownerFactionId = null)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("ID экземпляра объекта не может быть пустым.", nameof(instanceId));
        }

        InstanceId = instanceId;
        Definition = definition;
        Position = position;
        OwnerFactionId = ownerFactionId;
    }

    /// <summary>Уникальный ID экземпляра на карте.</summary>
    public string InstanceId { get; }

    /// <summary>Описание объекта из контента.</summary>
    public MapObjectDefinition Definition { get; }

    /// <summary>Гекс размещения.</summary>
    public Hex Position { get; }

    /// <summary>ID фракции-владельца; null — нейтральный объект.</summary>
    public string? OwnerFactionId { get; private set; }

    /// <summary>Посещён ли объект партией.</summary>
    public bool Visited { get; private set; }

    /// <summary>Стабильный ID контента объекта.</summary>
    public string DefinitionId => Definition.Id;

    /// <summary>Тип объекта из схемы.</summary>
    public string Type => Definition.Type;

    /// <summary>
    /// Назначает владельца объекта. Используется командами захвата в следующих этапах.
    /// </summary>
    public void SetOwner(string? ownerFactionId)
    {
        OwnerFactionId = ownerFactionId;
    }

    /// <summary>Отмечает объект как посещённый.</summary>
    public void MarkVisited()
    {
        Visited = true;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"MapObjectInstance({InstanceId}, {DefinitionId}, {Position})";
    }
}
