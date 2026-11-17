using Game.Core.Domain;

namespace Game.Core.Map;

/// <summary>
/// Размещение объекта на карте в терминах контента: стабильный ID экземпляра,
/// ID определения, гекс и необязательный владелец. Используется загрузчиком
/// рукотворных карт и генератором как промежуточное представление.
/// </summary>
public sealed class MapLayoutObject
{
    /// <summary>
    /// Создаёт размещение объекта.
    /// </summary>
    /// <param name="instanceId">Уникальный ID экземпляра на карте.</param>
    /// <param name="definitionId">Стабильный ID определения объекта.</param>
    /// <param name="position">Гекс размещения.</param>
    /// <param name="ownerFactionId">ID фракции-владельца; null — нейтральный.</param>
    /// <exception cref="ArgumentException">ID экземпляра или определения пуст.</exception>
    public MapLayoutObject(
        string instanceId,
        string definitionId,
        Hex position,
        string? ownerFactionId = null)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("ID экземпляра объекта не может быть пустым.", nameof(instanceId));
        }

        if (string.IsNullOrWhiteSpace(definitionId))
        {
            throw new ArgumentException("ID определения объекта не может быть пустым.", nameof(definitionId));
        }

        InstanceId = instanceId;
        DefinitionId = definitionId;
        Position = position;
        OwnerFactionId = ownerFactionId;
    }

    /// <summary>Уникальный ID экземпляра на карте.</summary>
    public string InstanceId { get; }

    /// <summary>Стабильный ID определения объекта.</summary>
    public string DefinitionId { get; }

    /// <summary>Гекс размещения.</summary>
    public Hex Position { get; }

    /// <summary>ID фракции-владельца; null — нейтральный объект.</summary>
    public string? OwnerFactionId { get; }
}
