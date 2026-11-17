using Godot;

/// <summary>
/// Подпись с номером дня. Текст берётся из локализации (ключ ui.map.day).
/// </summary>
public partial class DayCounterController : Label
{
	/// <summary>Показывает номер дня.</summary>
	/// <param name="day">Номер дня, начиная с 1.</param>
	public void SetDay(int day)
	{
		Text = GameSession.Text("ui.map.day", "day", day.ToString(System.Globalization.CultureInfo.InvariantCulture));
	}
}
