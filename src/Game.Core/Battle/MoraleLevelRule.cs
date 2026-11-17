namespace Game.Core.Battle;

/// <summary>
/// Строка таблицы морали из <c>rules/morale.json</c>: описательный уровень, шанс и модификатор статов.
/// </summary>
/// <param name="Level">Уровень морали, −4…+4.</param>
/// <param name="NameKey">Ключ локализации описательного названия уровня.</param>
/// <param name="ChancePercent">Шанс дополнительного действия (уровень &gt; 0) или пропуска (уровень &lt; 0), %.</param>
/// <param name="StatModifierPercent">Модификатор атаки и защиты, % (знак совпадает со знаком уровня).</param>
public readonly record struct MoraleLevelRule(
    int Level,
    string NameKey,
    int ChancePercent,
    int StatModifierPercent);
