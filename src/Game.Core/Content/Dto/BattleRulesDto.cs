namespace Game.Core.Content.Dto;

/// <summary>Боевые константы из <c>rules/battle.json</c> (GDD §7).</summary>
public sealed class BattleRulesDto
{
    /// <summary>Версия схемы.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>ID набора правил.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Ширина поля боя.</summary>
    public int FieldWidth { get; init; }

    /// <summary>Высота поля боя.</summary>
    public int FieldHeight { get; init; }

    /// <summary>Слотов армии в MVP.</summary>
    public int ArmySlots { get; init; }

    /// <summary>Максимум слотов армии.</summary>
    public int MaxArmySlots { get; init; }

    /// <summary>Выводится ли герой на поле.</summary>
    public bool HeroOnBattlefield { get; init; }

    /// <summary>Бонус защиты от действия «Защита», %.</summary>
    public int DefendDefenseBonusPercent { get; init; }

    /// <summary>Бонус резистов от защиты, % (в MVP 0).</summary>
    public int DefendResistBonusPercent { get; init; }

    /// <summary>Штраф инициативы за защиту.</summary>
    public int DefendInitiativePenalty { get; init; }

    /// <summary>Контратак за раунд.</summary>
    public int RetaliationPerRound { get; init; }

    /// <summary>Считается ли контратака действием.</summary>
    public bool RetaliationIsAction { get; init; }

    /// <summary>Ожидание переносит ход в конец раунда.</summary>
    public bool WaitMovesToEndOfRound { get; init; }

    /// <summary>Ожидание влияет на следующий раунд.</summary>
    public bool WaitAffectsNextRound { get; init; }

    /// <summary>Модификатор атаки/защиты за единицу разницы, в промилле.</summary>
    public int AttackDefenseModifierPerPoint { get; init; }

    /// <summary>Минимальный модификатор, промилле.</summary>
    public int AttackDefenseModifierMin { get; init; }

    /// <summary>Максимальный модификатор, промилле.</summary>
    public int AttackDefenseModifierMax { get; init; }

    /// <summary>Кап шанса крита, %.</summary>
    public int CritChanceCapPercent { get; init; }

    /// <summary>Кап критического урона, %.</summary>
    public int CritDamageCapPercent { get; init; }

    /// <summary>Пять типов урона.</summary>
    public IReadOnlyList<string> DamageTypes { get; init; } = Array.Empty<string>();
}
