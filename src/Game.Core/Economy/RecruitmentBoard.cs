using System.Collections.ObjectModel;

namespace Game.Core.Economy;

/// <summary>
/// Набор активных контрактов наёмников (GDD §5.5). Дневной тик либо уменьшает
/// их срок, либо заменяет ротацию источником контрактов.
/// </summary>
public sealed class RecruitmentBoard
{
    private readonly List<RecruitmentContract> _contracts = new();
    private readonly ReadOnlyCollection<RecruitmentContract> _readOnlyContracts;

    /// <summary>Создаёт пустое поле контрактов.</summary>
    public RecruitmentBoard()
    {
        _readOnlyContracts = new ReadOnlyCollection<RecruitmentContract>(_contracts);
    }

    /// <summary>Активные контракты в порядке добавления.</summary>
    public IReadOnlyList<RecruitmentContract> Contracts => _readOnlyContracts;

    /// <summary>
    /// Добавляет контракт в поле. ID контракта должен быть уникальным.
    /// </summary>
    /// <exception cref="ArgumentNullException">Контракт не задан.</exception>
    /// <exception cref="ArgumentException">Контракт с таким ID уже есть.</exception>
    public void Add(RecruitmentContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        foreach (RecruitmentContract existing in _contracts)
        {
            if (string.Equals(existing.Id, contract.Id, StringComparison.Ordinal))
            {
                throw new ArgumentException($"Контракт «{contract.Id}» уже есть на поле.", nameof(contract));
            }
        }

        _contracts.Add(contract);
    }

    /// <summary>
    /// Обновляет контракты на новый день. Если источник задан, поле полностью
    /// заменяется его ротацией; иначе сроки уменьшаются, истёкшие удаляются.
    /// </summary>
    /// <param name="source">Необязательный источник ротации.</param>
    /// <param name="day">Номер дня после тика.</param>
    /// <returns>Число удалённых или заменённых контрактов.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Номер дня меньше 1.</exception>
    public int Refresh(IRecruitmentContractSource? source, int day)
    {
        if (day < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(day), day, "Номер дня не может быть меньше 1.");
        }

        if (source is not null)
        {
            int removed = _contracts.Count;
            _contracts.Clear();
            _contracts.AddRange(source.CreateContracts(day));
            return removed;
        }

        return AdvanceDay();
    }

    /// <summary>
    /// Уменьшает срок всех контрактов и удаляет истёкшие.
    /// </summary>
    /// <returns>Число истёкших контрактов.</returns>
    public int AdvanceDay()
    {
        int expired = 0;
        for (int i = _contracts.Count - 1; i >= 0; i--)
        {
            if (_contracts[i].AdvanceDay())
            {
                _contracts.RemoveAt(i);
                expired++;
            }
        }

        return expired;
    }
}
