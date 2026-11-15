using System.Collections.ObjectModel;
using Game.Core.Domain;

namespace Game.Core.Economy;

/// <summary>
/// Состояние экономики, обрабатываемое дневным тиком (ADR-004, GDD §5.2).
/// Домен хранит казну, аккумуляторы прироста, жилища, контракты и эффекты;
/// сам тик не знает о карте, UI и файлах.
/// </summary>
public sealed class DailyTickState
{
    private readonly List<GrowthAccumulator> _growthAccumulators = new();
    private readonly List<HousingStock> _housingStocks = new();
    private readonly List<TimedEffect> _timedEffects = new();

    private readonly ReadOnlyCollection<GrowthAccumulator> _readOnlyGrowth;
    private readonly ReadOnlyCollection<HousingStock> _readOnlyHousing;
    private readonly ReadOnlyCollection<TimedEffect> _readOnlyEffects;

    /// <summary>
    /// Создаёт состояние экономики.
    /// </summary>
    /// <param name="treasury">Начальная казна; при null — пустая.</param>
    /// <param name="incomePerDay">Дневной доход поселений и шахт; при null — нулевой.</param>
    /// <param name="day">Текущий день, начиная с 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">День меньше 1.</exception>
    public DailyTickState(ResourcePool? treasury = null, ResourcePool? incomePerDay = null, int day = 1)
    {
        if (day < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(day), day, "Текущий день не может быть меньше 1.");
        }

        Treasury = treasury ?? ResourcePool.Empty;
        IncomePerDay = incomePerDay ?? ResourcePool.Empty;
        Day = day;
        RecruitmentBoard = new RecruitmentBoard();

        _readOnlyGrowth = new ReadOnlyCollection<GrowthAccumulator>(_growthAccumulators);
        _readOnlyHousing = new ReadOnlyCollection<HousingStock>(_housingStocks);
        _readOnlyEffects = new ReadOnlyCollection<TimedEffect>(_timedEffects);
    }

    /// <summary>Текущий день.</summary>
    public int Day { get; internal set; }

    /// <summary>Казна: семь ресурсов.</summary>
    public ResourcePool Treasury { get; internal set; }

    /// <summary>Дневной доход поселений и шахт.</summary>
    public ResourcePool IncomePerDay { get; set; }

    /// <summary>Аккумуляторы прироста юнитов.</summary>
    public IReadOnlyList<GrowthAccumulator> GrowthAccumulators => _readOnlyGrowth;

    /// <summary>Внешние жилища.</summary>
    public IReadOnlyList<HousingStock> HousingStocks => _readOnlyHousing;

    /// <summary>Активные контракты наёмников.</summary>
    public RecruitmentBoard RecruitmentBoard { get; }

    /// <summary>Эффекты с длительностью в днях.</summary>
    public IReadOnlyList<TimedEffect> TimedEffects => _readOnlyEffects;

    /// <summary>Добавляет аккумулятор прироста.</summary>
    /// <exception cref="ArgumentNullException">Аккумулятор не задан.</exception>
    public void AddGrowthAccumulator(GrowthAccumulator accumulator)
    {
        ArgumentNullException.ThrowIfNull(accumulator);
        _growthAccumulators.Add(accumulator);
    }

    /// <summary>Добавляет внешнее жилище.</summary>
    /// <exception cref="ArgumentNullException">Жилище не задано.</exception>
    public void AddHousingStock(HousingStock housing)
    {
        ArgumentNullException.ThrowIfNull(housing);
        _housingStocks.Add(housing);
    }

    /// <summary>Добавляет эффект с длительностью.</summary>
    /// <exception cref="ArgumentNullException">Эффект не задан.</exception>
    public void AddTimedEffect(TimedEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        _timedEffects.Add(effect);
    }

    internal void RemoveTimedEffectAt(int index)
    {
        _timedEffects.RemoveAt(index);
    }
}
