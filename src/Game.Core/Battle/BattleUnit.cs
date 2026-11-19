using Game.Core.Diagnostics;
using Game.Core.Domain;

namespace Game.Core.Battle;

/// <summary>
/// Боевое состояние стака (GDD §7.2): количество — пул здоровья, урон снимается с верхнего юнита,
/// остаток переносится. Хранит локальные флаги раунда (защита, ожидание, контратака, мораль) и
/// считает ветеранство по числу побеждённых стаков. Работает с копией данных армии: после боя
/// результат переносится обратно отдельным шагом (05f).
/// </summary>
public sealed class BattleUnit
{
    private const int MaxPercent = 100;

    private readonly List<ActiveEffect> _effects = new();
    private readonly Dictionary<string, int> _cooldowns = new(StringComparer.Ordinal);

    /// <summary>
    /// Создаёт боевой стак.
    /// </summary>
    /// <param name="id">Уникальный ID в бою (например, <c>A0</c>).</param>
    /// <param name="side">Сторона.</param>
    /// <param name="slotIndex">Индекс слота армии (тай-брейк очереди).</param>
    /// <param name="unit">Определение юнита.</param>
    /// <param name="count">Количество юнитов, не меньше 1.</param>
    /// <param name="position">Гекс на поле боя.</param>
    /// <param name="killedStacks">Побеждённые стаки до боя (ветеранство).</param>
    /// <exception cref="ArgumentException">ID пуст.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Количество, слот или счётчик убийств вне диапазона.</exception>
    public BattleUnit(
        string id,
        BattleSide side,
        int slotIndex,
        UnitDef unit,
        int count,
        Hex position,
        int killedStacks = 0)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("ID боевого стака не может быть пустым.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(unit);

        if (slotIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Индекс слота не может быть отрицательным.");
        }

        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "В боевом стаке должен быть хотя бы один юнит.");
        }

        if (killedStacks < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(killedStacks),
                killedStacks,
                "Число побеждённых стаков не может быть отрицательным.");
        }

        Id = id;
        InitialCount = count;
        InitialKilledStacks = killedStacks;
        Side = side;
        SlotIndex = slotIndex;
        Unit = unit;
        Count = count;
        TopUnitHealth = unit.Health;
        Position = position;
        KilledStacks = killedStacks;
    }

    /// <summary>ID боевого стака.</summary>
    public string Id { get; }

    /// <summary>Сторона.</summary>
    public BattleSide Side { get; }

    /// <summary>Индекс слота армии.</summary>
    public int SlotIndex { get; }

    /// <summary>Определение юнита.</summary>
    public UnitDef Unit { get; }

    /// <summary>Текущий гекс.</summary>
    public Hex Position { get; private set; }

    /// <summary>Количество живых юнитов.</summary>
    public int Count { get; private set; }

    /// <summary>Количество юнитов в начале боя.</summary>
    public int InitialCount { get; }

    /// <summary>Побеждённые стаки в начале боя.</summary>
    public int InitialKilledStacks { get; }

    /// <summary>Здоровье верхнего юнита (1…здоровье юнита); 0, если стак уничтожен.</summary>
    public int TopUnitHealth { get; private set; }

    /// <summary>Бонус атаки героя (пассивно, GDD §7.6).</summary>
    public int HeroAttackBonus { get; private set; }

    /// <summary>Бонус защиты героя (пассивно, GDD §7.6).</summary>
    public int HeroDefenseBonus { get; private set; }

    /// <summary>Пассивный бонус инициативы от класса героя.</summary>
    public int PassiveInitiativeBonus { get; private set; }

    /// <summary>Сколько шагов стак прошёл в текущем ходу (для бонуса «разгон»).</summary>
    public int StepsMovedThisTurn { get; private set; }

    /// <summary>Действующие длительные эффекты.</summary>
    public IReadOnlyList<ActiveEffect> Effects => _effects;

    /// <summary>Максимальное здоровье стака: начальное количество юнитов на здоровье юнита.</summary>
    public int MaxTotalHealth => checked(InitialCount * Unit.Health);

    /// <summary>Побеждённые стаки (ветеранство).</summary>
    public int KilledStacks { get; private set; }

    /// <summary>Жив ли стак.</summary>
    public bool IsAlive => Count > 0;

    /// <summary>Ранг ветеранства 1…5.</summary>
    public int VeterancyRank => VeterancyRules.RankForKilledStacks(KilledStacks);

    /// <summary>Атака: юнит, ветеранство, бонус героя и действующие модификаторы; не меньше 0.</summary>
    public int Attack => Math.Max(
        0,
        Unit.Attack + VeterancyRules.AttackBonus(VeterancyRank) + HeroAttackBonus
        + GetEffectSum(BattleEffectTypes.AttackModifier));

    /// <summary>Защита: юнит, ветеранство, бонус героя и действующие модификаторы (без «Защиты»); не меньше 0.</summary>
    public int Defense => Math.Max(
        0,
        Unit.Defense + VeterancyRules.DefenseBonus(VeterancyRank) + HeroDefenseBonus
        + GetEffectSum(BattleEffectTypes.DefenseModifier));

    /// <summary>Шанс крита с бонусом ветеранства, %; кап применяет калькулятор урона.</summary>
    public int CritChancePercent =>
        Math.Min(MaxPercent, Unit.CritChancePercent + VeterancyRules.CritChanceBonus(VeterancyRank));

    /// <summary>Критический урон с бонусом ветеранства, %; кап применяет калькулятор урона.</summary>
    public int CritDamagePercent => Unit.CritDamagePercent + VeterancyRules.CritDamageBonus(VeterancyRank);

    /// <summary>Инициатива: юнит, пассивный бонус класса героя и модификаторы эффектов; не меньше 0.</summary>
    public int Initiative => Math.Max(
        0,
        Unit.Initiative + PassiveInitiativeBonus + GetEffectSum(BattleEffectTypes.InitiativeModifier));

    /// <summary>Суммарное здоровье стака с учётом раненого верхнего юнита.</summary>
    public int TotalHealth => Count == 0 ? 0 : checked((Count - 1) * Unit.Health + TopUnitHealth);

    /// <summary>Действует ли «Защита» (до следующего хода юнита).</summary>
    public bool IsDefending { get; private set; }

    /// <summary>Ожидал ли юнит в текущем раунде.</summary>
    public bool WaitedThisRound { get; private set; }

    /// <summary>Ожидал ли юнит в прошлом раунде (определяет группу B).</summary>
    public bool WaitedLastRound { get; private set; }

    /// <summary>
    /// Остаток очков движения на текущий ход; в начале хода равен скорости. Перемещение тратит очки
    /// и не завершает ход; завершает ход атака (остаток сгорает), ADR-040.
    /// </summary>
    public int MovementPointsRemaining { get; private set; }

    /// <summary>Контратак, потраченных в текущем раунде.</summary>
    public int RetaliationsUsedThisRound { get; private set; }

    /// <summary>Дополнительных действий морали, полученных в текущем раунде.</summary>
    public int ExtraActionsUsedThisRound { get; private set; }

    /// <summary>
    /// Начинает новый раунд: ожидание текущего раунда становится «ожиданием прошлого раунда»,
    /// счётчики контратак и дополнительных действий сбрасываются. «Защита» раунды не пересекает
    /// сама по себе — её снимает <see cref="BeginTurn"/>.
    /// </summary>
    public void BeginRound()
    {
        WaitedLastRound = WaitedThisRound;
        WaitedThisRound = false;
        RetaliationsUsedThisRound = 0;
        ExtraActionsUsedThisRound = 0;
    }

    /// <summary>Начинает ход юнита: снимает «Защиту» и восстанавливает очки движения до скорости.</summary>
    public void BeginTurn()
    {
        IsDefending = false;
        MovementPointsRemaining = Unit.Speed;
        StepsMovedThisTurn = 0;
    }

    /// <summary>Задаёт пассивные бонусы героя и класса.</summary>
    /// <param name="attack">Бонус атаки.</param>
    /// <param name="defense">Бонус защиты.</param>
    /// <param name="initiative">Бонус инициативы.</param>
    public void SetHeroBonuses(int attack, int defense, int initiative)
    {
        HeroAttackBonus = attack;
        HeroDefenseBonus = defense;
        PassiveInitiativeBonus = initiative;
    }

    /// <summary>Фиксирует пройденные за ход шаги.</summary>
    /// <param name="steps">Число шагов, не меньше 0.</param>
    public void RegisterMovedSteps(int steps)
    {
        if (steps < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(steps), steps, "Число шагов не может быть отрицательным.");
        }

        StepsMovedThisTurn += steps;
    }

    /// <summary>Накладывает длительный эффект.</summary>
    /// <param name="type">Тип эффекта.</param>
    /// <param name="value">Значение.</param>
    /// <param name="rounds">Длительность, раундов, не меньше 1.</param>
    /// <param name="sourceId">Источник эффекта.</param>
    public void AddEffect(string type, int value, int rounds, string sourceId)
    {
        _effects.Add(new ActiveEffect(type, value, rounds, sourceId));
    }

    /// <summary>Сумма значений действующих эффектов типа.</summary>
    public int GetEffectSum(string type)
    {
        int sum = 0;

        foreach (ActiveEffect effect in _effects)
        {
            if (string.Equals(effect.Type, type, StringComparison.Ordinal))
            {
                sum = checked(sum + effect.Value);
            }
        }

        return sum;
    }

    /// <summary>Действует ли эффект типа.</summary>
    public bool HasEffect(string type)
    {
        return _effects.Any(effect => string.Equals(effect.Type, type, StringComparison.Ordinal));
    }

    /// <summary>Уменьшает длительность эффектов и убирает закончившиеся; возвращает число снятых.</summary>
    public int TickEffects()
    {
        return _effects.RemoveAll(effect => effect.Tick());
    }

    /// <summary>Остаток перезарядки способности, раундов.</summary>
    public int GetCooldown(string abilityId)
    {
        return _cooldowns.TryGetValue(abilityId, out int rounds) ? rounds : 0;
    }

    /// <summary>Запускает перезарядку способности.</summary>
    public void StartCooldown(string abilityId, int rounds)
    {
        if (rounds > 0)
        {
            _cooldowns[abilityId] = rounds;
        }
    }

    /// <summary>Уменьшает перезарядки на раунд.</summary>
    public void TickCooldowns()
    {
        foreach (string id in _cooldowns.Keys.ToList())
        {
            int left = _cooldowns[id] - 1;

            if (left <= 0)
            {
                _cooldowns.Remove(id);
            }
            else
            {
                _cooldowns[id] = left;
            }
        }
    }

    /// <summary>
    /// Лечит стак: восстанавливает здоровье пула, не выше максимума (начальное количество × здоровье юнита),
    /// возвращая погибших юнитов в пределах начального количества.
    /// </summary>
    /// <param name="amount">Сколько здоровья вернуть, не меньше 0.</param>
    /// <returns>Сколько здоровья реально восстановлено.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Значение отрицательно.</exception>
    /// <exception cref="InvalidOperationException">Стак уничтожен.</exception>
    public int Heal(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Лечение не может быть отрицательным.");
        }

        if (!IsAlive)
        {
            throw new InvalidOperationException($"Стак «{Id}» уничтожен и не может быть вылечен.");
        }

        int before = TotalHealth;
        int after = Math.Min(MaxTotalHealth, checked(before + amount));
        int unitHealth = Unit.Health;
        Count = (after + unitHealth - 1) / unitHealth;
        TopUnitHealth = after - (Count - 1) * unitHealth;
        return after - before;
    }

    /// <summary>
    /// Тратит очки движения.
    /// </summary>
    /// <param name="points">Сколько очков потратить, 0…остаток.</param>
    /// <exception cref="ArgumentOutOfRangeException">Очков больше остатка или меньше нуля.</exception>
    public void SpendMovement(int points)
    {
        if (points < 0 || points > MovementPointsRemaining)
        {
            throw new ArgumentOutOfRangeException(
                nameof(points),
                points,
                $"Нельзя потратить {points} очков движения: остаток {MovementPointsRemaining}.");
        }

        MovementPointsRemaining -= points;
    }

    /// <summary>Включает «Защиту» до следующего хода юнита.</summary>
    public void SetDefending()
    {
        IsDefending = true;
    }

    /// <summary>Отмечает ожидание в текущем раунде.</summary>
    public void MarkWaited()
    {
        WaitedThisRound = true;
    }

    /// <summary>Тратит одну контратаку текущего раунда.</summary>
    public void SpendRetaliation()
    {
        RetaliationsUsedThisRound++;
    }

    /// <summary>Фиксирует дополнительное действие морали в текущем раунде.</summary>
    public void RegisterExtraAction()
    {
        ExtraActionsUsedThisRound++;
    }

    /// <summary>Перемещает стак на гекс поля боя.</summary>
    public void MoveTo(Hex hex)
    {
        Position = hex;
    }

    /// <summary>
    /// Наносит урон: снимает здоровье с верхнего юнита, остаток переносится на следующих.
    /// Стак, получивший урон не меньше всего пула, уничтожается; перебор возвращается отдельно.
    /// </summary>
    /// <param name="damage">Урон, не меньше 0.</param>
    /// <exception cref="ArgumentOutOfRangeException">Урон отрицателен.</exception>
    /// <exception cref="InvalidOperationException">Стак уже уничтожен.</exception>
    public CasualtyResult ApplyDamage(int damage)
    {
        if (damage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage), damage, "Урон не может быть отрицательным.");
        }

        if (!IsAlive)
        {
            throw new InvalidOperationException($"Стак «{Id}» уже уничтожен и не может получать урон.");
        }

        int total = TotalHealth;
        int unitHealth = Unit.Health;

        if (damage >= total)
        {
            int killedAll = Count;
            Count = 0;
            TopUnitHealth = 0;
            return new CasualtyResult(total, damage - total, killedAll, 0, 0, true);
        }

        int remaining = total - damage;
        int newCount = (remaining + unitHealth - 1) / unitHealth;
        int newTop = remaining - (newCount - 1) * unitHealth;
        int killed = Count - newCount;

        Count = newCount;
        TopUnitHealth = newTop;

        return new CasualtyResult(damage, 0, killed, newCount, newTop, false);
    }

    /// <summary>
    /// Засчитывает побеждённые стаки; при росте ранга ветеранства пишет событие журнала.
    /// </summary>
    /// <param name="stacks">Число побеждённых стаков, не меньше 0.</param>
    /// <param name="logger">Необязательный журнал.</param>
    /// <returns><c>true</c>, если ранг вырос.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Число отрицательно.</exception>
    public bool RegisterKills(int stacks, IGameLogger? logger = null)
    {
        if (stacks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stacks), stacks, "Число побеждённых стаков не может быть отрицательным.");
        }

        int oldRank = VeterancyRank;
        KilledStacks = checked(KilledStacks + stacks);
        int newRank = VeterancyRank;

        if (newRank <= oldRank)
        {
            return false;
        }

        logger?.Log(LogEvent.Create(
            LogLevel.Info,
            "Battle",
            "Стак повысил ранг ветеранства.",
            fields: new Dictionary<string, string>
            {
                ["unitId"] = Id,
                ["oldRank"] = oldRank.ToString(),
                ["newRank"] = newRank.ToString(),
                ["killedStacks"] = KilledStacks.ToString()
            }));

        return true;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"BattleUnit({Id}, {Unit.Id} x{Count}, top={TopUnitHealth}, {Side}, slot={SlotIndex})";
    }
}
