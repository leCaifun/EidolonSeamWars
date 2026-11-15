using Game.Core.Domain;

namespace Game.Core.Heroes;

/// <summary>
/// Расчёт выкупа героя (GDD §7.7, ADR-012).
/// Цена = <c>Σ(unitPrice × count) + heroLevel × 1000 + 100 × artifactCount</c>.
/// Защитнику осады выкуп запрещён: такой счёт помечается <see cref="IsAllowed"/>=false.
/// </summary>
public sealed record RansomQuote
{
    /// <summary>Создаёт счёт на выкуп.</summary>
    public RansomQuote(int goldCost, int unitsCost, int heroLevelCost, int artifactsCost, bool isAllowed)
    {
        GoldCost = goldCost;
        UnitsCost = unitsCost;
        HeroLevelCost = heroLevelCost;
        ArtifactsCost = artifactsCost;
        IsAllowed = isAllowed;
    }

    /// <summary>Итоговая стоимость в золоте.</summary>
    public int GoldCost { get; }

    /// <summary>Стоимость выжившей армии.</summary>
    public int UnitsCost { get; }

    /// <summary>Надбавка за уровень героя.</summary>
    public int HeroLevelCost { get; }

    /// <summary>Надбавка за артефакты.</summary>
    public int ArtifactsCost { get; }

    /// <summary>Разрешён ли выкуп (защитнику осады — нет).</summary>
    public bool IsAllowed { get; }

    /// <summary>Запрещённый счёт без стоимости.</summary>
    public static RansomQuote Forbidden => new(0, 0, 0, 0, false);

    /// <summary>
    /// Проверяет, хватает ли золота в казне для оплаты счёта.
    /// </summary>
    public bool CanPay(ResourcePool treasury)
    {
        return IsAllowed && treasury.Get(ResourceKind.Gold) >= GoldCost;
    }
}
