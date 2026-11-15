namespace Game.Core.Content.Dto;

/// <summary>
/// Юнит в JSON-контенте (GDD §8, §18). Поля совпадают с существующими
/// файлами <c>game/content/core/units</c>; <c>schemaVersion</c> задаётся
/// на уровне файла и остаётся нулём для записей внутри коллекции.
/// </summary>
public sealed class UnitDto
{
    /// <summary>Версия схемы записи; в коллекциях не заполняется.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>Стабильный ID юнита.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>ID фракции-владельца.</summary>
    public string FactionId { get; init; } = string.Empty;

    /// <summary>Тир, 1…5.</summary>
    public int Tier { get; init; }

    /// <summary>Класс юнита из данных (infantry, shooter, caster и т. п.).</summary>
    public string UnitClass { get; init; } = string.Empty;

    /// <summary>Тип передвижения.</summary>
    public string MovementType { get; init; } = string.Empty;

    /// <summary>Тип урона.</summary>
    public string DamageType { get; init; } = string.Empty;

    /// <summary>Стоимость лидерства за одного юнита.</summary>
    public int LeadershipCost { get; init; }

    /// <summary>Базовая атака.</summary>
    public int Attack { get; init; }

    /// <summary>Базовая защита.</summary>
    public int Defense { get; init; }

    /// <summary>Инициатива.</summary>
    public int Initiative { get; init; }

    /// <summary>Скорость.</summary>
    public int Speed { get; init; }

    /// <summary>Минимальный урон.</summary>
    public int DamageMin { get; init; }

    /// <summary>Максимальный урон.</summary>
    public int DamageMax { get; init; }

    /// <summary>Шанс крита, %.</summary>
    public int CritChancePercent { get; init; }

    /// <summary>Множитель критического урона, %.</summary>
    public int CritDamagePercent { get; init; }

    /// <summary>Здоровье одного юнита.</summary>
    public int HitPoints { get; init; }

    /// <summary>Пять резистов.</summary>
    public ResistancesDto Resistances { get; init; } = new();

    /// <summary>ID способностей.</summary>
    public IReadOnlyList<string> Abilities { get; init; } = Array.Empty<string>();

    /// <summary>Стоимость найма в ресурсах.</summary>
    public IReadOnlyDictionary<string, int> RecruitCost { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Ключ локализации названия.</summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>Ключ локализации описания.</summary>
    public string DescriptionKey { get; init; } = string.Empty;

    /// <summary>ID промоушенов (M2); в MVP не влияет на бой.</summary>
    public IReadOnlyList<string> PromotionIds { get; init; } = Array.Empty<string>();

    /// <summary>Зарезервированный боезапас (ADR-015); в MVP не влияет на бой.</summary>
    public int Ammo { get; init; }
}
