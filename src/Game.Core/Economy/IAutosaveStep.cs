namespace Game.Core.Economy;

/// <summary>
/// Точка расширения автосохранения в дневном тике (ADR-004).
/// Запись файлов появится на этапе 07; домен вызывает шаг последним.
/// </summary>
public interface IAutosaveStep
{
    /// <summary>
    /// Выполняет автосохранение состояния после хода ИИ.
    /// </summary>
    void Save(DailyTickState state);
}
