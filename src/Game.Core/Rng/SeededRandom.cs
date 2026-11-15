namespace Game.Core.Rng;

/// <summary>
/// Детерминированный генератор псевдослучайных чисел на основе SplitMix64.
/// Возвращает только целые значения: вещественная арифметика в домене запрещена (AGENTS.md, инвариант 5).
/// Один экземпляр создаётся на сид-стрим; порядок вызовов является частью контракта реплея.
/// </summary>
public sealed class SeededRandom
{
    private ulong _state;

    /// <summary>
    /// Создаёт генератор с указанным сидом. Нулевой сид допустим.
    /// </summary>
    /// <param name="seed">Начальное состояние генератора.</param>
    public SeededRandom(ulong seed)
    {
        Seed = seed;
        _state = seed;
    }

    /// <summary>Исходный сид генератора.</summary>
    public ulong Seed { get; }

    /// <summary>
    /// Возвращает следующее 64-битное беззнаковое число.
    /// </summary>
    public ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;
        ulong z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>
    /// Возвращает следующее 32-битное беззнаковое число.
    /// </summary>
    public uint NextUInt32()
    {
        return (uint)(NextUInt64() >> 32);
    }

    /// <summary>
    /// Возвращает целое число в диапазоне [<paramref name="minInclusive"/>; <paramref name="maxExclusive"/>).
    /// </summary>
    /// <param name="minInclusive">Нижняя граница включительно.</param>
    /// <param name="maxExclusive">Верхняя граница исключительно; должна быть больше нижней.</param>
    /// <exception cref="ArgumentOutOfRangeException">Верхняя граница не больше нижней.</exception>
    public int Next(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxExclusive),
                maxExclusive,
                "Верхняя граница диапазона должна быть больше нижней.");
        }

        ulong range = (ulong)((long)maxExclusive - minInclusive);
        ulong offset = NextBounded(range);
        return (int)(minInclusive + (long)offset);
    }

    /// <summary>
    /// Возвращает целое число в диапазоне [0; <paramref name="maxExclusive"/>).
    /// </summary>
    /// <param name="maxExclusive">Верхняя граница исключительно; должна быть больше нуля.</param>
    /// <exception cref="ArgumentOutOfRangeException">Верхняя граница не больше нуля.</exception>
    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxExclusive),
                maxExclusive,
                "Верхняя граница диапазона должна быть больше нуля.");
        }

        return Next(0, maxExclusive);
    }

    /// <summary>
    /// Возвращает число в диапазоне [0; 999] для проверок в промилле.
    /// Это основной способ броска в боевых и экономических правилах.
    /// </summary>
    public int RollPerMille()
    {
        return Next(0, 1000);
    }

    private ulong NextBounded(ulong bound)
    {
        // Отбрасывание выборок, попадающих в неполный остаток, убирает смещение.
        ulong threshold = (0UL - bound) % bound;
        while (true)
        {
            ulong value = NextUInt64();
            if (value >= threshold)
            {
                return value % bound;
            }
        }
    }
}
