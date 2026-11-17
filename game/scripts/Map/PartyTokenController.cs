using Game.Core.Map;
using Godot;

/// <summary>
/// Токен партии на карте. Позицию берёт из Core при вызове Refresh.
/// </summary>
public partial class PartyTokenController : Node2D
{
	/// <summary>Партия в Core.</summary>
	public Party? Subject { get; private set; }

	/// <summary>
	/// Привязывает токен к партии.
	/// </summary>
	/// <param name="party">Партия Core.</param>
	/// <param name="texture">Текстура токена; может быть <c>null</c>.</param>
	public void Bind(Party party, Texture2D? texture)
	{
		Subject = party;

		AddChild(new Sprite2D
		{
			Texture = texture,
			Centered = true,
			Scale = new Vector2(0.75f, 0.75f)
		});

		Refresh(true);
	}

	/// <summary>
	/// Переставляет токен в позицию партии и задаёт видимость.
	/// </summary>
	/// <param name="isShown">Нужно ли показывать токен (партия видна фракции игрока).</param>
	public void Refresh(bool isShown)
	{
		if (Subject is null)
		{
			return;
		}

		Position = HexMetrics.ToPixel(Subject.Position);
		Visible = isShown;
	}
}
