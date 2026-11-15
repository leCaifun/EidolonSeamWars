using Game.Core.Domain;
using Game.Core.Economy;

namespace Game.Core.Tests;

public class DailyTickTests
{
    private sealed class RecordingAiStep : IDailyAiStep
    {
        public List<string> Order { get; }

        public int ObservedDay { get; private set; }

        public int ObservedGold { get; private set; }

        public RecordingAiStep(List<string> order)
        {
            Order = order;
        }

        public void Execute(DailyTickState state)
        {
            ObservedDay = state.Day;
            ObservedGold = state.Treasury.Get(ResourceKind.Gold);
            Order.Add("ai");
        }
    }

    private sealed class RecordingAutosaveStep : IAutosaveStep
    {
        public List<string> Order { get; }

        public int ObservedDay { get; private set; }

        public RecordingAutosaveStep(List<string> order)
        {
            Order = order;
        }

        public void Save(DailyTickState state)
        {
            ObservedDay = state.Day;
            Order.Add("autosave");
        }
    }

    private sealed class FixedContractSource : IRecruitmentContractSource
    {
        private readonly RecruitmentContract _contract;

        public FixedContractSource(RecruitmentContract contract)
        {
            _contract = contract;
        }

        public int Calls { get; private set; }

        public IReadOnlyList<RecruitmentContract> CreateContracts(int day)
        {
            Calls++;
            return new[] { _contract };
        }
    }

    [Fact]
    public void Execute_AdvancesDayAndAddsIncome()
    {
        DailyTickState state = new(
            treasury: new ResourcePool(5, 0, 0, 0, 0, 0, 0),
            incomePerDay: new ResourcePool(10, 2, 0, 0, 0, 0, 0));
        DailyTick tick = new();

        DailyTickResult result = tick.Execute(state);

        Assert.Equal(2, state.Day);
        Assert.Equal(2, result.Day);
        Assert.Equal(15, state.Treasury.Get(ResourceKind.Gold));
        Assert.Equal(2, state.Treasury.Get(ResourceKind.Wood));
        Assert.Equal(10, result.Income.Get(ResourceKind.Gold));
    }

    [Fact]
    public void Execute_IsDeterministicForIdenticalStates()
    {
        static DailyTickState CreateState()
        {
            DailyTickState state = new(
                treasury: new ResourcePool(1, 2, 3, 4, 5, 6, 7),
                incomePerDay: new ResourcePool(2, 3, 4, 5, 6, 7, 8));
            GrowthAccumulator growth = new(growthPerDayPerMille: 700);
            HousingStock housing = new(stock: 2, cap: 20, growthPerDayPerMille: 1500, uncapturedRegenPerDay: 1);
            housing.Capture();
            state.AddGrowthAccumulator(growth);
            state.AddHousingStock(housing);
            state.AddTimedEffect(new TimedEffect("test.effect", "test.hero", daysRemaining: 10));
            state.RecruitmentBoard.Add(new RecruitmentContract("test.contract", "test.unit", 1, 10, daysRemaining: 2));
            return state;
        }

        DailyTickState first = CreateState();
        DailyTickState second = CreateState();
        DailyTick tick = new();

        List<DailyTickResult> firstResults = new();
        List<DailyTickResult> secondResults = new();

        for (int day = 0; day < 5; day++)
        {
            firstResults.Add(tick.Execute(first));
            secondResults.Add(tick.Execute(second));
        }

        Assert.Equal(secondResults, firstResults);
        Assert.Equal(second.Day, first.Day);
        Assert.Equal(second.Treasury, first.Treasury);
        Assert.Equal(second.GrowthAccumulators[0].AvailableUnits, first.GrowthAccumulators[0].AvailableUnits);
        Assert.Equal(second.HousingStocks[0].Stock, first.HousingStocks[0].Stock);
        Assert.Equal(second.TimedEffects[0].DaysRemaining, first.TimedEffects[0].DaysRemaining);
    }

    [Fact]
    public void Execute_RejectsNullState()
    {
        DailyTick tick = new();

        Assert.Throws<ArgumentNullException>(() => tick.Execute(null!));
    }

    [Fact]
    public void Execute_AccumulatesGrowthAcrossDays()
    {
        DailyTickState state = new();
        GrowthAccumulator accumulator = new(growthPerDayPerMille: 500);
        state.AddGrowthAccumulator(accumulator);
        DailyTick tick = new();

        Assert.Equal(0, tick.Execute(state).GrowthUnits);
        Assert.Equal(0, accumulator.AvailableUnits);

        Assert.Equal(1, tick.Execute(state).GrowthUnits);
        Assert.Equal(1, accumulator.AvailableUnits);
    }

    [Fact]
    public void Execute_CapsGrowthAtFourteenDaysOfProduction()
    {
        DailyTickState state = new();
        GrowthAccumulator accumulator = new(growthPerDayPerMille: 1000);
        state.AddGrowthAccumulator(accumulator);
        DailyTick tick = new();

        for (int day = 0; day < 30; day++)
        {
            tick.Execute(state);
        }

        Assert.Equal(14, accumulator.CapUnits);
        Assert.Equal(14, accumulator.AvailableUnits);
    }

    [Fact]
    public void Execute_RegeneratesCapturedHousingAndNotUncaptured()
    {
        DailyTickState state = new();
        HousingStock captured = new(stock: 0, cap: 10, growthPerDayPerMille: 1000, uncapturedRegenPerDay: 5);
        HousingStock uncaptured = new(stock: 0, cap: 10, growthPerDayPerMille: 1000, uncapturedRegenPerDay: 2);
        captured.Capture();
        state.AddHousingStock(captured);
        state.AddHousingStock(uncaptured);
        DailyTick tick = new();

        DailyTickResult result = tick.Execute(state);

        Assert.Equal(1, result.HousingUnits);
        Assert.Equal(1, captured.Stock);
        Assert.Equal(2, uncaptured.Stock);
        Assert.True(uncaptured.IsCaptured == false);
    }

    [Fact]
    public void Execute_ExpiresShortLivedContracts()
    {
        DailyTickState state = new();
        state.RecruitmentBoard.Add(new RecruitmentContract("test.contract", "test.unit", 2, 100, daysRemaining: 1));
        DailyTick tick = new();

        DailyTickResult result = tick.Execute(state);

        Assert.Equal(1, result.ExpiredContracts);
        Assert.Empty(state.RecruitmentBoard.Contracts);
    }

    [Fact]
    public void Execute_ReplacesContractsFromSource()
    {
        DailyTickState state = new();
        state.RecruitmentBoard.Add(new RecruitmentContract("test.old", "test.unit", 1, 10, daysRemaining: 5));
        FixedContractSource source = new(
            new RecruitmentContract("test.new", "test.unit", 3, 30, daysRemaining: 2));
        DailyTick tick = new(contractSource: source);

        DailyTickResult result = tick.Execute(state);

        Assert.Equal(1, source.Calls);
        Assert.Equal(1, result.ExpiredContracts);
        RecruitmentContract contract = Assert.Single(state.RecruitmentBoard.Contracts);
        Assert.Equal("test.new", contract.Id);
    }

    [Fact]
    public void Execute_TicksTimedEffectsAndRemovesExpired()
    {
        DailyTickState state = new();
        TimedEffect shortEffect = new("test.effect.short", "test.hero", daysRemaining: 1);
        TimedEffect longEffect = new("test.effect.long", "test.hero", daysRemaining: 2);
        state.AddTimedEffect(shortEffect);
        state.AddTimedEffect(longEffect);
        DailyTick tick = new();

        DailyTickResult result = tick.Execute(state);

        Assert.Equal(1, result.ExpiredEffects);
        Assert.Single(state.TimedEffects);
        Assert.Same(longEffect, state.TimedEffects[0]);
        Assert.Equal(1, longEffect.DaysRemaining);
    }

    [Fact]
    public void Execute_RunsAiThenAutosaveAfterEconomySteps()
    {
        List<string> order = new();
        RecordingAiStep aiStep = new(order);
        RecordingAutosaveStep autosaveStep = new(order);
        DailyTickState state = new(incomePerDay: new ResourcePool(7, 0, 0, 0, 0, 0, 0));
        DailyTick tick = new(aiStep, autosaveStep);

        tick.Execute(state);

        Assert.Equal(new[] { "ai", "autosave" }, order);
        Assert.Equal(2, aiStep.ObservedDay);
        Assert.Equal(7, aiStep.ObservedGold);
        Assert.Equal(2, autosaveStep.ObservedDay);
    }

    [Fact]
    public void GrowthAccumulator_ConsumeRemovesAvailableUnitsOnly()
    {
        GrowthAccumulator accumulator = new(growthPerDayPerMille: 2000);
        accumulator.AdvanceDay();

        Assert.Equal(2, accumulator.AvailableUnits);
        Assert.Equal(1, accumulator.Consume(1));
        Assert.Equal(1, accumulator.AvailableUnits);
        Assert.Equal(1, accumulator.Consume(3));
        Assert.Equal(0, accumulator.AvailableUnits);
        Assert.Throws<ArgumentOutOfRangeException>(() => accumulator.Consume(-1));
    }

    [Fact]
    public void GrowthAccumulator_RejectsNegativeGrowth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GrowthAccumulator(-1));
    }

    [Fact]
    public void HousingStock_ValidatesRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HousingStock(stock: 11, cap: 10, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HousingStock(stock: -1, cap: 10, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HousingStock(stock: 0, cap: -1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HousingStock(stock: 0, cap: 10, 0, -1));
    }

    [Fact]
    public void HousingStock_StopsAtCap()
    {
        HousingStock housing = new(stock: 9, cap: 10, growthPerDayPerMille: 5000, uncapturedRegenPerDay: 0);
        housing.Capture();

        int produced = housing.AdvanceDay();

        Assert.Equal(1, produced);
        Assert.Equal(10, housing.Stock);
    }

    [Fact]
    public void RecruitmentBoard_AddRejectsDuplicateIdentifier()
    {
        RecruitmentBoard board = new();
        board.Add(new RecruitmentContract("test.contract", "test.unit", 1, 10, 1));

        Assert.Throws<ArgumentException>(
            () => board.Add(new RecruitmentContract("test.contract", "test.unit", 2, 20, 2)));
    }

    [Fact]
    public void DailyTickState_ValidatesDayAndAccumulators()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DailyTickState(day: 0));
        Assert.Throws<ArgumentNullException>(() => new DailyTickState().AddGrowthAccumulator(null!));
        Assert.Throws<ArgumentNullException>(() => new DailyTickState().AddHousingStock(null!));
        Assert.Throws<ArgumentNullException>(() => new DailyTickState().AddTimedEffect(null!));
    }
}
