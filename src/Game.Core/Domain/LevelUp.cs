using Game.Core.Rng;

namespace Game.Core.Domain;

/// <summary>
/// Детерминированный выбор прироста характеристики при повышении уровня
/// (GDD §9.1, ADR-011, ADR-017). Использует только стрим <c>levelup</c>.
/// </summary>
public static class LevelUp
{
    /// <summary>
    /// Выбирает характеристику для прироста на +1 по весам класса.
    /// Порядок перебора фиксирован порядком <see cref="HeroStatKind"/>.
    /// </summary>
    /// <param name="heroClass">Класс героя с весами левелапа.</param>
    /// <param name="levelUpRandom">Стрим <c>levelup</c>; не может быть null.</param>
    /// <exception cref="ArgumentNullException">Класс или генератор не заданы.</exception>
    /// <exception cref="InvalidOperationException">Сумма весов некорректна.</exception>
    public static HeroStatKind RollStat(HeroClassDefinition heroClass, SeededRandom levelUpRandom)
    {
        ArgumentNullException.ThrowIfNull(heroClass);
        ArgumentNullException.ThrowIfNull(levelUpRandom);

        int total = heroClass.TotalLevelUpWeight;
        int roll = levelUpRandom.Next(total);
        int cumulative = 0;

        foreach (HeroStatKind kind in Enum.GetValues<HeroStatKind>())
        {
            cumulative = checked(cumulative + heroClass.GetWeight(kind));
            if (roll < cumulative)
            {
                return kind;
            }
        }

        throw new InvalidOperationException(
            $"Не удалось выбрать характеристику класса «{heroClass.Id}»: roll={roll}, total={total}.");
    }
}
