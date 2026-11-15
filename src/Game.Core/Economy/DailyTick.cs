using Game.Core.Diagnostics;
using Game.Core.Domain;

namespace Game.Core.Economy;

/// <summary>
/// Дневной тик экономики (ADR-004, GDD §5.2). Порядок шагов фиксирован:
/// доход → прирост → регенерация жилищ → контракты найма → длительности →
/// ход ИИ → автосохранение. ИИ и автосохранение — точки расширения.
/// </summary>
public sealed class DailyTick
{
    private readonly IDailyAiStep? _aiStep;
    private readonly IAutosaveStep? _autosaveStep;
    private readonly IRecruitmentContractSource? _contractSource;
    private readonly IGameLogger? _logger;

    /// <summary>
    /// Создаёт дневной тик с необязательными расширениями.
    /// </summary>
    /// <param name="aiStep">Ход ИИ; на этапе 02 может отсутствовать.</param>
    /// <param name="autosaveStep">Автосохранение; появится на этапе 07.</param>
    /// <param name="contractSource">Источник ротации контрактов наёмников.</param>
    /// <param name="logger">Необязательный журнал категории Economy.</param>
    public DailyTick(
        IDailyAiStep? aiStep = null,
        IAutosaveStep? autosaveStep = null,
        IRecruitmentContractSource? contractSource = null,
        IGameLogger? logger = null)
    {
        _aiStep = aiStep;
        _autosaveStep = autosaveStep;
        _contractSource = contractSource;
        _logger = logger;
    }

    /// <summary>
    /// Проводит один день и переводит состояние на следующий.
    /// </summary>
    /// <param name="state">Состояние экономики; не может быть null.</param>
    /// <returns>Сводка выполненных шагов.</returns>
    /// <exception cref="ArgumentNullException">Состояние не задано.</exception>
    /// <exception cref="OverflowException">Казна или счётчики переполнены.</exception>
    public DailyTickResult Execute(DailyTickState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        state.Day = checked(state.Day + 1);

        ResourcePool income = state.IncomePerDay;
        state.Treasury = state.Treasury.Add(income);

        int growthUnits = 0;
        foreach (GrowthAccumulator accumulator in state.GrowthAccumulators)
        {
            growthUnits = checked(growthUnits + accumulator.AdvanceDay());
        }

        int housingUnits = 0;
        foreach (HousingStock housing in state.HousingStocks)
        {
            housingUnits = checked(housingUnits + housing.AdvanceDay());
        }

        int expiredContracts = state.RecruitmentBoard.Refresh(_contractSource, state.Day);

        int expiredEffects = 0;
        for (int i = state.TimedEffects.Count - 1; i >= 0; i--)
        {
            if (state.TimedEffects[i].AdvanceDay())
            {
                state.RemoveTimedEffectAt(i);
                expiredEffects++;
            }
        }

        _aiStep?.Execute(state);
        _autosaveStep?.Save(state);

        _logger?.Log(LogEvent.Create(
            LogLevel.Info,
            "Economy",
            $"День {state.Day}: доход начислен, прирост {growthUnits}, жилища {housingUnits}, контрактов удалено {expiredContracts}, эффектов истекло {expiredEffects}.",
            fields: new Dictionary<string, string>
            {
                ["day"] = state.Day.ToString(),
                ["growthUnits"] = growthUnits.ToString(),
                ["housingUnits"] = housingUnits.ToString(),
                ["expiredContracts"] = expiredContracts.ToString(),
                ["expiredEffects"] = expiredEffects.ToString()
            }));

        return new DailyTickResult(
            state.Day,
            income,
            growthUnits,
            housingUnits,
            expiredContracts,
            expiredEffects);
    }
}
