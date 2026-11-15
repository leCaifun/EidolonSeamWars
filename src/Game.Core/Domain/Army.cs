using System.Collections.ObjectModel;
using Game.Core.Diagnostics;

namespace Game.Core.Domain;

/// <summary>
/// Армия из ограниченного числа слотов с жёстким лимитом лидерства (ADR-006).
/// Ёмкость задаётся при создании: у героя она равна <see cref="Hero.Leadership"/>,
/// у отряда без героя — <see cref="HeroLessCapacity"/>.
/// Перегруз невозможен: добавление и слияние проверяют лимит до изменения состояния.
/// </summary>
public sealed class Army
{
    /// <summary>Базовое число слотов армии (GDD §6.2).</summary>
    public const int DefaultSlotCount = 5;

    /// <summary>Максимальное число слотов, зарезервированное под будущие расширения.</summary>
    public const int MaxSlotCount = 7;

    /// <summary>Ёмкость лидерства отряда без героя (ADR-006).</summary>
    public const int HeroLessCapacity = 50;

    private readonly List<Stack> _slots = new();
    private readonly ReadOnlyCollection<Stack> _readOnlySlots;
    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт армию с указанной ёмкостью лидерства и числом слотов.
    /// </summary>
    /// <param name="capacity">Ёмкость лидерства; должна быть положительной.</param>
    /// <param name="slotCount">Число слотов, 1…<see cref="MaxSlotCount"/>.</param>
    /// <param name="logger">Необязательный журнал доменных операций.</param>
    /// <exception cref="ArgumentOutOfRangeException">Ёмкость или число слотов вне допустимого диапазона.</exception>
    public Army(int capacity, int slotCount = DefaultSlotCount, IGameLogger? logger = null)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Ёмкость лидерства должна быть положительной.");
        }

        if (slotCount < 1 || slotCount > MaxSlotCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotCount),
                slotCount,
                $"Число слотов армии должно быть в диапазоне 1…{MaxSlotCount}.");
        }

        Capacity = capacity;
        SlotCount = slotCount;
        _logger = logger;
        _readOnlySlots = new ReadOnlyCollection<Stack>(_slots);
    }

    /// <summary>Ёмкость лидерства армии.</summary>
    public int Capacity { get; }

    /// <summary>Число доступных слотов.</summary>
    public int SlotCount { get; }

    /// <summary>Слоты армии в порядке добавления; пустые стаки удаляются.</summary>
    public IReadOnlyList<Stack> Slots => _readOnlySlots;

    /// <summary>Занятое лидерство с учётом количества юнитов.</summary>
    public int UsedLeadership
    {
        get
        {
            int total = 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                total = checked(total + _slots[i].LeadershipCostTotal);
            }

            return total;
        }
    }

    /// <summary>Остаток лидерства; никогда не отрицателен.</summary>
    public int RemainingLeadership => Capacity - UsedLeadership;

    /// <summary>Создаёт армию без героя с фиксированной ёмкостью 50.</summary>
    public static Army WithoutHero(int slotCount = DefaultSlotCount, IGameLogger? logger = null)
    {
        return new Army(HeroLessCapacity, slotCount, logger);
    }

    /// <summary>
    /// Проверяет, можно ли добавить стак, не нарушая лимит лидерства и слотов.
    /// </summary>
    /// <exception cref="ArgumentNullException">Стак не задан.</exception>
    public bool CanAdd(Stack stack)
    {
        ArgumentNullException.ThrowIfNull(stack);

        if (stack.IsEmpty || _slots.Count >= SlotCount)
        {
            return false;
        }

        return UsedLeadership + stack.LeadershipCostTotal <= Capacity;
    }

    /// <summary>
    /// Пытается добавить стак. При отказе армия остаётся неизменной.
    /// </summary>
    /// <exception cref="ArgumentNullException">Стак не задан.</exception>
    public bool TryAdd(Stack stack)
    {
        ArgumentNullException.ThrowIfNull(stack);

        if (!CanAdd(stack))
        {
            _logger?.Log(LogEvent.Create(
                LogLevel.Debug,
                "Economy",
                $"Отряд «{stack.UnitDefId}» × {stack.Count} не добавлен: лидерство {UsedLeadership + stack.LeadershipCostTotal}/{Capacity}, слотов {_slots.Count}/{SlotCount}."));
            return false;
        }

        _slots.Add(stack);
        return true;
    }

    /// <summary>
    /// Удаляет стак по индексу слота.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Индекс вне диапазона слотов.</exception>
    public Stack RemoveAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotIndex),
                slotIndex,
                $"Индекс слота должен быть в диапазоне 0…{_slots.Count - 1}.");
        }

        Stack removed = _slots[slotIndex];
        _slots.RemoveAt(slotIndex);
        return removed;
    }

    /// <summary>
    /// Пытается слить исходный стак в целевой. Стаки должны быть одного юнита.
    /// При отказе армия остаётся неизменной.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Индекс вне диапазона слотов.</exception>
    public bool TryMerge(int sourceIndex, int targetIndex)
    {
        EnsureIndex(sourceIndex, nameof(sourceIndex));
        EnsureIndex(targetIndex, nameof(targetIndex));

        if (sourceIndex == targetIndex)
        {
            throw new ArgumentException("Нельзя слить стак сам с собой.", nameof(targetIndex));
        }

        Stack source = _slots[sourceIndex];
        Stack target = _slots[targetIndex];

        if (!string.Equals(source.UnitDefId, target.UnitDefId, StringComparison.Ordinal))
        {
            return false;
        }

        target.Merge(source);
        _slots.RemoveAt(sourceIndex);
        return true;
    }

    /// <summary>Полностью очищает армию, например при разгроме или захвате поселения.</summary>
    public void Clear()
    {
        _slots.Clear();
    }

    private void EnsureIndex(int index, string parameterName)
    {
        if (index < 0 || index >= _slots.Count)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                index,
                $"Индекс слота должен быть в диапазоне 0…{_slots.Count - 1}.");
        }
    }
}
