using System.Text;

namespace Game.Core.Rng;

/// <summary>
/// Набор детерминированных сид-стримов, производных от одного мастер-сида.
/// Стримы изолированы: расход чисел в бою не влияет на генерацию карты.
/// Порядок вызовов внутри каждого стрима — часть контракта реплея (ADR-018).
/// </summary>
public sealed class RngStreams
{
    private const string DerivationPrefix = "esw.rng.v1.";

    private readonly Dictionary<RngStreamKind, SeededRandom> _streams = new();

    /// <summary>
    /// Создаёт набор стримов от мастер-сида.
    /// </summary>
    /// <param name="masterSeed">Мастер-сид партии.</param>
    public RngStreams(ulong masterSeed)
    {
        MasterSeed = masterSeed;
    }

    /// <summary>Мастер-сид, из которого выводятся все стримы.</summary>
    public ulong MasterSeed { get; }

    /// <summary>
    /// Возвращает стрим указанного вида. Повторный вызов возвращает тот же экземпляр,
    /// поэтому последовательность продолжается, а не начинается заново.
    /// </summary>
    /// <param name="kind">Вид стрима.</param>
    public SeededRandom Get(RngStreamKind kind)
    {
        if (_streams.TryGetValue(kind, out SeededRandom? existing))
        {
            return existing;
        }

        SeededRandom created = new(DeriveSeed(MasterSeed, StreamKey(kind)));
        _streams.Add(kind, created);
        return created;
    }

    /// <summary>
    /// Выводит сид именованного стрима без создания экземпляра.
    /// Имя должно быть стабильным: изменение имени ломает воспроизводимость реплеев.
    /// </summary>
    /// <param name="masterSeed">Мастер-сид партии.</param>
    /// <param name="streamName">Стабильное имя стрима.</param>
    /// <exception cref="ArgumentException">Имя пусто.</exception>
    public static ulong DeriveSeed(ulong masterSeed, string streamName)
    {
        if (string.IsNullOrWhiteSpace(streamName))
        {
            throw new ArgumentException("Имя сид-стрима не может быть пустым.", nameof(streamName));
        }

        ulong hash = Fnv1a64(DerivationPrefix + streamName);
        // Перемешивание мастер-сида с хешем имени даёт независимые, но воспроизводимые стримы.
        return Mix64(masterSeed ^ hash);
    }

    private static string StreamKey(RngStreamKind kind)
    {
        return kind switch
        {
            RngStreamKind.Map => "map",
            RngStreamKind.Battle => "battle",
            RngStreamKind.Loot => "loot",
            RngStreamKind.LevelUp => "levelup",
            _ => throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Неизвестный вид сид-стрима.")
        };
    }

    private static ulong Fnv1a64(string value)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        ulong hash = offsetBasis;
        foreach (byte b in Encoding.UTF8.GetBytes(value))
        {
            hash ^= b;
            hash *= prime;
        }

        return hash;
    }

    private static ulong Mix64(ulong value)
    {
        ulong z = value + 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
