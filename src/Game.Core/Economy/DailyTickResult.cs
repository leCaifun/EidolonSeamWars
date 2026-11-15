using Game.Core.Domain;

namespace Game.Core.Economy;

/// <summary>
/// Результат одного дневного тика (ADR-004). Используется для тестов,
/// логирования и будущего UI сводки дня.
/// </summary>
public sealed record DailyTickResult
{
    /// <summary>Создаёт результат тика.</summary>
    public DailyTickResult(
        int day,
        ResourcePool income,
        int growthUnits,
        int housingUnits,
        int expiredContracts,
        int expiredEffects)
    {
        Day = day;
        Income = income;
        GrowthUnits = growthUnits;
        HousingUnits = housingUnits;
        ExpiredContracts = expiredContracts;
        ExpiredEffects = expiredEffects;
    }

    /// <summary>Номер дня после тика.</summary>
    public int Day { get; }

    /// <summary>Начисленный доход.</summary>
    public ResourcePool Income { get; }

    /// <summary>Произведено юнитов аккумуляторами прироста.</summary>
    public int GrowthUnits { get; }

    /// <summary>Произведено юнитов захваченными жилищами.</summary>
    public int HousingUnits { get; }

    /// <summary>Удалено контрактов (истёкших или заменённых ротацией).</summary>
    public int ExpiredContracts { get; }

    /// <summary>Удалено истёкших эффектов.</summary>
    public int ExpiredEffects { get; }
}
