namespace Game.Core.Economy;

/// <summary>
/// Точка расширения хода ИИ в дневном тике (ADR-004, GDD §11).
/// На этапе 02 реализация отсутствует: тик вызывает шаг, если он передан.
/// </summary>
public interface IDailyAiStep
{
    /// <summary>
    /// Выполняет ход ИИ после экономических шагов и до автосохранения.
    /// </summary>
    void Execute(DailyTickState state);
}
