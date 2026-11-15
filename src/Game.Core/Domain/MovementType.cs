namespace Game.Core.Domain;

/// <summary>
/// Тип передвижения юнита (GDD §8). Влияет на иконку карточки и будущие
/// боевые правила; в этапе 02 только валидируется.
/// </summary>
public enum MovementType
{
    /// <summary>Пехота.</summary>
    Infantry = 0,

    /// <summary>Летун.</summary>
    Flyer = 1,

    /// <summary>Стрелок.</summary>
    Ranged = 2,

    /// <summary>Осадный юнит.</summary>
    Siege = 3
}
