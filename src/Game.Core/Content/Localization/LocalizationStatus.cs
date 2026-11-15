namespace Game.Core.Content.Localization;

/// <summary>Статус готовности языка (ADR-026).</summary>
public enum LocalizationStatus
{
    /// <summary>Черновой перевод; пропуски ключей — предупреждения.</summary>
    Draft = 0,

    /// <summary>Экспериментальный перевод; скрыт в настройках по умолчанию.</summary>
    Experimental = 1,

    /// <summary>Полный проверенный перевод; обязано быть 100% покрытие.</summary>
    Complete = 2
}
