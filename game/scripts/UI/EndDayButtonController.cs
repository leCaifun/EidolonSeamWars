using Godot;

/// <summary>
/// Кнопка «Завершить день». Сама ничего не меняет: сообщает о нажатии событием.
/// </summary>
public partial class EndDayButtonController : Button
{
	/// <summary>Игрок нажал кнопку завершения дня.</summary>
	public event Action? EndDayRequested;

	/// <inheritdoc />
	public override void _Ready()
	{
		Text = GameSession.Text("ui.map.end_day");
		Pressed += OnPressed;
	}

	private void OnPressed()
	{
		EndDayRequested?.Invoke();
	}
}
