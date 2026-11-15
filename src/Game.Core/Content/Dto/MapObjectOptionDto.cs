namespace Game.Core.Content.Dto;

/// <summary>Вариант взаимодействия с объектом карты.</summary>
public sealed class MapObjectOptionDto
{
    /// <summary>Локальный ID варианта внутри объекта.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Ключ локализации названия.</summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>Ключ локализации описания.</summary>
    public string DescriptionKey { get; init; } = string.Empty;

    /// <summary>Требуется ли бой.</summary>
    public bool RequiresBattle { get; init; }

    /// <summary>Стоимость варианта в ресурсах.</summary>
    public IReadOnlyDictionary<string, int> Cost { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Награда: ресурсы или <c>reputation</c>.</summary>
    public IReadOnlyDictionary<string, int> Reward { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Задержка отложенной награды в днях.</summary>
    public int? DelayedRewardDays { get; init; }

    /// <summary>Отложенная награда.</summary>
    public IReadOnlyDictionary<string, int> DelayedReward { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);
}
