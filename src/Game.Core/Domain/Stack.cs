using Game.Core.Numerics;

namespace Game.Core.Domain;

/// <summary>
/// Отряд = тип × количество (GDD §7.2). Количество одновременно является пулом HP:
/// урон снимает верхнего юнита, остаток переносится. Ранг ветеранства хранится
/// за стаком и вычисляется по числу побеждённых отрядов.
/// </summary>
public sealed class Stack
{
    /// <summary>
    /// Создаёт стак указанного юнита.
    /// </summary>
    /// <exception cref="ArgumentNullException">Юнит не задан.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Количество или число побед отрицательны.</exception>
    public Stack(UnitDef unit, int count, int killedStacks = 0)
    {
        Unit = unit ?? throw new ArgumentNullException(nameof(unit));
        Count = RequireNonNegative(count, nameof(count));
        KilledStacks = RequireNonNegative(killedStacks, nameof(killedStacks));
    }

    /// <summary>Контентное описание юнита.</summary>
    public UnitDef Unit { get; }

    /// <summary>Стабильный ID юнита.</summary>
    public string UnitDefId => Unit.Id;

    /// <summary>Текущее количество юнитов в стаке.</summary>
    public int Count { get; private set; }

    /// <summary>Сколько вражеских отрядов уничтожил этот стак.</summary>
    public int KilledStacks { get; private set; }

    /// <summary>Ранг ветеранства 1…5.</summary>
    public int VeterancyRank => VeterancyRules.RankForKilledStacks(KilledStacks);

    /// <summary>Ключ локализации названия ранга.</summary>
    public string VeterancyNameKey => VeterancyRules.NameKey(VeterancyRank);

    /// <summary>Атака с бонусом ветеранства.</summary>
    public int Attack => Unit.Attack + VeterancyRules.AttackBonus(VeterancyRank);

    /// <summary>Защита с бонусом ветеранства.</summary>
    public int Defense => Unit.Defense + VeterancyRules.DefenseBonus(VeterancyRank);

    /// <summary>Крит. шанс с бонусом ветеранства, кап 75%.</summary>
    public int CritChancePercent =>
        Math.Min(75, Unit.CritChancePercent + VeterancyRules.CritChanceBonus(VeterancyRank));

    /// <summary>Крит. урон с бонусом ветеранства, кап 300%.</summary>
    public int CritDamagePercent =>
        Math.Min(300, Unit.CritDamagePercent + VeterancyRules.CritDamageBonus(VeterancyRank));

    /// <summary>Суммарное здоровье стака.</summary>
    public int TotalHealth => checked(Count * Unit.Health);

    /// <summary>Суммарная стоимость лидерства стака.</summary>
    public int LeadershipCostTotal => checked(Count * Unit.LeadershipCost);

    /// <summary>Признак пустого стака; пустые стаки не занимают слот армии.</summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// Наносит потери стаку. Возвращает фактически снятое количество.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Потери отрицательны.</exception>
    public int TakeCasualties(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Потери не могут быть отрицательными.");
        }

        int removed = Math.Min(count, Count);
        Count -= removed;
        return removed;
    }

    /// <summary>
    /// Регистрирует уничтоженный вражеский отряд и возвращает новый ранг.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Количество побед отрицательно.</exception>
    public int RegisterKilledStacks(int count = 1)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Число побед не может быть отрицательным.");
        }

        KilledStacks = checked(KilledStacks + count);
        return VeterancyRank;
    }

    /// <summary>
    /// Сливает другой стак того же юнита в текущий.
    /// </summary>
    /// <exception cref="ArgumentException">Разные типы юнитов.</exception>
    /// <exception cref="OverflowException">Количество переполняет int.</exception>
    public void Merge(Stack other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (!string.Equals(UnitDefId, other.UnitDefId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Нельзя слить стаки разных юнитов: «{UnitDefId}» и «{other.UnitDefId}».",
                nameof(other));
        }

        Count = checked(Count + other.Count);
        KilledStacks = checked(KilledStacks + other.KilledStacks);
    }

    /// <summary>
    /// Отделяет указанное количество юнитов в новый стак с пропорциональным
    /// числом побед. Исходный стак уменьшается.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Количество вне 1…<see cref="Count"/>.</exception>
    public Stack Split(int count)
    {
        if (count <= 0 || count > Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                $"Количество отделяемых юнитов должно быть в диапазоне 1…{Count}.");
        }

        int movedKills = Count == 0
            ? 0
            : (int)Math.Min(KilledStacks, IntegerMath.MulDiv(KilledStacks, count, Count));

        Count -= count;
        KilledStacks -= movedKills;
        return new Stack(Unit, count, movedKills);
    }

    private static int RequireNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Значение не может быть отрицательным.");
        }

        return value;
    }
}
