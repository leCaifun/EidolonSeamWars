using System.Collections.ObjectModel;
using Game.Core.Diagnostics;

namespace Game.Core.Domain;

/// <summary>
/// Поселение (GDD §5.3): владелец, фракция-шаблон, уровень, здания и гарнизон.
/// Захват реализован по упрощённому правилу MVP (ADR-013): здания удаляются,
/// гарнизон очищается, уровень сбрасывается до 1, шаблон меняется на фракцию
/// захватчика.
/// </summary>
public sealed class Settlement
{
    /// <summary>Минимальный уровень поселения.</summary>
    public const int MinLevel = 1;

    /// <summary>Максимально допустимый уровень поселения в MVP.</summary>
    public const int MaxLevelCap = 4;

    private readonly List<string> _buildingIds = new();
    private readonly ReadOnlyCollection<string> _readOnlyBuildingIds;
    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт поселение.
    /// </summary>
    /// <param name="id">Стабильный строковый ID.</param>
    /// <param name="nameKey">Ключ локализации названия.</param>
    /// <param name="ownerFactionId">ID фракции-владельца.</param>
    /// <param name="templateFactionId">ID фракции-шаблона.</param>
    /// <param name="maxLevel">Максимальный уровень фракции, 1…4.</param>
    /// <param name="level">Текущий уровень, 1…<paramref name="maxLevel"/>.</param>
    /// <param name="buildingSlotCount">Число слотов зданий.</param>
    /// <param name="garrison">Гарнизон; при null создаётся пустая армия без героя.</param>
    /// <param name="logger">Необязательный журнал доменных операций.</param>
    /// <exception cref="ArgumentException">Пустой ID, ключ или ID фракции.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Уровни или число слотов вне допустимого диапазона.</exception>
    public Settlement(
        string id,
        string nameKey,
        string ownerFactionId,
        string templateFactionId,
        int maxLevel,
        int level,
        int buildingSlotCount,
        Army? garrison = null,
        IGameLogger? logger = null)
    {
        Id = RequireText(id, nameof(id));
        NameKey = RequireText(nameKey, nameof(nameKey));
        OwnerFactionId = RequireText(ownerFactionId, nameof(ownerFactionId));
        TemplateFactionId = RequireText(templateFactionId, nameof(templateFactionId));

        if (maxLevel < MinLevel || maxLevel > MaxLevelCap)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxLevel),
                maxLevel,
                $"Максимальный уровень поселения должен быть в диапазоне {MinLevel}…{MaxLevelCap}.");
        }

        if (level < MinLevel || level > maxLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level),
                level,
                $"Уровень поселения должен быть в диапазоне {MinLevel}…{maxLevel}.");
        }

        if (buildingSlotCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingSlotCount), buildingSlotCount, "Число слотов зданий не может быть отрицательным.");
        }

        MaxLevel = maxLevel;
        Level = level;
        BuildingSlotCount = buildingSlotCount;
        Garrison = garrison ?? Army.WithoutHero();
        _logger = logger;
        _readOnlyBuildingIds = new ReadOnlyCollection<string>(_buildingIds);
    }

    /// <summary>Стабильный строковый ID поселения.</summary>
    public string Id { get; }

    /// <summary>Ключ локализации названия.</summary>
    public string NameKey { get; }

    /// <summary>ID фракции-владельца.</summary>
    public string OwnerFactionId { get; private set; }

    /// <summary>ID фракции-шаблона, определяющей здания и юнитов.</summary>
    public string TemplateFactionId { get; private set; }

    /// <summary>Максимальный уровень для текущей фракции-шаблона.</summary>
    public int MaxLevel { get; private set; }

    /// <summary>Текущий уровень.</summary>
    public int Level { get; private set; }

    /// <summary>Число слотов под здания.</summary>
    public int BuildingSlotCount { get; }

    /// <summary>Построенные здания в порядке добавления.</summary>
    public IReadOnlyList<string> BuildingIds => _readOnlyBuildingIds;

    /// <summary>Свободные слоты зданий.</summary>
    public int FreeBuildingSlots => BuildingSlotCount - _buildingIds.Count;

    /// <summary>Гарнизон поселения; без героя ёмкость фиксирована.</summary>
    public Army Garrison { get; }

    /// <summary>
    /// Пытается построить здание. Дубликаты и переполнение слотов запрещены.
    /// </summary>
    /// <exception cref="ArgumentException">ID здания пуст.</exception>
    public bool TryAddBuilding(string buildingId)
    {
        string validated = RequireText(buildingId, nameof(buildingId));

        if (FreeBuildingSlots <= 0 || _buildingIds.Contains(validated, StringComparer.Ordinal))
        {
            return false;
        }

        _buildingIds.Add(validated);
        _logger?.Log(LogEvent.Create(
            LogLevel.Info,
            "Economy",
            $"В поселении «{Id}» построено здание «{validated}».",
            correlationId: Id,
            fields: new Dictionary<string, string>
            {
                ["settlementId"] = Id,
                ["buildingId"] = validated
            }));

        return true;
    }

    /// <summary>
    /// Удаляет здание по ID. Возвращает <c>false</c>, если здания не было.
    /// </summary>
    /// <exception cref="ArgumentException">ID здания пуст.</exception>
    public bool RemoveBuilding(string buildingId)
    {
        string validated = RequireText(buildingId, nameof(buildingId));
        return _buildingIds.Remove(validated);
    }

    /// <summary>
    /// Захватывает поселение по правилу MVP (ADR-013).
    /// </summary>
    /// <param name="newOwnerFactionId">ID фракции захватчика.</param>
    /// <param name="newMaxLevel">Максимальный уровень фракции захватчика, 1…4.</param>
    /// <exception cref="ArgumentException">ID фракции пуст.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Максимальный уровень вне 1…4.</exception>
    public void Capture(string newOwnerFactionId, int newMaxLevel)
    {
        string validatedOwner = RequireText(newOwnerFactionId, nameof(newOwnerFactionId));

        if (newMaxLevel < MinLevel || newMaxLevel > MaxLevelCap)
        {
            throw new ArgumentOutOfRangeException(
                nameof(newMaxLevel),
                newMaxLevel,
                $"Максимальный уровень поселения должен быть в диапазоне {MinLevel}…{MaxLevelCap}.");
        }

        string previousOwner = OwnerFactionId;
        int previousLevel = Level;
        int removedBuildings = _buildingIds.Count;

        _buildingIds.Clear();
        Garrison.Clear();

        OwnerFactionId = validatedOwner;
        TemplateFactionId = validatedOwner;
        MaxLevel = newMaxLevel;
        Level = MinLevel;

        _logger?.Log(LogEvent.Create(
            LogLevel.Info,
            "Economy",
            $"Поселение «{Id}» захвачено фракцией «{validatedOwner}»: уровень {previousLevel}→{Level}, удалено зданий {removedBuildings}, гарнизон очищен.",
            correlationId: Id,
            fields: new Dictionary<string, string>
            {
                ["settlementId"] = Id,
                ["previousOwner"] = previousOwner,
                ["newOwner"] = validatedOwner,
                ["previousLevel"] = previousLevel.ToString(),
                ["removedBuildings"] = removedBuildings.ToString()
            }));
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Строковое поле поселения не может быть пустым.", parameterName);
        }

        return value;
    }
}
