namespace Game.Core.Content.Localization;

/// <summary>
/// Разбор строкового статуса языка. Неизвестный статус не бросает исключение
/// в загрузчике: валидатор сообщает об ошибке с файлом и полем.
/// </summary>
public static class LocalizationStatusParser
{
    /// <summary>
    /// Пытается разобрать статус.
    /// </summary>
    /// <param name="value">Строка из JSON.</param>
    /// <param name="status">Разобранный статус.</param>
    /// <returns><c>true</c>, если статус известен.</returns>
    public static bool TryParse(string? value, out LocalizationStatus status)
    {
        switch (value)
        {
            case "draft":
                status = LocalizationStatus.Draft;
                return true;
            case "experimental":
                status = LocalizationStatus.Experimental;
                return true;
            case "complete":
                status = LocalizationStatus.Complete;
                return true;
            default:
                status = LocalizationStatus.Draft;
                return false;
        }
    }

    /// <summary>Возвращает строковое имя статуса для JSON.</summary>
    public static string ToToken(LocalizationStatus status)
    {
        return status switch
        {
            LocalizationStatus.Complete => "complete",
            LocalizationStatus.Experimental => "experimental",
            _ => "draft"
        };
    }
}
