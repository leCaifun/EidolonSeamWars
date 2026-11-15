using System.Collections.ObjectModel;

namespace Game.Core.Domain;

/// <summary>
/// Иммутабельный набор семи ресурсов. Операции возвращают новый набор,
/// поэтому состояние нельзя изменить в обход команд и валидации.
/// </summary>
public readonly struct ResourcePool : IEquatable<ResourcePool>
{
    private readonly int[]? _amounts;

    /// <summary>Пустой набор: все семь ресурсов равны нулю.</summary>
    public static readonly ResourcePool Empty = new(0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// Создаёт набор из семи неотрицательных значений в порядке
    /// <see cref="ResourceKind"/>: золото, древесина, руда, кристаллы,
    /// сера, ртуть, эссенция.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Хотя бы одно значение отрицательно.</exception>
    public ResourcePool(int gold, int wood, int ore, int crystal, int sulfur, int mercury, int essence)
    {
        _amounts = new[] { gold, wood, ore, crystal, sulfur, mercury, essence };

        for (int i = 0; i < _amounts.Length; i++)
        {
            if (_amounts[i] < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gold),
                    _amounts[i],
                    $"Ресурс {(ResourceKind)i} не может быть отрицательным.");
            }
        }
    }

    private ResourcePool(int[] amounts)
    {
        _amounts = amounts;
    }

    /// <summary>Количество указанного ресурса.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Неизвестный вид ресурса.</exception>
    public int this[ResourceKind kind] => Get(kind);

    /// <summary>
    /// Возвращает количество указанного ресурса.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Неизвестный вид ресурса.</exception>
    public int Get(ResourceKind kind)
    {
        ValidateKind(kind);
        return _amounts is null ? 0 : _amounts[(int)kind];
    }

    /// <summary>
    /// Возвращает новый набор с добавленным количеством ресурса.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Неизвестный ресурс или отрицательное количество.</exception>
    /// <exception cref="OverflowException">Сумма не помещается в int.</exception>
    public ResourcePool Add(ResourceKind kind, int amount)
    {
        ValidateKind(kind);
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Количество ресурса не может быть отрицательным.");
        }

        int[] result = CopyAmounts();
        result[(int)kind] = checked(result[(int)kind] + amount);
        return new ResourcePool(result);
    }

    /// <summary>
    /// Возвращает новый набор, сложенный с другим набором покомпонентно.
    /// </summary>
    /// <exception cref="OverflowException">Сумма хотя бы одного ресурса не помещается в int.</exception>
    public ResourcePool Add(ResourcePool other)
    {
        int[] result = CopyAmounts();
        int[] otherAmounts = other.CopyAmounts();
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = checked(result[i] + otherAmounts[i]);
        }

        return new ResourcePool(result);
    }

    /// <summary>
    /// Проверяет, хватает ли ресурсов для оплаты стоимости.
    /// </summary>
    public bool CanAfford(ResourcePool cost)
    {
        int[] otherAmounts = cost.CopyAmounts();
        for (int i = 0; i < otherAmounts.Length; i++)
        {
            int available = _amounts is null ? 0 : _amounts[i];
            if (available < otherAmounts[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Пытается оплатить стоимость. При нехватке ресурсов возвращает <c>false</c>
    /// и не изменяет исходный набор.
    /// </summary>
    public bool TrySpend(ResourcePool cost, out ResourcePool remaining)
    {
        int[] otherAmounts = cost.CopyAmounts();
        int[] result = CopyAmounts();

        for (int i = 0; i < otherAmounts.Length; i++)
        {
            if (result[i] < otherAmounts[i])
            {
                remaining = this;
                return false;
            }
        }

        for (int i = 0; i < otherAmounts.Length; i++)
        {
            result[i] -= otherAmounts[i];
        }

        remaining = new ResourcePool(result);
        return true;
    }

    /// <summary>
    /// Оплачивает стоимость или выбрасывает исключение с контекстом операции.
    /// </summary>
    /// <exception cref="InvalidOperationException">Ресурсов не хватает.</exception>
    public ResourcePool Spend(ResourcePool cost, string context)
    {
        if (!TrySpend(cost, out ResourcePool remaining))
        {
            throw new InvalidOperationException(
                $"Недостаточно ресурсов для операции «{context}».");
        }

        return remaining;
    }

    /// <summary>Возвращает копию значений в порядке <see cref="ResourceKind"/>.</summary>
    public IReadOnlyList<int> ToArray()
    {
        return new ReadOnlyCollection<int>(CopyAmounts());
    }

    /// <inheritdoc />
    public bool Equals(ResourcePool other)
    {
        int[] left = CopyAmounts();
        int[] right = other.CopyAmounts();

        for (int i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ResourcePool other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        int hash = 17;
        int[] amounts = CopyAmounts();
        for (int i = 0; i < amounts.Length; i++)
        {
            hash = (hash * 31) + amounts[i];
        }

        return hash;
    }

    /// <summary>Сравнивает два набора ресурсов.</summary>
    public static bool operator ==(ResourcePool left, ResourcePool right)
    {
        return left.Equals(right);
    }

    /// <summary>Сравнивает два набора ресурсов на неравенство.</summary>
    public static bool operator !=(ResourcePool left, ResourcePool right)
    {
        return !left.Equals(right);
    }

    /// <summary>Складывает два набора ресурсов.</summary>
    public static ResourcePool operator +(ResourcePool left, ResourcePool right)
    {
        return left.Add(right);
    }

    private int[] CopyAmounts()
    {
        return _amounts is null ? new int[7] : (int[])_amounts.Clone();
    }

    private static void ValidateKind(ResourceKind kind)
    {
        if (kind < ResourceKind.Gold || kind > ResourceKind.Essence)
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Неизвестный вид ресурса.");
        }
    }
}
